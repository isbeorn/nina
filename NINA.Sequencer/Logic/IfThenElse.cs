using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Core.Utility;
using System.Text;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.Generators;

namespace NINA.Sequencer.Logic {
    [ExportMetadata("Name", "If/Then/Else")]
    [ExportMetadata("Description", "Executes the first instruction set if the Expression is True (or 1); the second if False (or 0)")]
    [ExportMetadata("Icon", "IfSVG")]
    [ExportMetadata("Category", "Expressions")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class IfThenElse : SequenceContainer, IValidatable, ITrueFalse {
        [ImportingConstructor]
        public IfThenElse() : base(new SequentialStrategy()) {
            ThenContainer = new SequentialContainer();
            ElseContainer = new SequentialContainer();
        }

        public IfThenElse(IfThenElse copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                ThenContainer = (SequentialContainer)copyMe.ThenContainer.Clone();
                ThenContainer.Name = "Then";
                Add(ThenContainer);
                ElseContainer = (SequentialContainer)copyMe.ElseContainer.Clone();
                ElseContainer.Name = "Else";
                Add(ElseContainer);
            }
        }

        [IsExpression]
        private string header;

        [JsonProperty]
        public SequentialContainer ThenContainer { get; set; }
        [JsonProperty]
        public SequentialContainer ElseContainer { get; set; }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            Logger.Info("Execute, Predicate: " + HeaderExpression.Definition);
            if (string.IsNullOrEmpty(HeaderExpression.Definition)) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            try {
                //if (IfExpr.ImageVolatile) {
                //    Logger.Info("ImageVolatile");
                //    while (TakeExposure.LastImageProcessTime < TakeExposure.LastExposureTIme) {
                //        Logger.Info("Waiting 250ms for processing...");
                //        progress?.Report(new ApplicationStatus() { Status = "" });
                //        await CoreUtil.Wait(TimeSpan.FromMilliseconds(250), token, default);
                //    }
                //    // Get latest values
                //    Logger.Info("ImageVolatile, new data");
                //}

                HeaderExpression.Evaluate();

                if (!string.Equals(HeaderExpression.ValueString, "0", StringComparison.OrdinalIgnoreCase) && (HeaderExpression.Error == null)) {
                    Logger.Info("Predicate is true; running Then");
                    await ThenContainer.Execute(progress, token);
                } else {
                    Logger.Info("Predicate is false; running Else");
                    await ElseContainer.Execute(progress, token);
                }
            } catch (ArgumentException ex) {
                Logger.Info("If error: " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfThenElse)}, Expr: {HeaderExpression}";
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        public new bool Validate() {

            var i = new List<string>();

            ThenContainer.Validate();
            i.AddRange(ThenContainer.Issues);
            ElseContainer.Validate();
            i.AddRange(ElseContainer.Issues);

            Expression.ValidateExpressions(i, HeaderExpression);

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
        }

    }
}
