#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Xaml.Behaviors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using Parlot.Fluent;

namespace NINA.Sequencer.Serialization {

    public abstract class JsonCreationConverter<T> : JsonConverter {

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

        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                         object existingValue,
                                         JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) return null;

            if (this is JsonCreationConverter<ISequenceContainer> c) {
                PowerupsUpgrader.RegisterContainerConverter(c);
            } else if (this is JsonCreationConverter<ISequenceItem> i) {
                PowerupsUpgrader.RegisterItemConverter(i);
            }

            // Load JObject from stream
            JObject jObject = JObject.Load(reader);
            T target = default(T);
            if (jObject != null) {
                if (jObject["$ref"] != null) {
                    string id = (jObject["$ref"] as JValue).Value as string;
                    target = (T)serializer.ReferenceResolver.ResolveReference(serializer, id);
                } else {
                    JToken token;
                    jObject.TryGetValue("$type", out token);
                    Logger.Info("Creating " + objectType);

                    // Create target object based on JObject
                    target = Create(objectType, jObject);

                    // Populate the object properties
                    serializer.Populate(jObject.CreateReader(), target);

                    if (jObject.TryGetValue("$type", out token)) {
                        string ts = token.ToString();
                        if (ts.EndsWith(", WhenPlugin")) {
                            target = (T)PowerupsUpgrader.UpgradeInstruction(target);
                        }
                    }
                }
            }

            return target;
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
}