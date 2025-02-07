#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Wait Until")]
    [ExportMetadata("Description", "Waits until the Expression is true.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [ExpressionObject]

    public partial class WaitUntil : SequenceItem, IValidatable {
        private ISafetyMonitorMediator safetyMonitorMediator;
        protected ISequenceMediator sequenceMediator;
        private IProfileService profileService;

        [ImportingConstructor]
        public WaitUntil(ISafetyMonitorMediator safetyMonitorMediator, ISequenceMediator seqMediator, IProfileService pService) {
            this.safetyMonitorMediator = safetyMonitorMediator;
            this.sequenceMediator = seqMediator;
            this.profileService = pService;
        }

        private WaitUntil(WaitUntil cloneMe) : this(cloneMe.safetyMonitorMediator, cloneMe.sequenceMediator, cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        [IsExpression]
        private double predicate;
 
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public TimeSpan WaitInterval { get; set; } = TimeSpan.FromSeconds(5);

        public bool Validate() {
            var i = new List<string>();
            
            Expression.AddExprIssues(i, PredicateExpression);
            
            Issues = i;
            return i.Count == 0;
        }
        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitUntil)}";
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            while (Parent != null) {
                PredicateExpression.Evaluate();
                if (!string.Equals(PredicateExpression.ValueString, "0", StringComparison.OrdinalIgnoreCase) && (PredicateExpression.Error == null)) {
                    break;
                }
                progress?.Report(new ApplicationStatus() { Status = "Waiting..." });
                await CoreUtil.Wait(WaitInterval, token, default);
            }
        }
    }
}