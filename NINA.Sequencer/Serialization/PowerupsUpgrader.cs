using Accord.Statistics.Kernels;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Namotion.Reflection;
using Newtonsoft.Json.Linq;
using Nikon;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.Dome;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Rotator;
using NINA.Sequencer.SequenceItem.Switch;
using NINA.Sequencer.SequenceItem.Telescope;
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

        private static T CreateNewItem<T>(ISequenceItem item) {
            var method = itemFactory.GetType().GetMethod(nameof(itemFactory.GetItem)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(itemFactory, null);
            ISequenceItem newItem = (ISequenceItem)newObj;
            newItem.Name += " [Was " + item.Name;
            newItem.Attempts = item.Attempts;
            newItem.ErrorBehavior = item.ErrorBehavior;
            return newObj;
        }
        private static T CreateNewContainer<T>(string oldName) {
            var method = containerFactory.GetType().GetMethod(nameof(containerFactory.GetContainer)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(containerFactory, null);
            ((ISequenceContainer)newObj).Name += " [Was " + oldName;
            return newObj;
        }

        private static T CreateNewCondition<T>(string oldName) {
            var method = containerFactory.GetType().GetMethod(nameof(conditionFactory.GetCondition)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(conditionFactory, null);
            ((ISequenceCondition)newObj).Name += " [Was " + oldName;
            return newObj;
        }

        private static T CreateNewTrigger<T>(string oldName) {
            var method = triggerFactory.GetType().GetMethod(nameof(triggerFactory.GetTrigger)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(triggerFactory, null);
            ((ISequenceTrigger)newObj).Name += " [Was " + oldName;
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

        private static string GetExpr(Type t, ISequenceEntity item, string propertyName) {
            PropertyInfo pi = t.GetProperty(propertyName);
            object expr = pi.GetValue(item);
            pi = expr.GetType().GetProperty("Expression");
            return pi.GetValue(expr) as string;
        }

        private static void PutExpr(Type t, ISequenceEntity item, string propertyName, string value) {
            PropertyInfo pi = t.GetProperty(propertyName);
            Expression expr = pi.GetValue(item) as Expression;
            expr.Definition = value;
        }

        public static void PreUpgradeInstruction(string originalType, JObject jObject) {
            switch (originalType) {
                case "WhenPlugin.When.GetArray, WhenPlugin":
                case "WhenPlugin.When.PutArray, WhenPlugin":
                    jObject.Add("iNameExpr", jObject["NameExpr"]);
                    jObject.Remove("NameExpr");
                    jObject.Add("iIExpr", jObject["IExpr"]);
                    jObject.Remove("IExpr");
                    jObject.Add("iVExpr", jObject["VExpr"]);
                    jObject.Remove("VExpr");
                    break;
                case "WhenPlugin.When.InitializeArray, WhenPlugin":
                    jObject.Add("iNameExpr", jObject["NameExpr"]);
                    jObject.Remove("NameExpr");
                    break;
            }
        }

        public static object UpgradeInstruction(object obj, JObject jObject) {

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
                } else if (trigger != null) {
                    t = trigger.GetType();
                } else {
                    t = item.GetType();
                }

                Logger.Info("Powerups Upgrade: " + t);
                switch (t.Name) {
                    // The following are updates from Powerups + instructions to NINA instructions
                    case "DitherAfterExposures": {
                            DitherAfterExposures newObj = CreateNewTrigger<DitherAfterExposures>(trigger.Name);
                            newObj.AfterExposuresExpression.Definition = GetExpr(t, trigger, "AfterExpr");
                            newObj.AttachNewParent(trigger.Parent);
                            return newObj; 
                        }
                    case "CoolCamera": {
                            CoolCamera newObj = CreateNewItem<CoolCamera>(item);
                            newObj.TemperatureExpression.Definition = GetExpr(t, item, "TempExpr");
                            newObj.DurationExpression.Definition = GetExpr(t, item, "DurExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "MoveRotatorMechanical": {
                            MoveRotatorMechanical newObj = CreateNewItem<MoveRotatorMechanical>(item);
                            newObj.MechanicalPositionExpression.Definition = GetExpr(t, item, "RExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "SetSwitchValue": {
                            SetSwitchValue newObj = CreateNewItem<SetSwitchValue>(item);
                            newObj.ValueExpression.Definition = GetExpr(t, item, "ValueExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "SlewToAltAz": {
                            SlewScopeToAltAz newObj = CreateNewItem<SlewScopeToAltAz>(item);
                            newObj.AzExpression.Definition = GetExpr(t, item, "AzExpr");
                            newObj.AltExpression.Definition = GetExpr(t, item, "AltExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "SlewToRADec": {
                            SlewScopeToRaDec newObj = CreateNewItem<SlewScopeToRaDec>(item);
                            newObj.RaExpression.Definition = GetExpr(t, item, "RAExpr");
                            newObj.DecExpression.Definition = GetExpr(t, item, "DecExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "Center": {
                            Center newObj = CreateNewItem<Center>(item);
                            newObj.RaExpression.Definition = GetExpr(t, item, "RAExpr");
                            newObj.DecExpression.Definition = GetExpr(t, item, "DecExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "MoveFocuserAbsolute": {
                            MoveFocuserAbsolute newObj = CreateNewItem<MoveFocuserAbsolute>(item);
                            newObj.PositionExpression.Definition = GetExpr(t, item, "PExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "MoveFocuserRelative": {
                            MoveFocuserRelative newObj = CreateNewItem<MoveFocuserRelative>(item);
                            newObj.RelativePositionExpression.Definition = GetExpr(t, item, "PExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "SlewDomeAzimuth": {
                            SlewDomeAzimuth newObj = CreateNewItem<SlewDomeAzimuth>(item);
                            newObj.AzimuthDegreesExpression.Definition = GetExpr(t, item, "AzExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "WaitForTimeSpan": {
                            WaitForTimeSpan newObj = CreateNewItem<WaitForTimeSpan>(item);
                            newObj.TimeExpression.Definition = GetExpr(t, item, "WaitExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "LoopWhile": {
                            LoopWhile newObj = CreateNewCondition<LoopWhile>(condition.Name);
                            newObj.PredicateExpression.Definition = GetExpr(t, condition, "PredicateExpr");
                            newObj.AttachNewParent(condition.Parent);
                            return newObj;
                        }
                    case "WaitUntil": {
                            WaitUntil newObj = CreateNewItem<WaitUntil>(item);
                            newObj.PredicateExpression.Definition = GetExpr(t, item, "PredicateExpr");
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "SwitchFilter": {
                            SwitchFilter newObj = CreateNewItem<SwitchFilter>(item);
                            PropertyInfo pi = t.GetProperty("FilterExpr");
                            newObj.ComboBoxText = (string)pi.GetValue(item);
                            newObj.AttachNewParent(item.Parent);
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
                            newObj.AttachNewParent(item.Parent);
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
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "TakeExposure": {
                            TakeExposure newObj = CreateNewItem<TakeExposure>(item);
                            newObj.ExposureTimeExpression.Definition = GetExpr(t, item, "EExpr");
                            newObj.GainExpression.Definition = GetExpr(t, item, "GExpr");
                            newObj.OffsetExpression.Definition = GetExpr(t, item, "OExpr");

                            PropertyInfo pi = t.GetProperty("Binning");
                            newObj.Binning = (BinningMode)pi.GetValue(item);
                            pi = t.GetProperty("ImageType");
                            newObj.ImageType = (string)pi.GetValue(item);
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "SetConstant": {
                            GlobalConstant newObj = CreateNewItem<GlobalConstant>(item);
                            PropertyInfo pi = t.GetProperty("Definition");
                            newObj.Expr.Definition = (string)pi.GetValue(item);
                            pi = t.GetProperty("Identifier");
                            newObj.Identifier = (string)pi.GetValue(item);
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "SetGlobalVariable":
                    case "SetVariable": {
                            GlobalVariable newObj = CreateNewItem<GlobalVariable>(item);
                            PropertyInfo pi = t.GetProperty("OriginalDefinition");
                            newObj.OriginalExpr.Definition = (string)pi.GetValue(item);
                            pi = t.GetProperty("Identifier");
                            newObj.Identifier = (string)pi.GetValue(item);
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "ResetVariable": {
                            ResetVariable newObj = CreateNewItem<ResetVariable>(item);
                            PropertyInfo pi = t.GetProperty("Variable");
                            newObj.Variable = (string)pi.GetValue(item);
                            pi = t.GetProperty("Expr");
                            object expr = pi.GetValue(item);
                            pi = expr.GetType().GetProperty("Expression");
                            string exp = pi.GetValue(expr) as string;
                            if (exp != null) {
                                newObj.Expr.Definition = exp;
                            }
                            newObj.AttachNewParent(item.Parent);
                            return newObj;
                        }
                    case "ResetVariableToDate": {
                            ResetVariableToDate newObj = CreateNewItem<ResetVariableToDate>(item);
                            PropertyInfo pi = t.GetProperty("Variable");
                            newObj.Variable = (string)pi.GetValue(item);
                            newObj.AttachNewParent(item.Parent);
                            pi = t.GetProperty("Hours");
                            newObj.Hours = (int)(pi.GetValue(item) as Int32?);
                            pi = t.GetProperty("Minutes");
                            newObj.Minutes = (int)(pi.GetValue(item) as Int32?);
                            pi = t.GetProperty("Seconds");
                            newObj.Seconds = (int)(pi.GetValue(item) as Int32?);
                            return newObj;
                        }
                    // The following are updates from Powerups to Powerups Lite
                    case "AddImagePattern": {
                            Type tt = Type.GetType("PowerupsLite.When.AddImagePattern, PowerupsLite");
                            var method = itemFactory.GetType().GetMethod(nameof(itemFactory.GetItem)).MakeGenericMethod(new Type[] { tt });
                            ISequenceItem newObj = (ISequenceItem)method.Invoke(itemFactory, null);
                            PropertyInfo pi = t.GetProperty("Identifier");
                            PropertyInfo npi = tt.GetProperty("Identifier");
                            npi.SetValue(newObj, pi.GetValue(item) as string);
                            PutExpr(tt, newObj, "ExprExpression", GetExpr(t, item, "Expr"));
                            pi = t.GetProperty("PatternDescription");
                            npi = tt.GetProperty("PatternDescription");
                            npi.SetValue(newObj, pi.GetValue(item) as string);
                            return newObj;
                        }
                    case "IfContainer": {
                            Type tt = Type.GetType("PowerupsLite.When.IfContainer, PowerupsLite");
                            var method = containerFactory.GetType().GetMethod(nameof(containerFactory.GetContainer)).MakeGenericMethod(new Type[] { tt });
                            ISequenceContainer newObj = (ISequenceContainer)method.Invoke(containerFactory, null);
                            ISequenceContainer ifc = (ISequenceContainer)obj;
                            foreach (ISequenceItem i in ifc.Items) {
                                ISequenceItem newItem = (ISequenceItem)i.Clone();
                                newObj.Items.Add(newItem);
                                newItem.AttachNewParent(newObj);
                            }
                            return newObj;
                        }
                    case "TemplateContainer": {
                            Type tt = Type.GetType("PowerupsLite.When.TemplateContainer, PowerupsLite");
                            var method = containerFactory.GetType().GetMethod(nameof(containerFactory.GetContainer)).MakeGenericMethod(new Type[] { tt });
                            ISequenceContainer newObj = (ISequenceContainer)method.Invoke(containerFactory, null);
                            ISequenceContainer ifc = (ISequenceContainer)obj;
                            foreach (ISequenceItem i in ifc.Items) {
                                ISequenceItem newItem = (ISequenceItem)i.Clone();
                                newObj.Items.Add(newItem);
                                newItem.AttachNewParent(newObj);
                            }
                            return newObj;
                        }
                    case "InitializeArray": {
                            PutExpr(t, item, "NameExprExpression", GetExpr(t, item, "iNameExpr"));
                            item.Name += " [3.2=>3.3";
                            return obj;
                        }
                    case "ForEachInArray": {
                            Type tt = Type.GetType("PowerupsLite.When.ForEachInArray, PowerupsLite");
                            var method = itemFactory.GetType().GetMethod(nameof(itemFactory.GetItem)).MakeGenericMethod(new Type[] { tt });
                            ISequenceItem newObj = (ISequenceItem)method.Invoke(itemFactory, null);
                            PropertyInfo pi = t.GetProperty("ValueVariable");
                            PropertyInfo npi = newObj.GetType().GetProperty("ValueVariable");
                            npi.SetValue(newObj, (string)pi.GetValue(item));
                            pi = t.GetProperty("IndexVariable");
                            npi = newObj.GetType().GetProperty("IndexVariable");
                            npi.SetValue(newObj, (string)pi.GetValue(item));
                            PutExpr(tt, newObj, "NameExprExpression", GetExpr(t, item, "NameExpr"));
                            newObj.Name += " [Lite";
                            return newObj;
                        }
                    case "GetArray": {
                            PutExpr(t, item, "NameExprExpression", GetExpr(t, item, "iNameExpr"));
                            PutExpr(t, item, "IExprExpression", GetExpr(t, item, "iIExpr"));
                            PutExpr(t, item, "VExprExpression", GetExpr(t, item, "iVExpr"));
                            item.Name += " [3.2=>3.3";
                            return obj;
                        }
                    case "PutArray": {
                            PutExpr(t, item, "NameExprExpression", GetExpr(t, item, "iNameExpr"));
                            PutExpr(t, item, "IExprExpression", GetExpr(t, item, "iIExpr"));
                            PutExpr(t, item, "VExprExpression", GetExpr(t, item, "iVExpr"));
                            item.Name += " [3.2=>3.3";
                            return obj;
                        }

                    case "IfConstant":
                    case "IfThenElse":
                    case "WhenSwitch":
                        Expression e = (Expression)item.GetType().GetProperty("PredicateExpression").GetValue(item, null);
                        if (jObject["IfExpr"] != null) {
                            e.Definition = jObject["IfExpr"]["Expression"].ToString();
                        }
                        item.Name += " [3.2=>3.3";
                        break;

                    case "IfTimeout":
                        item.Name += " [3.2=>3.3";
                        break;

                    // Unchanged (no Expressions)
                    case "DoFlip":
                    case "DIYMeridianFlip":
                    case "PassMeridian":
                    case "RotateImage":

                    case "WaitIndefinitely":
                    case "Breakpoint":
                    case "EndSequence":
                    case "IfFailed":
                    case "WhenUnsafe":
                        item.Name += " [3.2=>3.3";
                        break;

                    default: {
                            item.Name += " *MANUAL UPGRADE REQUIRED*";
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
