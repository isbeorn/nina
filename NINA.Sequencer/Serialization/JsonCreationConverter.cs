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

            T target = default(T);

            try {
                // Load JObject from stream
                JObject jObject = JObject.Load(reader);
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
                            // Substitute with Powerups Lite class
                            jObject["$type"] = token;
                        }

                        // Create target object based on JObject
                        target = Create(objectType, jObject);


                        if (lite == Upgrade.Lite) {
                            // Fix up name of the upgraded instruction (this doesn't persist)
                            ((ISequenceEntity)target).Name += " [SP->Lite";
                        }

                        if (lite == Upgrade.None) {
                            ((ISequenceEntity)target).Name += " [CANNOT UPGRADE";
                            return target;
                        }

                        // Populate the object properties
                        serializer.Populate(jObject.CreateReader(), target);

                        if (jObject.TryGetValue("$type", out token)) {
                            string ts = token.ToString();
                            if (ts.EndsWith(", WhenPlugin")) {
                                ISequenceEntity oldTarget = target as ISequenceEntity;
                                ISequenceEntity newTarget = (T)PowerupsUpgrader.UpgradeInstruction(target) as ISequenceEntity;
                                if (newTarget.Parent == null) {
                                    newTarget.AttachNewParent(oldTarget.Parent);
                                }
                                target = (T)newTarget;
                            } else if (ts == "PowerupsLite.When.IfConstant, PowerupsLite" || ts == "PowerupsLite.When.IfThenElse, PowerupsLite" || ts == "PowerupsLite.When.WhenSwitch, PowerupsLite") {
                                // Instruction is already upgraded, along with the contents of its instruction sets; need to get the predicate
                                Expression expr = (Expression)target.GetType().GetProperty("PredicateExpression").GetValue(target, null);
                                if (jObject["IfExpr"] != null) {
                                    expr.Definition = jObject["IfExpr"]["Expression"].ToString();
                                }
                            }
                        }
                    }
                }

                return target;
            } catch (Exception ex) {
                return target;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        private enum Upgrade { NINA, Lite, None }

        // When all that's needed is changing the $type
        private (Upgrade, string) PowerupsLiteSimpleMigration(string token) => token switch {
            "WhenPlugin.When.CVContainer, WhenPlugin" => (Upgrade.Lite, "NINA.Sequencer.Container.SequentialContainer, NINA.Sequencer"),
            "WhenPlugin.When.DIYMeridianFlipTrigger, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.DIYMeridianFlipTrigger, PowerupsLite"),
            "WhenPlugin.When.AssignVariables, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.AssignVariables, PowerupsLite"),
            "WhenPlugin.When.AutofocusTrigger, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.AutofocusTrigger, PowerupsLite"),
            "WhenPlugin.When.Break, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.Break, PowerupsLite"),
            "WhenPlugin.When.RotateImage, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.RotateImage, PowerupsLite"),
            "WhenPlugin.When.DIYTrigger, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.DIYTrigger, PowerupsLite"),
            "WhenPlugin.When.DoFlip, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.DoFlip, PowerupsLite"),
            "WhenPlugin.When.EndInstructionSet, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.EndInstructionSet, PowerupsLite"),
            "WhenPlugin.When.EndSequence, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.EndSequence, PowerupsLite"),
            "WhenPlugin.When.ExternalScript, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.ExternalScript, PowerupsLite"),
            "WhenPlugin.When.FlipRotator, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.FlipRotator, PowerupsLite"),
            "WhenPlugin.When.GSSend, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.GSSend, PowerupsLite"),
            "WhenPlugin.When.ForEachList, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.ForEachList, PowerupsLite"),
            "WhenPlugin.When.ForEachInArray, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.ForEachInArray, PowerupsLite"),
            "WhenPlugin.When.IfFailed, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.IfFailed, PowerupsLite"),
            "WhenPlugin.When.IfTimeout, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.IfTimeout, PowerupsLite"),
            "WhenPlugin.When.InterruptTrigger, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.InterruptTrigger, PowerupsLite"),
            "WhenPlugin.When.LogThis, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.LogThis, PowerupsLite"),
            "WhenPlugin.When.Log, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.Log, PowerupsLite"),
            "WhenPlugin.When.OnceSafe, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.OnceSafe, PowerupsLite"),
            "WhenPlugin.When.PassMeridian, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.PassMeridian, PowerupsLite"),
            "WhenPlugin.When.SafeTrigger, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.SafeTrigger, PowerupsLite"),
            "WhenPlugin.When.TemplateByReference, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.TemplateByReference, PowerupsLite"),
            "WhenPlugin.When.WaitIndefinitely, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.WaitIndefinitely, PowerupsLite"),
            "WhenPlugin.When.WaitUntilSafe, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.WaitUntilSafe, PowerupsLite"),
            "WhenPlugin.When.WhenUnsafe, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.WhenUnsafe, PowerupsLite"),

            "WhenPlugin.When.IfConstant, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.IfConstant, PowerupsLite"),
            "WhenPlugin.When.IfThenElse, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.IfThenElse, PowerupsLite"),
            "WhenPlugin.When.WhenSwitch, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.WhenSwitch, PowerupsLite"),
            "WhenPlugin.When.RepeatUntilAllSucceed, WhenPlugin" => (Upgrade.Lite, "PowerupsLite.When.RepeatUntilAllSucceed, PowerupsLite"),

            // Complex types
            "WhenPlugin.When.AddImagePattern, WhenPlugin" => (Upgrade.None, "WhenPlugin.When.AddImagePattern, WhenPlugin"), // No change),
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