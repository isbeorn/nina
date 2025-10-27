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

        /*
         * There are a number of upgrade cases:
         * 1) Upgrading NINA 3.2 instructions to NINA 3.3
         *    These require PowerupsUpgrader.UpgradeInstruction, which creates and populates the NINA 3.3 instruction
         * 2) Upgrading Powerups 3.2 + instructions into newly created NINA 3.3 instructions
         *    These would be LoopWhile and WaitUntil
         * 3) Upgrading Powerups 3.2 instructions without Expressions to Powerups 3.3 instructions
         *    These don't require anything
         * 4) Upgrading Powerups 3.2 instructions with Expressions to Powerups 3.3 instructions
         *    These require PowerupsUpgrader.PreUpgradeInstruction to allow the 3.2 instructions to be deserialized
         *    And then PowerupsUpgrader.UpgradeInstruction to populate Expressions from the old Expr class
         */

        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                         object existingValue,
                                         JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) return null;

            // There's got to be a better way to do this...
            if (this is JsonCreationConverter<ISequenceContainer> c) {
                PowerupsUpgrader.RegisterContainerConverter(c);
            } else if (this is JsonCreationConverter<ISequenceCondition> q) {
                PowerupsUpgrader.RegisterConditionConverter(q);
            } else if (this is JsonCreationConverter<ISequenceItem> i) {
                PowerupsUpgrader.RegisterItemConverter(i);
            } else if (this is JsonCreationConverter<ISequenceTrigger> t) {
                PowerupsUpgrader.RegisterTriggerConverter(t);
            }

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
                        string originalType = token.ToString();

                        Upgrade lite = Upgrade.NINA;
                        (lite, token) = PowerupsLiteSimpleMigration(token?.ToString());

                        if (lite == Upgrade.Lite) {
                            jObject["$type"] = token;
                        }

                        // Create target object based on JObject
                        target = Create(objectType, jObject);


                        if (lite == Upgrade.Lite) {
                            // Fix up name of the upgraded instruction (this doesn't persist)
                            ((ISequenceEntity)target).Name += " [Lite";
                        }

                        if (lite == Upgrade.None) {
                            ((ISequenceEntity)target).Name += " [CANNOT UPGRADE";
                            return target;
                        }

                        PowerupsUpgrader.PreUpgradeInstruction(originalType, jObject);
                   
                        // Populate the object properties
                        serializer.Populate(jObject.CreateReader(), target);

                        if (jObject.TryGetValue("$type", out token)) {
                            string ts = token.ToString();
                            if (ts.EndsWith(", WhenPlugin")) {
                                ISequenceEntity oldTarget = target as ISequenceEntity;
                                ISequenceEntity newTarget = (T)PowerupsUpgrader.UpgradeInstruction(target, jObject) as ISequenceEntity;
                                if (newTarget.Parent == null) {
                                    newTarget.AttachNewParent(oldTarget.Parent);
                                }
                                target = (T)newTarget;
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