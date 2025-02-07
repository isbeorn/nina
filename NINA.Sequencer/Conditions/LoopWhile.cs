using Newtonsoft.Json;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Utility;
using System.Windows.Controls;
using NINA.Core.Model;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Expressions;

namespace NINA.Sequencer.Conditions {
    [ExportMetadata("Name", "Loop While")]
    [ExportMetadata("Description", "Loops while the Expression is not false.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    [ExpressionObject]

    public partial class LoopWhile : SequenceCondition, IValidatable, ITrueFalse {

        [ImportingConstructor]
        public LoopWhile() {
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenFails, TimeSpan.FromSeconds(5));
        }

        public LoopWhile(LoopWhile copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
            }
        }

        [IsExpression]
        private double predicate;

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(LoopWhile)}, Predicate: {PredicateExpression.Definition}";
        }


        public IList<string> Issues { get; set; }

        public bool Validate() {

            var i = new List<string>();

            //Switches = Symbol.GetSwitches();
            //RaisePropertyChanged("Switches");

            Expression.AddExprIssues(i, PredicateExpression);

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
        }

        private bool Debugging = false;

        private void LogInfo(string str) {
            if (Debugging) {
                Logger.Info(str);
            }
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {

            if (string.IsNullOrEmpty(PredicateExpression.Definition)) {
                Logger.Warning("LoopWhile: Check, Predicate Expression is null or empty, " + PredicateExpression + " (Expression = " + PredicateExpression.Definition + ")");
                throw new SequenceEntityFailedException("LoopWhile, PredicateExpr is null or empty");
            }

            //if (!Symbol.SwitchWeatherConnectionStatusCurrent()) {
            //   Symbol.UpdateSwitchWeatherData();
            //}

            PredicateExpression.Evaluate();
           
            if (PredicateExpression.Error != null) {
                Logger.Warning("LoopWhile: Check, error in PredicateExpression: " + PredicateExpression.Error);
                throw new SequenceEntityFailedException(PredicateExpression.Error);
            } else {
                if (!string.Equals(PredicateExpression.ValueString, "0", StringComparison.OrdinalIgnoreCase)) {
                    Logger.Debug("LoopWhile, Predicate is true, " + PredicateExpression);
                    return true;
                } else {
                    Logger.Debug("LoopWhile, Predicate is false, " + PredicateExpression);
                    return false;
                }
            }
        }

        public override void AfterParentChanged() {
            if (Parent == null) {
                SequenceBlockTeardown();
            } else {
                if (Parent.Status == SequenceEntityStatus.RUNNING) {
                    SequenceBlockInitialize();
                }
            }
            PredicateExpression.Evaluate();
        }

        public override void SequenceBlockTeardown() {
            try { ConditionWatchdog?.Cancel(); } catch { }
        }

        public override void SequenceBlockInitialize() {
            ConditionWatchdog?.Start();
        }

        public IList<string> Switches { get; set; } = null;

        private async Task InterruptWhenFails() {
 
            if (!Check(null, null)) {
                if (this.Parent != null) {
                    if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                        Logger.Info("Expression returned false - Interrupting current Instruction Set");
                        await Parent.Interrupt();
                    }
                }
            }
        }

    }
}
