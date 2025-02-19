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
using NINA.Sequencer.Container.ExecutionStrategy;
using Grpc.Core;
using NINA.Sequencer.SequenceItem.Expressions;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Container;
using NINA.Sequencer.Generators;

namespace NINA.Sequencer.Logic {
    [ExportMetadata("Name", "If")]
    [ExportMetadata("Description", "Executes an instruction set if the Expression is True (or 1)")]
    [ExportMetadata("Icon", "IfSVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class IfExpression : SequenceContainer, IValidatable, ITrueFalse {

        [ImportingConstructor]
        public IfExpression() : base(new SequentialStrategy()) {
        }

        public IfExpression(IfExpression copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
            }
        }

        partial void AfterClone(IfExpression clone) {
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            Logger.Info("Expression: " + HeaderExpression.Definition);
            if (string.IsNullOrEmpty(HeaderExpression.Definition)) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            try {
                // This code is for Image data
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
                    Logger.Info("Expression is true, " + HeaderExpression);
                    await base.Execute(progress, token);
                } else {
                    Logger.Info("Expression is false, " + HeaderExpression);
                    return;
                }
            } catch (ArgumentException ex) {
                Logger.Info("If error: " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
            }
        }


        public override void ResetProgress() {
            base.ResetProgress();
        }

        [IsExpression]
        public string header;


        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfExpression)}, Expr: {HeaderExpression}";
        }

        public IList<string> Switches { get; set; } = null;

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        public new bool Validate() {

            var i = new List<string>();

            Expression.ValidateExpressions(i, HeaderExpression);

            Issues = i;
            return i.Count == 0;
        }
    }
}
