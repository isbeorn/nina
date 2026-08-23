using Newtonsoft.Json;
using NINA.Core.Model;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Expressions {
    [ExportMetadata("Name", "Define Scoped Variable")]
    [ExportMetadata("Description", "Creates a Scoped Variable which can be used in Expressions")]
    [ExportMetadata("Icon", "VariableSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Symbol")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class Variable : UserSymbol {

        [ImportingConstructor]
        public Variable() : base() {
            Name = Name;
            Icon = Icon;
        }
        public Variable(Variable copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public Variable(string id, string def, ISequenceContainer parent) : base() {
            Identifier = id;
            Expr = new Expression(def, parent, this);
            Expr.Symbol = this;
            OriginalExpr = new Expression(def, parent, this);
            AttachNewParent(parent);
            Executed = true;
            Expr.Evaluate();
        }

        protected void PreClone(Variable clone) {
            clone.Identifier = Identifier;
            if (Expr != null) {
                clone.Expr = CloneExpression(Expr, clone.Parent, clone);
            }

            if (OriginalExpr != null) {
                clone.OriginalExpr = CloneExpression(OriginalExpr, clone.Parent, clone);
            } else {
                clone.OriginalExpr = new Expression("", clone.Parent, clone);
            }

        }

        private static Expression CloneExpression(Expression source, ISequenceEntity context, Variable owner) {
            Expression clone = new Expression("", context, owner) {
                AutoValue = source.AutoValue,
                Default = source.Default,
                DefaultString = source.DefaultString,
                IsValid = source.IsValid,
                Range = source.Range,
                SymbolBroker = source.SymbolBroker,
                Type = source.Type,
                Validator = source.Validator
            };
            clone.Definition = source.Definition;
            return clone;
        }

        public override object Clone() {
            Variable clone = new Variable(this);
            PreClone(clone);
            return clone;
        }

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
                RaisePropertyChanged();
            }
        }

        private Expression _originalExpr = null;
        public Expression OriginalExpr {
            get => _originalExpr;
            set {
                if (ReferenceEquals(_originalExpr, value)) {
                    return;
                }
                _originalExpr?.ReleaseConsumers();
                _originalExpr = value;
                RaisePropertyChanged();
            }
        }

        public override void AfterParentChanged() {
            // Don't remove a global variable if we're upgrading, or it will get removed inside Expression.FindGlobalSymbol()
            //if (!(this is GlobalVariable)) {
                base.AfterParentChanged();
            //}
            Expr = new Expression(Expr?.Definition ?? "", Parent, this);
            if (!Executed && Parent != null && Expr != null) {
                Expr.IsExpression = true;
                if (Expr.Definition.Length > 0) {
                    Expr.Error = "Not evaluated";
                }
            }
            OriginalExpr = new Expression(OriginalExpr?.Definition ?? "", Parent, this);
        }

        public override void ReleaseExpressionConsumers() {
            base.ReleaseExpressionConsumers();
            OriginalExpr?.ReleaseConsumers();
        }


        public override string ToString() {
            if (Expr != null) {
                return $"Variable: {Identifier}, Definition: {Expr.Definition}, Parent: {Parent?.Name}, Expr: {Expr}";

            } else {
                return $"Variable: {Identifier}, Parent: {Parent?.Name} Expr: null";
            }
        }

        public override bool Validate() {
            SymbolDictionary cached = null;
            IList<string> i = new List<string>();
            if (!IsAttachedToRoot()) {
                Issues = i;
                RaisePropertyChanged("Issues");
                return true;
            }

            if (Identifier.Length == 0 || OriginalExpr.Definition?.Length == 0) {
                i.Add("A name and an initial value must be specified");
            } else if (!UserSymbol.ValidSymbolRegex.IsMatch(Identifier)) {
                i.Add("The name of a Variable must be alphanumeric");
            }

            if (this is GlobalVariable && SymbolCache.TryGetValue(SParent(), out cached) && !cached.ContainsKey(Identifier)) {
                LastSParent = null;
                AfterParentChanged();
            }

            if (Expr == null) {
                Expression.ValidateExpressions(i, OriginalExpr);
            } else if (!Executed) {
                Expression.ValidateExpressions(i, Expr, OriginalExpr);
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
            Expr.Definition = OriginalExpr.Definition;
            Expr.Evaluate();
            if (Expr.Error != null && !Expression.JustWarnings(Expr.Error)) {
                throw new SequenceEntityFailedException("The value of the expression '" + Expr.Definition + "' was invalid");
            }
            if (!Expr.HasEvaluatedResult) {
                throw new SequenceEntityFailedException("The value of the expression '" + Expr.Definition + "' did not produce a valid result");
            }
            Executed = true;

            if (this is GlobalVariable) {
                // Find the one in Globals and set it
                UserSymbol global = FindGlobalSymbol(Identifier);
                if (global is GlobalVariable sgv) {

                    // Bug fix
                    foreach (var res in Expr.Resolved) {
                        if (res.Value == null) {
                            //res.GlobalVolatile = true;
                            break;
                        }
                    }

                    sgv.Expr = Expr;
                    sgv.Expr.Definition = Expr.Definition;
                    sgv.Executed = true;
                }
            }

            return Task.CompletedTask;
        }
    }
}
