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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Sequencer.SimpleSequence {

    public interface ISimpleExposure : ISequenceContainer, IDropContainer, IConditionable, ITriggerable, ISequenceItem, IImmutableContainer {
        bool Dither { get; set; }
        bool Enabled { get; set; }

        Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token);

        ISequenceTrigger GetDitherAfterExposures();

        ISequenceCondition GetLoopCondition();

        ISequenceItem GetSwitchFilter();

        ISequenceItem GetTakeExposure();

        void OnDeserializing(StreamingContext context);

        IImmutableContainer TransformToSmartExposure();
    }
}