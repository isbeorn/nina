using CsvHelper;
using Microsoft.CodeAnalysis.FlowAnalysis;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Serialization {
    public class PowerupsUpgrader {

        private static ISequencerFactory sequencerFactory = null;

        private static T CreateNewInstruction<T>(string oldName) {
            var method = sequencerFactory.GetType().GetMethod(nameof(sequencerFactory.GetItem)).MakeGenericMethod(new Type[] { typeof(T) });
            T newObj = (T)method.Invoke(sequencerFactory, null);
            ((ISequenceItem)newObj).Name += " [SP: " + oldName;
            return newObj;
        }

        public static object UpgradeIntruction(JsonCreationConverter<ISequenceItem> conv, object obj) {

            if (sequencerFactory == null) {
                FieldInfo fi = conv.GetType().GetField("factory", BindingFlags.Instance | BindingFlags.NonPublic);
                sequencerFactory = (ISequencerFactory)fi.GetValue(conv);
            }

            ISequenceItem item = obj as ISequenceItem;
            Type t = item.GetType();
            switch (t.Name) {
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
                        item.Name += " [Powerups";
                        break;
                    }
            }
            return obj;
        }
    }
}
