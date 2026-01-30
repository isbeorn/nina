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
using System.Collections.Generic; 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;

namespace NINA.Sequencer.Serialization {

    /// <summary>
    /// Non-generic static storage for upgraders - one per plugin
    /// </summary>
    public static class SequenceEntityUpgraderRegistry {
        // Dictionary keyed by plugin name -> single upgrader that handles all stages
        private static Dictionary<string, ISequenceEntityUpgrader> _upgraders = new Dictionary<string, ISequenceEntityUpgrader>();
        private static ISequencerFactory _factory;

        /// <summary>
        /// Register a single upgrader for a plugin (handles all stages)
        /// </summary>
        public static void RegisterUpgrader(string pluginName, ISequenceEntityUpgrader upgrader) {
            if (string.IsNullOrWhiteSpace(pluginName) || upgrader == null) return;
            _upgraders[pluginName] = upgrader;
            Logger.Info($"Registered upgrader for {pluginName}");
        }

        /// <summary>
        /// Register the sequencer factory for use in upgrade contexts
        /// </summary>
        public static void RegisterFactory(ISequencerFactory factory) {
            _factory = factory;
        }

        /// <summary>
        /// Get factory instance
        /// </summary>
        public static ISequencerFactory Factory => _factory;

        /// <summary>
        /// Get upgrader for a specific plugin
        /// </summary>
        public static ISequenceEntityUpgrader GetUpgraderForPlugin(string pluginName) {
            if (string.IsNullOrWhiteSpace(pluginName)) {
                return null;
            }

            return _upgraders.TryGetValue(pluginName, out var upgrader) ? upgrader : null;
        }

        /// <summary>
        /// Extract plugin/assembly name from a type string like "Namespace.Type, AssemblyName"
        /// </summary>
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
    }

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

            // Load JObject from stream
            JObject jObject = JObject.Load(reader);
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
                        string pluginName = SequenceEntityUpgraderRegistry.ExtractPluginName(originalType);
                        var upgrader = SequenceEntityUpgraderRegistry.GetUpgraderForPlugin(pluginName);

                        // Only create upgradeContext if an upgrader exists
                        SequenceUpgradeContext upgradeContext = null;
                        if (upgrader != null) {
                            upgradeContext = new SequenceUpgradeContext {
                                Serializer = serializer,
                                RequestedType = objectType,
                                Json = jObject,
                                OriginalTypeString = originalType,
                                Factory = SequenceEntityUpgraderRegistry.Factory
                            };
                        }

                        // BeforeCreate stage
                        if (upgrader != null && upgrader.CanUpgrade(upgradeContext, SequenceUpgradeStage.BeforeCreate)) {
                            try {
                                var beforeCreateResult = upgrader.Upgrade(upgradeContext, SequenceUpgradeStage.BeforeCreate, null);
                                if (beforeCreateResult is JObject modifiedJObject) {
                                    jObject = modifiedJObject;
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"BeforeCreate upgrade failed for type {originalType}: {ex.Message}");
                            }
                        }

                        // Create target object (uses the potentially modified jObject)
                        target = Create(objectType, jObject);

                        // AfterCreate stage
                        if (upgrader != null && upgrader.CanUpgrade(upgradeContext, SequenceUpgradeStage.AfterCreate)) {
                            try {
                                var afterCreateResult = upgrader.Upgrade(upgradeContext, SequenceUpgradeStage.AfterCreate, target);
                                if (afterCreateResult != null && afterCreateResult is T typedResult) {
                                    target = typedResult;
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"AfterCreate upgrade failed for type {originalType}: {ex.Message}");
                            }
                        }

                        // Populate the object properties
                        serializer.Populate(jObject.CreateReader(), target);

                        // AfterPopulate stage
                        if (upgrader != null && upgrader.CanUpgrade(upgradeContext, SequenceUpgradeStage.AfterPopulate)) {
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
                Logger.Error("Failed to deserialize sequence entity", ex);
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
}