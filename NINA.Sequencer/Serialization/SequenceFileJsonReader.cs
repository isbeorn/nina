#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NINA.Sequencer.Serialization {
    // A validation pass lets the two large built-in envelopes populate directly from
    // the file. Other entities still use the detached DOM expected by converters.
    internal sealed class SequenceFileJsonReader : JsonTextReader {
        private readonly FileStream stream;
        private readonly Dictionary<(int Line, int Column), JObject> envelopes;

        private SequenceFileJsonReader(FileStream stream, Dictionary<(int, int), JObject> envelopes)
            : base(CreateTextReader(stream)) {
            this.stream = stream;
            this.envelopes = envelopes;
        }

        internal static SequenceFileJsonReader Open(string path) {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try {
                var envelopes = new Dictionary<(int, int), JObject>();
                {
                    using var reader = new JsonTextReader(CreateTextReader(stream));
                    try {
                        if (ReadContent(reader) && reader.TokenType == JsonToken.StartObject) {
                            ScanEnvelope(reader, typeof(SequenceRootContainer), true, envelopes);
                            if (ReadContent(reader)) envelopes.Clear();
                        }
                    } catch (JsonException) {
                        // Let the ordinary loader retain its existing malformed-input behavior.
                        envelopes.Clear();
                    }
                }
                stream.Position = 0;
                // Recreate the text reader so BOM detection also runs on the second pass.
                return new SequenceFileJsonReader(stream, envelopes);
            } catch {
                stream.Dispose();
                throw;
            }
        }

        internal bool TryGetEnvelope(out JObject header) {
            header = null;
            return TokenType == JsonToken.StartObject && envelopes.TryGetValue((LineNumber, LinePosition), out header);
        }

        public override void Close() {
            try { base.Close(); }
            finally { stream.Dispose(); }
        }

        private static StreamReader CreateTextReader(Stream stream) =>
            new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);

        private static bool ReadContent(JsonReader reader) {
            while (reader.Read()) {
                if (reader.TokenType != JsonToken.Comment) return true;
            }
            return false;
        }

        private static bool ReadStringProperty(JsonReader reader, string name) =>
            ReadContent(reader) && reader.TokenType == JsonToken.PropertyName && (string)reader.Value == name
            && ReadContent(reader) && reader.TokenType == JsonToken.String;

        private static bool IsType(string name, Type type) =>
            name == type.FullName + ", " + type.Assembly.GetName().Name || name == type.AssemblyQualifiedName;

        private static void ScanEnvelope(JsonTextReader reader, Type type, bool scanAreas,
            Dictionary<(int, int), JObject> envelopes) {
            int depth = reader.Depth;
            var position = (reader.LineNumber, reader.LinePosition);
            if (!ReadStringProperty(reader, "$id")) { FinishObject(reader, depth); return; }
            string id = (string)reader.Value;
            if (!ReadStringProperty(reader, "$type") || !IsType((string)reader.Value, type)) {
                FinishObject(reader, depth);
                return;
            }

            var header = new JObject { ["$id"] = id, ["$type"] = (string)reader.Value };
            var names = new HashSet<string>(StringComparer.Ordinal) { "$id", "$type" };
            bool eligible = true;
            while (ReadContent(reader) && reader.TokenType == JsonToken.PropertyName) {
                string name = (string)reader.Value;
                eligible &= names.Add(name) && !name.StartsWith("$", StringComparison.Ordinal);
                if (!ReadContent(reader)) break;
                if (name == "Strategy") {
                    var strategy = ReadStrategy(reader);
                    if (strategy == null) eligible = false;
                    else header[name] = strategy;
                } else if (name == "Items" || name == "Conditions" || name == "Triggers") {
                    eligible &= ScanCollection(reader, scanAreas && name == "Items", envelopes);
                } else {
                    // Typed text-reader methods can convert dates and numeric strings
                    // differently from a DOM. Only canonical scalar values may stream.
                    eligible &= name switch {
                        "Name" => reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Null,
                        "IsExpanded" => reader.TokenType == JsonToken.Boolean,
                        "Attempts" or "ErrorBehavior" => reader.TokenType == JsonToken.Integer,
                        "Parent" => reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.Null,
                        _ => false
                    };
                    reader.Skip();
                }
            }
            if (eligible && header["Strategy"] != null && reader.TokenType == JsonToken.EndObject && reader.Depth == depth) {
                envelopes[position] = header;
            }
        }

        private static JObject ReadStrategy(JsonTextReader reader) {
            if (reader.TokenType != JsonToken.StartObject) { reader.Skip(); return null; }
            var strategy = new JObject();
            bool eligible = true;
            while (ReadContent(reader) && reader.TokenType == JsonToken.PropertyName) {
                string name = (string)reader.Value;
                if (!ReadContent(reader)) break;
                if ((name == "$id" || name == "$type") && reader.TokenType == JsonToken.String && strategy[name] == null) {
                    strategy[name] = (string)reader.Value;
                } else {
                    eligible = false;
                    reader.Skip();
                }
            }
            return eligible && IsType((string)strategy["$type"], typeof(SequentialStrategy)) ? strategy : null;
        }

        private static bool ScanCollection(JsonTextReader reader, bool scanAreas, Dictionary<(int, int), JObject> envelopes) {
            if (reader.TokenType == JsonToken.StartArray) {
                ScanItems(reader, scanAreas, envelopes);
                return true;
            }
            if (reader.TokenType != JsonToken.StartObject) { reader.Skip(); return false; }
            var names = new HashSet<string>(StringComparer.Ordinal);
            bool eligible = true;
            bool valuesSeen = false;
            while (ReadContent(reader) && reader.TokenType == JsonToken.PropertyName) {
                string name = (string)reader.Value;
                eligible &= names.Add(name) && !valuesSeen;
                if (!ReadContent(reader)) break;
                if (name == "$values" && reader.TokenType == JsonToken.StartArray) {
                    valuesSeen = true;
                    ScanItems(reader, scanAreas, envelopes);
                } else {
                    eligible &= (name == "$id" || name == "$type") && reader.TokenType == JsonToken.String;
                    reader.Skip();
                }
            }
            return eligible && valuesSeen;
        }

        private static void ScanItems(JsonTextReader reader, bool scanAreas, Dictionary<(int, int), JObject> envelopes) {
            if (!scanAreas) { reader.Skip(); return; }
            while (ReadContent(reader) && reader.TokenType != JsonToken.EndArray) {
                if (reader.TokenType == JsonToken.StartObject) ScanEnvelope(reader, typeof(TargetAreaContainer), false, envelopes);
                else reader.Skip();
            }
        }

        internal static void FinishObject(JsonReader reader, int depth) {
            while (reader.Depth > depth || reader.TokenType != JsonToken.EndObject) {
                reader.Skip();
                if (reader.Depth == depth && reader.TokenType == JsonToken.EndObject) return;
                if (!reader.Read()) return;
            }
        }
    }
}
