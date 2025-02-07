using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NINA.Sequencer.Container;
using NINA.Core.Utility;
using System.Data;
using NINA.Sequencer.Logic;
using System.Drawing;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Sequencer.Generators;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Define Variable")]
    [ExportMetadata("Description", "Creates a Variable whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "VariableSVG")]
    [ExportMetadata("Category", "Expressions")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [ExpressionObject]

    public partial class DefineVariable : Symbol {

        [ImportingConstructor]
        public DefineVariable() : base() {
            Name = Name;
            Icon = Icon;
        }
        public DefineVariable(DefineVariable copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public DefineVariable(string id, string def, ISequenceContainer parent) {
            DefineVariable sv = new DefineVariable();
            sv.AttachNewParent(parent);
            sv.Identifier = id;
            sv.Executed = true;
        }

        public override object Clone() {
            DefineVariable clone = new DefineVariable(this);

            clone.Identifier = Identifier;
            clone.Expr = Expr;
            if (Expr != null) {
                clone.Expr = new Expression(Expr.Definition, clone.Parent, this);
            }

            clone.OriginalExpr = OriginalExpr;
            if (OriginalExpr != null) {
                clone.OriginalExpr = new Expression(Expr.Definition, clone.Parent, this);
            } else {
                clone.OriginalExpr = new Expression("", clone.Parent, this);
            }
            return clone;
        }

        //public static void SetVariableReference(string id, string def, ISequenceContainer parent) {
        //    DefineVariable sv = new DefineVariable();
        //    sv.AttachNewParent(parent);
        //    sv.Identifier = id;

        //    if (def.StartsWith('@')) {
        //        sv.Definition = "'" + def.Substring(1) + "'";
        //        sv.Executed = true;
        //        return;
        //    }

        //    sv.Definition = def;
        //    sv.Executed = true;


        //    Symbol sym = Symbol.FindSymbol(def.Substring(1), parent);
        //    if (sym != null) {
        //        sv.Expr = sym.Expr;
        //        sv.IsReference = true;
        //    } else {
        //        throw new SequenceEntityFailedException("Call by reference symbol not found: " + def);
        //    }
        //}

        private bool iExecuted = false;
        public bool Executed {
            get => iExecuted;
            set {
                iExecuted = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public string OriginalDefinition {
            get => OriginalExpr?.Definition;
            set {
                OriginalExpr.Definition = value;
                RaisePropertyChanged("OriginalExpr");
            }
        }

        private Expression _originalExpr = null;
        public Expression OriginalExpr {
            get => _originalExpr;
            set {
                _originalExpr = value;
                RaisePropertyChanged();
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Expr = new Expression(Expr?.Definition ?? "", this);
            if (!Executed && Parent != null && Expr != null) {
                Expr.IsExpression = true;
                if (Expr.Definition.Length > 0) {
                    Expr.Error = "Not evaluated";
                }
            }
        }


        public override string ToString() {
            if (Expr != null) {
                return $"Variable: {Identifier}, Definition: {Expr.Definition}, Parent: {Parent?.Name}, Expr: {Expr}";

            } else {
                return $"Variable: {Identifier}, Parent: {Parent?.Name} Expr: null";
            }
        }

        public override bool Validate() {
            if (!IsAttachedToRoot()) return true;
            IList<string> i = new List<string>();

            if (Identifier.Length == 0 || OriginalExpr.Definition?.Length == 0) {
                i.Add("A name and an initial value must be specified");
            } else if (!Regex.IsMatch(Identifier, VALID_SYMBOL)) {
                i.Add("The name of a Constant must be alphanumeric");
            }

            if (!Executed) {
                OriginalExpr.Validate();
                if (Expr.Error != null) {
                    Expression.AddExprIssues(i, OriginalExpr);
                }
            } else {
                Expression.AddExprIssues(i, Expr, OriginalExpr);
            }

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
        }

        public override void ResetProgress() {
            base.ResetProgress();
            Executed = false;
            if (Expr != null) {
                Expr.Definition = "";
                Expr.IsExpression = true;
                Expr.Evaluate();
            }
            SymbolDirty(this);
        }


        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Debugging) {
                Logger.Info("Executing Vx");
                DumpSymbols();
            }
            Expr.Definition = OriginalExpr.Definition;
            Executed = true;
            Expr.Evaluate();

            //if (this is SetGlobalVariable) {
            //    // Find the one in Globals and set it
            //    Symbol global = FindGlobalSymbol(Identifier);
            //    if (global is SetGlobalVariable sgv) {

            //        // Bug fix
            //        foreach (var res in Expr.Resolved) {
            //            if (res.Value == null) {
            //                Expr.GlobalVolatile = true;
            //                break;
            //            }
            //        }

            //        sgv.Expr = Expr;
            //        sgv.Definition = Expr.Expression;
            //        sgv.Executed = true;
            //    }
            //}
            return Task.CompletedTask;
        }
    }
}
