#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger {

    public interface ITriggerable {
        IList<ISequenceTrigger> Triggers { get; }

        void Add(ISequenceTrigger trigger);

        ICollection<ISequenceTrigger> GetTriggersSnapshot();

        bool Remove(ISequenceTrigger trigger);

        Task RunTriggers(ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token);

        Task RunTriggersAfter(ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token);
    }
}