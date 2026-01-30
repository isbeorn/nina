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
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Xaml.Behaviors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using Parlot.Fluent;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using static NINA.Astrometry.NOVAS;

namespace NINA.Sequencer.Serialization {

    /// <summary>
    /// Non-generic static storage for upgraders to avoid generic type parameter issues
    /// </summary>
    public static class SequenceEntityUpgraderRegistry {
        // Dictionary keyed by (pluginName, stage) -> list of upgraders
        private static Dictionary<(string pluginName, SequenceUpgradeStage stage), List<ISequenceEntityUpgrader>> _upgraders 
            = new Dictionary<(string, SequenceUpgradeStage), List<ISequenceEntityUpgrader>>();
        
        private static ISequencerFactory _factory;

        /// <summary>
        /// Register upgraders from a specific plugin, indexed by plugin name and stage
        /// </summary>
        public static void RegisterUpgraders(string pluginName, IEnumerable<ISequenceEntityUpgrader> upgraders) {
            if (string.IsNullOrWhiteSpace(pluginName) || upgraders == null) return;

            foreach (var upgrader in upgraders) {
                var key = (pluginName, upgrader.Stage);
                if (!_upgraders.ContainsKey(key)) {
                    _upgraders[key] = new List<ISequenceEntityUpgrader>();
                }
                _upgraders[key].Add(upgrader);
            }
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
        /// Get upgraders for a specific plugin and stage
        /// </summary>
        public static IEnumerable<ISequenceEntityUpgrader> GetUpgradersForPlugin(string pluginName, SequenceUpgradeStage stage) {
            if (string.IsNullOrWhiteSpace(pluginName)) {
                return Enumerable.Empty<ISequenceEntityUpgrader>();
            }

            var key = (pluginName, stage);
            return _upgraders.TryGetValue(key, out var upgraders) 
                ? upgraders 
                : Enumerable.Empty<ISequenceEntityUpgrader>();
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

                        Upgrade lite = Upgrade.NINA;
                        (lite, token) = PowerupsLiteSimpleMigration(token?.ToString());

                        if (lite == Upgrade.Lite) {
                            jObject["$type"] = token;
                        }

                        // Extract plugin name from originalType (e.g., "WhenPlugin.When.ExpressionCondition, WhenPlugin" -> "WhenPlugin")
                        string pluginName = SequenceEntityUpgraderRegistry.ExtractPluginName(originalType);

                        // Create upgrade context for plugin upgraders
                        var upgradeContext = new SequenceUpgradeContext {
                            Serializer = serializer,
                            RequestedType = objectType,
                            Json = jObject,
                            OriginalTypeString = originalType,
                            Factory = SequenceEntityUpgraderRegistry.Factory
                        };

                        // Run BeforeCreate upgraders for this specific plugin
                        object beforeCreateResult = null;
                        var beforeCreateUpgraders = SequenceEntityUpgraderRegistry.GetUpgradersForPlugin(pluginName, SequenceUpgradeStage.BeforeCreate);
                        foreach (var upgrader in beforeCreateUpgraders) {
                            try {
                                if (upgrader.CanUpgrade(upgradeContext)) {
                                    beforeCreateResult = upgrader.Upgrade(upgradeContext, beforeCreateResult);
                                    // If upgrader modified the JObject, update it and recreate context
                                    if (beforeCreateResult is JObject modifiedJObject) {
                                        jObject = modifiedJObject;
                                        upgradeContext = new SequenceUpgradeContext {
                                            Serializer = serializer,
                                            RequestedType = objectType,
                                            Json = modifiedJObject,
                                            OriginalTypeString = originalType,
                                            Factory = SequenceEntityUpgraderRegistry.Factory
                                        };
                                    }
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"BeforeCreate upgrader '{upgrader.Name}' failed for type {originalType}: {ex.Message}");
                            }
                        }

                        // Create target object based on JObject
                        target = Create(objectType, jObject);

                        // Run AfterCreate upgraders for this specific plugin
                        var afterCreateUpgraders = SequenceEntityUpgraderRegistry.GetUpgradersForPlugin(pluginName, SequenceUpgradeStage.AfterCreate);
                        foreach (var upgrader in afterCreateUpgraders) {
                            try {
                                if (upgrader.CanUpgrade(upgradeContext)) {
                                    var upgraded = upgrader.Upgrade(upgradeContext, target);
                                    if (upgraded != null && upgraded is T typedUpgraded) {
                                        target = typedUpgraded;
                                    }
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"AfterCreate upgrader '{upgrader.Name}' failed for type {originalType}: {ex.Message}");
                            }
                        }
                  
                        // Populate the object properties
                        serializer.Populate(jObject.CreateReader(), target);

                        // Run AfterPopulate upgraders for this specific plugin
                        var afterPopulateUpgraders = SequenceEntityUpgraderRegistry.GetUpgradersForPlugin(pluginName, SequenceUpgradeStage.AfterPopulate);
                        foreach (var upgrader in afterPopulateUpgraders) {
                            try {
                                if (upgrader.CanUpgrade(upgradeContext)) {
                                    var upgraded = upgrader.Upgrade(upgradeContext, target);
                                    if (upgraded != null && upgraded is T typedUpgraded) {
                                        target = typedUpgraded;
                                    }
                                }
                            } catch (Exception ex) {
                                Logger.Warning($"AfterPopulate upgrader '{upgrader.Name}' failed for type {originalType}: {ex.Message}");
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

        private enum Upgrade { NINA, Lite, None }

        // When all that's needed is changing the $type
        private (Upgrade, string) PowerupsLiteSimpleMigration(string token) => token switch {
            "WhenPlugin.When.CVContainer, WhenPlugin" => (Upgrade.Lite, "NINA.Sequencer.Container.SequentialContainer, NINA.Sequencer"),
            // Complex types
            "WhenPlugin.When.Call, WhenPlugin" => (Upgrade.None, "WhenPlugin.When.Call, WhenPlugin"), // No change),
            "WhenPlugin.When.Return, WhenPlugin" => (Upgrade.None, "WhenPlugin.When.Return, WhenPlugin"), // No change),

            _ => (Upgrade.NINA, token)
        };

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