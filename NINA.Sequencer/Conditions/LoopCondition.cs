#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Sequencer.SequenceItem;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Validations;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_LoopCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_LoopCondition_Description")]
    [ExportMetadata("Icon", "LoopSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class LoopCondition : SequenceCondition, IValidatable {

        [ImportingConstructor]
        public LoopCondition() {
        }

        private LoopCondition(LoopCondition cloneMe) : this() {
            CopyMetaData(cloneMe);
        }

        private int completedIterations;

        [JsonProperty]
        public int CompletedIterations {
            get => completedIterations;
            set {
                completedIterations = value;
                RaisePropertyChanged();
            }
        }

        public IList<string> Issues { get; private set; }

        [IsExpression(Default = 2, Range = [1, 0], HasValidator = true)]
        private int iterations;

        partial void IterationsExpressionValidator(Expression expr) {
            RaisePropertyChanged("Iterations");
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            var check = CompletedIterations < Iterations;
            if(!check && IsActive()) {
                Logger.Info($"{nameof(LoopCondition)} finished. Iterations: {CompletedIterations} / {Iterations}");
            }
            return check;
        }

        public override void ResetProgress() {
            CompletedIterations = 0;
            Status = Core.Enum.SequenceEntityStatus.CREATED;
        }

        public override void SequenceBlockFinished() {
            CompletedIterations++;
        }

        public override string ToString() {
            return $"Condition: {nameof(LoopCondition)}, Iterations: {CompletedIterations}/{Iterations}";
        }

        public bool Validate() {
            IList<string> issues = new List<string>();
            Expression.ValidateExpressions(issues, IterationsExpression);
            Issues = issues;
            return Issues.Count == 0;
        }
    }
}