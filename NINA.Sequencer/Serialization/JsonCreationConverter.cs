#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NINA.Core.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;

namespace NINA.Sequencer.Serialization {

    public abstract class JsonCreationConverter<T> : JsonConverter {

        public JsonCreationConverter(ISequencerFactory factory) {
            Factory = factory;
            reuseOwnedTokens = IsBuiltInConverter(GetType());
        }

        private static bool IsBuiltInConverter(Type converterType) =>
            converterType == typeof(SequenceContainerCreationConverter)
                || converterType == typeof(SequenceItemCreationConverter)
                || converterType == typeof(SequenceConditionCreationConverter)
                || converterType == typeof(SequenceTriggerCreationConverter)
                || converterType == typeof(SequenceDateTimeProviderCreationConverter);

        private readonly bool reuseOwnedTokens;
        private static readonly IContractResolver DefaultResolver = JsonSerializer.Create().ContractResolver;

        private static bool HasDefaultJsonHooks(JsonSerializer serializer) {
            // Additional converters or resolver hooks can retain the reader's DOM even
            // when the entity itself is built in. Keep their original detached input.
            if (JsonConvert.DefaultSettings != null || !ReferenceEquals(serializer.ContractResolver, DefaultResolver)) return false;
            var converters = serializer.Converters;
            for (int i = 0; i < converters.Count; i++) {
                var converter = converters[i];
                if (!IsBuiltInConverter(converter.GetType()) || (converter is SequenceItemCreationConverter item
                    && !item.UsesBuiltInContainerConverter)) return false;
            }
            return true;
        }

        /// <summary>
        /// Create an instance of objectType, based properties in the JSON object
        /// </summary>
        /// <param name="objectType">type of object expected</param>
        /// <param name="jObject">
        /// contents of JSON object that will be deserialized
        /// </param>
        /// <returns></returns>
        public abstract T Create(Type objectType, JObject jObject);

        public override bool CanConvert(Type objectType) {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => false;

        protected ISequencerFactory Factory;

        private static string GetSourcePath(JsonSerializer serializer) =>
            serializer?.Context.Context as string ?? string.Empty;

        public static string ExtractPluginName(string typeString) {
            if (string.IsNullOrWhiteSpace(typeString)) {
                return string.Empty;
            }

            // Format: "Namespace.Type, AssemblyName"
            var parts = typeString.Split(',');
            if (parts.Length >= 2) {
                return parts[1].Trim();
            }

            return string.Empty;
        }

        private class NullUpgrader : ISequenceEntityUpgrader {
            public string Name { get; set; } = string.Empty;
            public SequenceUpgradeStage Stages => 0;
            public object Upgrade(SequenceUpgradeContext context, SequenceUpgradeStage stage, object entity) {
                return entity;
            }
        }

        private ISequenceEntityUpgrader NoUpgrader = new NullUpgrader();

        private ISequenceEntityUpgrader GetUpgraderForPlugin(string pluginName) {
            if (Factory != null) {
                foreach (var upgrader in Factory.Upgraders) {
                    var assemblyName = upgrader.GetType().Assembly.GetName().Name;
                    if (string.Equals(assemblyName, pluginName, StringComparison.OrdinalIgnoreCase)) {
                        return upgrader;
                    }
                }
            }
            return NoUpgrader;
        }

        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                         object existingValue,
                                         JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) return null;

            // Only built-in converters may reuse our own DOM. External readers and custom
            // converters keep the detached copy that Create implementations can retain.
            bool reuseTokens = reuseOwnedTokens && HasDefaultJsonHooks(serializer) && (this is not SequenceItemCreationConverter itemConverter
                || itemConverter.UsesBuiltInContainerConverter);
            var ownedObject = reuseTokens && reader is OwnedSequenceTokenReader ownedReader
                && reader.TokenType == JsonToken.StartObject ? ownedReader.CurrentToken as JObject : null;
            JObject streamHeader = null;
            ISequenceEntityUpgrader streamUpgrader = null;
            if (reuseTokens && reader is SequenceFileJsonReader fileReader && fileReader.TryGetEnvelope(out var header)) {
                streamUpgrader = GetUpgraderForPlugin(ExtractPluginName((string)header["$type"]));
                if (streamUpgrader == NoUpgrader) streamHeader = header;
            }
            bool streamEnvelope = streamHeader != null;
            int sourceDepth = reader.Depth;
            JObject jObject = streamHeader ?? ownedObject ?? JObject.Load(reader);
            if (ownedObject != null) reader.Skip();
            T target = default(T);

