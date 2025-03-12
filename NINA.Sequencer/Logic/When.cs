using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Generators;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Logic
{
    [ExportMetadata("Name", "When")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_Guider_RestoreGuiding_Description")]
    [ExportMetadata("Icon", "GuiderSVG")]
    [ExportMetadata("Category", "Expressions")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class When : SequenceTrigger, IValidatable, ITrueFalse {

        public When() {
            TriggerRunner.Add(new WaitForTimeSpan() { Name = "Wait for Time Span" }); 
        }

        private When(When cloneMe) : this() {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(When clone) {
            clone.TriggerRunner = (SequentialContainer)TriggerRunner.Clone();
            clone.TriggerRunner.Name = "Instructions";
        }

        [IsExpression]
        private string predicate;

        public bool OnceOnly { get; set; } = false;

        public bool Interrupt { get; set; } = true;

        public IList<string> Issues => new List<string>();

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            throw new NotImplementedException();
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            return false;
        }

        public bool Validate() {

            Expression.ValidateExpressions(Issues, PredicateExpression);
            return Issues.Count == 0;
        }
    }
}
