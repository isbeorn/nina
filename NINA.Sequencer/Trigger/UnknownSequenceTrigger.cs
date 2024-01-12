#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger {

    internal class UnknownSequenceTrigger : SequenceTrigger, IValidatable {

        public UnknownSequenceTrigger() {
        }

        public UnknownSequenceTrigger(string token) {
            base.Name = token;
        }

        public new string Name {
            get => $"<{Loc.Instance["LblUnknownInstruction"]} - {base.Name}> ";
            private set => base.Name = value;
        }

        public IList<string> Issues => new List<string>() { Loc.Instance["LblUnknownInstructionValidation"] };

        public override object Clone() {
            return new UnknownSequenceTrigger() {
                Name = base.Name
            };
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            throw new SequenceItemSkippedException($"Unknown trigger will be skipped - ${Name}");
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            return false;
        }

        public bool Validate() {
            return false;
        }
    }
}