            try {
                if (jObject != null) {
                    if (jObject["$ref"] != null) {
                        string id = (jObject["$ref"] as JValue).Value as string;
                        target = (T)serializer.ReferenceResolver.ResolveReference(serializer, id);
                    } else {
                        JToken token;
                        jObject.TryGetValue("$type", out token);
                        string originalType = token?.ToString();

                        // Extract plugin name and get upgrader
                        string pluginName = ExtractPluginName(originalType);
                        ISequenceEntityUpgrader upgrader = streamUpgrader ?? GetUpgraderForPlugin(pluginName);

                        // Only create upgradeContext if an upgrader exists
                        SequenceUpgradeContext upgradeContext = null;
                        if (upgrader != NoUpgrader) {
                            if (ownedObject != null) jObject = (JObject)jObject.DeepClone();
                            upgradeContext = new SequenceUpgradeContext {
                                Serializer = serializer,
                                RequestedType = objectType,
                                Json = jObject,
                                OriginalTypeString = originalType,
                                Factory = Factory
                            };
                        }

                        // BeforeCreate stage
                        if (upgrader.Stages.HasFlag(SequenceUpgradeStage.BeforeCreate)) {
                            try {
                                var beforeCreateResult = upgrader.Upgrade(upgradeContext, SequenceUpgradeStage.BeforeCreate, null);
                                if (beforeCreateResult is JObject modifiedJObject) {
                                    jObject = modifiedJObject;
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"BeforeCreate upgrade failed for type {originalType}: {ex.Message}");
                            }
                        }

                        // Create stage
                        if (upgrader.Stages.HasFlag(SequenceUpgradeStage.Create)) {
                            try {
                                var createResult = upgrader.Upgrade(upgradeContext, SequenceUpgradeStage.Create, target);
                                if (createResult != null && createResult is T typedResult) {
                                    target = typedResult;
                                } else {
                                    target = Create(objectType, jObject);
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"Create upgrade failed for type {originalType}: {ex.Message}");
                            }
                        } else {
                            // Create target object (uses the potentially modified jObject)
                            target = Create(objectType, jObject);
                        }

                        // AfterCreate stage
                        if (upgrader.Stages.HasFlag(SequenceUpgradeStage.AfterCreate)) {
                            try {
                                var afterCreateResult = upgrader.Upgrade(upgradeContext, SequenceUpgradeStage.AfterCreate, target);
                                if (afterCreateResult != null && afterCreateResult is T typedResult) {
                                    target = typedResult;
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"AfterCreate upgrade failed for type {originalType}: {ex.Message}");
                            }
                        }

                        // Upgraders and custom converters can observe their JSON after population,
                        // so child migrations must still operate on independent copies there.
                        if (streamEnvelope && target?.GetType() != GetType(originalType)) {
                            // A custom factory may return a subtype with additional JSON behavior.
                            // Keep the instance already created but restore its ordinary DOM input.
                            jObject = JObject.Load(reader);
                            streamEnvelope = false;
                        }
                        if (target != null && target.GetType().Assembly != typeof(SequenceJsonConverter).Assembly) {
                            // Plugin properties can have converters that retain their reader's DOM.
                            // Preserve the detached snapshot and isolate later child migrations.
                            if (ReferenceEquals(jObject, ownedObject)) jObject = (JObject)jObject.DeepClone();
                            reuseTokens = false;
                        }
                        serializer.Populate(streamEnvelope ? reader : reuseTokens && upgrader == NoUpgrader
                            ? new OwnedSequenceTokenReader(jObject) : jObject.CreateReader(), target);

                        // AfterPopulate stage
                        if (upgrader.Stages.HasFlag(SequenceUpgradeStage.AfterPopulate)) {
                            try {
                                var afterPopulateResult = upgrader.Upgrade(upgradeContext, SequenceUpgradeStage.AfterPopulate, target);
                                if (afterPopulateResult != null && afterPopulateResult is T typedResult) {
                                    target = typedResult;
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"AfterPopulate upgrade failed for type {originalType}: {ex.Message}");
                            }
                        }

                        // Handle parent attachment if target was replaced
                        if (target is ISequenceEntity entity && entity.Parent == null) {
                            if (existingValue is ISequenceEntity existingEntity && existingEntity.Parent != null) {
                                entity.AttachNewParent(existingEntity.Parent);
                            }
                        }
                    }
                }

                return target;
            } catch (Exception ex) {
                if (streamEnvelope) SequenceFileJsonReader.FinishObject(reader, sourceDepth);
                var sourcePath = GetSourcePath(serializer);
                Logger.Error($"Deserialize failed. File='{sourcePath}', Error={ex.Message}");
                var unknownEntityName = "";
                if (jObject.TryGetValue("$type", out var token)) {
                    unknownEntityName = token?.ToString() ?? "";
                }
                switch (objectType) {
                    case ISequenceTrigger:
                        return new UnknownSequenceTrigger(unknownEntityName);
                    case ISequenceCondition:
                        return new UnknownSequenceCondition(unknownEntityName);
                    default:
                        return new UnknownSequenceItem(unknownEntityName);
                }
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        protected Type GetType(string typeString) {
            var t = Type.GetType(typeString);
            if (t == null) {
                //Migration from Versions prior to the module split
                t = Type.GetType(typeString.Replace(", NINA", ", NINA.Sequencer"));
                if (t == null) {
                    t = Type.GetType(typeString.Replace(", NINA", ", NINA.Core"));
                    if (t == null) {
                        t = Type.GetType(typeString.Replace(", NINA", ", NINA.Astrometry"));
                    }
                }
            }
            return t;
        }
    }

    internal sealed class OwnedSequenceTokenReader : JTokenReader {
        // Keep reader paths relative to this entity even when its token has a parent.
        public OwnedSequenceTokenReader(JObject value) : base(value, string.Empty) { }
    }
}