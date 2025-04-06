using Accord.Statistics.Kernels;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Nikon;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.Dome;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Rotator;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Guider;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace NINA.Sequencer.Serialization {
    public class PowerupsUpgrader {

        private static ISequencerFactory itemFactory = null;
        private static ISequencerFactory containerFactory = null;
        private static ISequencerFactory conditionFactory = null;
        private static ISequencerFactory triggerFactory = null;

        private static T CreateNewInstruction<T>(string oldName) {
            var method = itemFactory.GetType().GetMethod(nameof(itemFactory.GetItem)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(itemFactory, null);
            // For now...
            ((ISequenceItem)newObj).Name += " [SP: " + oldName;
            return newObj;
        }
        private static T CreateNewContainer<T>(string oldName) {
            var method = containerFactory.GetType().GetMethod(nameof(containerFactory.GetContainer)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(containerFactory, null);
            // For now...
            ((ISequenceContainer)newObj).Name += " [SP: " + oldName;
            return newObj;
        }

        private static T CreateNewCondition<T>(string oldName) {
            var method = containerFactory.GetType().GetMethod(nameof(conditionFactory.GetCondition)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(conditionFactory, null);
            // For now...
            ((ISequenceCondition)newObj).Name += " [SP: " + oldName;
            return newObj;
        }

        private static T CreateNewTrigger<T>(string oldName) {
            var method = triggerFactory.GetType().GetMethod(nameof(triggerFactory.GetTrigger)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(triggerFactory, null);
            // For now...
            ((ISequenceTrigger)newObj).Name += " [SP: " + oldName;
            return newObj;
        }

        public static void RegisterContainerConverter(JsonCreationConverter<ISequenceContainer> conv) {
            if (containerFactory == null) {
                FieldInfo fi = conv.GetType().GetField("factory", BindingFlags.Instance | BindingFlags.NonPublic);
                containerFactory = (ISequencerFactory)fi.GetValue(conv);
            }
        }

        public static void RegisterItemConverter(JsonCreationConverter<ISequenceItem> conv) {
            if (itemFactory == null) {
                FieldInfo fi = conv.GetType().GetField("factory", BindingFlags.Instance | BindingFlags.NonPublic);
                itemFactory = (ISequencerFactory)fi.GetValue(conv);
            }
        }
        public static void RegisterConditionConverter(JsonCreationConverter<ISequenceCondition> conv) {
            if (conditionFactory == null) {
                FieldInfo fi = conv.GetType().GetField("factory", BindingFlags.Instance | BindingFlags.NonPublic);
                conditionFactory = (ISequencerFactory)fi.GetValue(conv);
            }
        }
        public static void RegisterTriggerConverter(JsonCreationConverter<ISequenceTrigger> conv) {
            if (triggerFactory == null) {
                FieldInfo fi = conv.GetType().GetField("factory", BindingFlags.Instance | BindingFlags.NonPublic);
                triggerFactory = (ISequencerFactory)fi.GetValue(conv);
            }
        }

        private static string GetExpr(Type t, ISequenceEntity item, string name) {
            PropertyInfo pi = t.GetProperty(name);
            object expr = pi.GetValue(item);
            pi = expr.GetType().GetProperty("Expression");
            return pi.GetValue(expr) as string;

        }

        public static object UpgradeInstruction(object obj) {

            try {
                ISequenceItem item = obj as ISequenceItem;
                ISequenceCondition condition = obj as ISequenceCondition;
                ISequenceTrigger trigger = obj as ISequenceTrigger;
                if (item == null && condition == null && trigger == null) {
                    return obj;
                }
                Type t;
                if (condition != null) {
                    t = condition.GetType();
                } else {
                    t = item.GetType();
                }

                Logger.Info("Upgrade: " + t);
                switch (t.Name) {
                    case "DitherAfterExposures": {
                            DitherAfterExposures newObj = CreateNewTrigger<DitherAfterExposures>(trigger.Name);
                            newObj.AfterExposuresExpression.Definition = GetExpr(t, trigger, "AfterExpr");
                            return 
                        }
                    case "CoolCamera": {
                            CoolCamera newObj = CreateNewInstruction<CoolCamera>(item.Name);
                            newObj.TemperatureExpression.Definition = GetExpr(t, item, "TempExpr");
                            newObj.DurationExpression.Definition = GetExpr(t, item, "DurExpr");
                            return newObj;
                        }
                    case "MoveRotatorMechanical": {
                            MoveRotatorMechanical newObj = CreateNewInstruction<MoveRotatorMechanical>(item.Name);
                            newObj.MechanicalPositionExpression.Definition = GetExpr(t, item, "RExpr");
                            return newObj;
                        }
                    case "SlewDomeAzimuth": {
                            SlewDomeAzimuth newObj = CreateNewInstruction<SlewDomeAzimuth>(item.Name);
                            newObj.AzimuthDegreesExpression.Definition = GetExpr(t, item, "AzExpr");
                            return newObj;
                        }
                    case "WaitForTimeSpan": {
                            WaitForTimeSpan newObj = CreateNewInstruction<WaitForTimeSpan>(item.Name);
                            newObj.TimeExpression.Definition = GetExpr(t, item, "WaitExpr");
                            return newObj;
                        }
                    case "LoopWhile": {
                            LoopWhile newObj = CreateNewCondition<LoopWhile>(condition.Name);
                            newObj.PredicateExpression.Definition = GetExpr(t, condition, "PredicateExpr");
                            return newObj;
                        }
                    case "WaitUntil": {
                            WaitUntil newObj = CreateNewInstruction<WaitUntil>(item.Name);
                            newObj.PredicateExpression.Definition = GetExpr(t, item, "PredicateExpr");
                            return newObj;
                        }
                    case "SwitchFilter": {
                            SwitchFilter newObj = CreateNewInstruction<SwitchFilter>(item.Name);
                            PropertyInfo pi = t.GetProperty("FilterExpr");
                            newObj.ComboBoxText = (string)pi.GetValue(item);
                            return newObj;
                        }
                    case "SmartExposure": {
                            SmartExposure newObj = CreateNewContainer<SmartExposure>(item.Name);
                            ((LoopCondition)newObj.Conditions[0]).IterationsExpression.Definition = GetExpr(t, item, "IterExpr");
                            ISequenceContainer smart = item as ISequenceContainer;
                            TakeExposure oldTe = (TakeExposure)smart.Items[1];
                            TakeExposure newTe = (TakeExposure)newObj.Items[1];
                            newTe.ExposureTimeExpression.Definition = oldTe?.ExposureTimeExpression.Definition;
                            newTe.GainExpression.Definition = oldTe?.GainExpression.Definition;
                            newTe.OffsetExpression.Definition = oldTe?.OffsetExpression.Definition;
                            newTe.Binning = oldTe?.Binning;
                            newTe.ImageType = oldTe?.ImageType;
                            SwitchFilter oldSf = (SwitchFilter)smart.Items[0];
                            SwitchFilter newSf = (SwitchFilter)newObj.Items[0];
                            newSf.ComboBoxText = oldSf.ComboBoxText;
                            // Dither?
                            return newObj;
                        }
                    case "TakeManyExposures": {
                            TakeManyExposures newObj = CreateNewContainer<TakeManyExposures>(item.Name);
                            ((LoopCondition)newObj.Conditions[0]).IterationsExpression.Definition = GetExpr(t, item, "IterExpr");
                            ISequenceContainer smart = item as ISequenceContainer;
                            TakeExposure oldTe = (TakeExposure)smart.Items[0];
                            TakeExposure newTe = (TakeExposure)newObj.Items[0];
                            newTe.ExposureTimeExpression.Definition = oldTe?.ExposureTimeExpression.Definition;
                            newTe.GainExpression.Definition = oldTe?.GainExpression.Definition;
                            newTe.OffsetExpression.Definition = oldTe?.OffsetExpression.Definition;
                            newTe.Binning = oldTe?.Binning;
                            newTe.ImageType = oldTe?.ImageType;
                            return newObj;
                        }
                    case "TakeExposure": {
                            TakeExposure newObj = CreateNewInstruction<TakeExposure>(item.Name);
                            newObj.ExposureTimeExpression.Definition = GetExpr(t, item, "EExpr");
                            newObj.GainExpression.Definition = GetExpr(t, item, "GExpr");
                            newObj.OffsetExpression.Definition = GetExpr(t, item, "OExpr");

                            PropertyInfo pi = t.GetProperty("Binning");
                            newObj.Binning = (BinningMode)pi.GetValue(item);
                            pi = t.GetProperty("ImageType");
                            newObj.ImageType = (string)pi.GetValue(item);
                            return newObj;
                        }
                    case "SetConstant": {
                            DefineConstant newObj = CreateNewInstruction<DefineConstant>(item.Name);
                            PropertyInfo pi = t.GetProperty("Definition");
                            newObj.Expr.Definition = (string)pi.GetValue(item);
                            pi = t.GetProperty("Identifier");
                            newObj.Identifier = (string)pi.GetValue(item);
                            return newObj;
                        }
                    case "SetVariable": {
                            DefineVariable newObj = CreateNewInstruction<DefineVariable>(item.Name);
                            PropertyInfo pi = t.GetProperty("OriginalDefinition");
                            newObj.OriginalExpr.Definition = (string)pi.GetValue(item);
                            pi = t.GetProperty("Identifier");
                            newObj.Identifier = (string)pi.GetValue(item);
                            return newObj;
                        }
                    case "ResetVariable": {
                            ResetVariable newObj = CreateNewInstruction<ResetVariable>(item.Name);
                            PropertyInfo pi = t.GetProperty("Variable");
                            newObj.Variable = (string)pi.GetValue(item);
                            pi = t.GetProperty("Expr");
                            object expr = pi.GetValue(item);
                            pi = expr.GetType().GetProperty("Expression");
                            string exp = pi.GetValue(expr) as string;
                            if (exp != null) {
                                newObj.Expr.Definition = exp;
                            }
                            return newObj;
                        }
                    default: {
                            item.Name += " MANUAL UPGRADE REQUIRED";
                            break;
                        }
                }
                return obj;
            } catch (Exception ex) {
                Logger.Error(ex);
                return obj;
            }
        }
    }
}
