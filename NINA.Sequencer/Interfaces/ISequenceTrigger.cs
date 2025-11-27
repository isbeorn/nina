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
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger {

    public interface ISequenceTrigger : ISequenceEntity {

        /// <summary>
        /// Indicates if the trigger can be placed multiple times into an instruction set or should only be present once
        /// </summary>
        bool AllowMultiplePerSet { get; }

        /// <summary>
        /// Before each Sequence Item is executed this method will be called to determine if the trigger should be executed
        /// </summary>
        /// <param name="previousItem">The previous instruction that was executed</param>
        /// <param name="nextItem">The next instruction that will be executed next</param>
        /// <returns></returns>
        bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem);

        /// <summary>
        /// After each Sequence Item is executed this method will be called to determine if the trigger should be executed
        /// </summary>
        /// <param name="previousItem">The previous instruction that was executed</param>
        /// <param name="nextItem">The next instruction that will be executed next</param>
        /// <returns></returns>
        bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem);

        /// <summary>
        /// Runs the actual trigger logic
        /// </summary>
        /// <param name="context">The container of the next sequence item to be processed. As triggers get called in a cascade the item might be in a child container.</param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task Run(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token);
    }
}