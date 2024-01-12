#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using System;
using System.Threading.Tasks;

namespace NINA.Sequencer.Container {

    public interface ISequenceRootContainer : ISequenceContainer, ITriggerable {

        void AddRunningItem(ISequenceItem item);

        void RemoveRunningItem(ISequenceItem item);

        void SkipCurrentRunningItems();

        string SequenceTitle { get; set; }

        Task RaiseFailureEvent(ISequenceEntity sender, Exception ex);
        event Func<object, SequenceEntityFailureEventArgs, Task> FailureEvent;
    }
}