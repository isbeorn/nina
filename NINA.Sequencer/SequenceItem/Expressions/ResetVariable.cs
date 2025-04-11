using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.Container;
using NINA.Core.Utility;
using System.Text.RegularExpressions;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Set Variable")]
    [ExportMetadata("Description", "If the Variable has been previously defined, its value will become the result of the Expression")]
    [ExportMetadata("Icon", "VariableSVG")]
    [ExportMetadata("Category", "Expressions")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ResetVariable : SequenceItem, IValidatable {
        [ImportingConstructor]


        public ResetVariable() {
            Icon = Icon;
            Expr = new Expression("", Parent);
        }

        public ResetVariable(ResetVariable copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
            }
        }

        public override object Clone() {
            ResetVariable clone = new ResetVariable(this) { };
            clone.Expr = new Expression(Expr.Definition, clone.Parent);
            clone.Variable = Variable;
            return clone;
        }

        private Expression _Expr = null;

        [JsonProperty]
        public Expression Expr {
            get => _Expr;
            set {
                _Expr = value;
                RaisePropertyChanged();
            }
        }

        private string variable;

        [JsonProperty]
        public string Variable {
            get => variable;
            set {
                if (value == variable) {
                    return;
                }
                variable = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Bunch of reasons the instruction might be invalid
            if (Issues.Count != 0) {
                throw new SequenceEntityFailedException("The instruction is invalid");
            }
            Expr.Evaluate();

            // Find Symbol, make sure it's valid
            UserSymbol sym = UserSymbol.FindSymbol(Variable, Parent);
            if (sym == null || !(sym is Variable)) {
                throw new SequenceEntityFailedException("The symbol isn't found or isn't a Variable");
            } else if (Expr.Error != null && !Expression.JustWarnings(Expr.Error)) {
                throw new SequenceEntityFailedException("The value of the expression '" + Expr.Definition + "' was invalid");
            }
            Variable sv = sym as Variable;
            if (sv == null || sv.Executed == false) {
                throw new SequenceEntityFailedException("The Variable definition has not been executed");
            }

            string oldDefinition = sym.Expr.Definition;

            if (Expr.StringValue != null) {
                sym.Expr.Error = null;
                sym.Expr.Definition = "'" + Expr.StringValue + "'";
            } else {
                sym.Expr.Definition = Expr.Value.ToString();
            }

            Logger.Info("SetVariable: " + Variable + " from " + oldDefinition + " to " + sym.Expr.Definition);

            // Make sure references are updated
            UserSymbol.SymbolDirty(sym);

            return Task.CompletedTask;
        }

        private bool IsAttachedToRoot() {
            ISequenceContainer p = Parent;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                }
                p = p.Parent;
            }
            return false;
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Expr.Context = this;
            Expr.Evaluate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ResetVariable)}, Variable: {variable}, Expr: {Expr}";
        }

        public bool Validate() {
            if (!IsAttachedToRoot()) return true;

            var i = new List<string>();
            if (Expr.Definition.Length == 0 || Variable == null || Variable.Length == 0) {
                i.Add("The variable and new value expression must both be specified");
            } else if (Variable.Length > 0 && !Regex.IsMatch(Variable, UserSymbol.VALID_SYMBOL)) {
                i.Add("'" + Variable + "' is not a legal Variable name");
            } else {
                UserSymbol sym = UserSymbol.FindSymbol(Variable, Parent);
                if (sym == null) {
                    i.Add("The Variable '" + Variable + "' is not in scope.");
                } else if (sym is Constant) {
                    i.Add("The symbol '" + Variable + "' is a Constant and may not be used with this instruction");
                }
            }

            Expr.Evaluate();

            Issues = i;
            return Issues.Count == 0;
        }
    }
}
