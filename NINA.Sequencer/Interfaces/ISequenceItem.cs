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
using NINA.Sequencer.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Sequencer.SequenceItem {

    public interface ISequenceItem : ISequenceEntity, ISequenceHasChanged{

        /// <summary>
        /// The actual logic when the sequence item should be executed
        /// </summary>
        /// <param name="progress">The handle to report back the progress to the application</param>
        /// <param name="token">Cancellation token to interrupt the sequence</param>
        /// <returns></returns>
        Task Run(IProgress<ApplicationStatus> progress, CancellationToken token);

        /// <summary>
        /// Resets the progress of the sequence item
        /// </summary>
        void ResetProgress();

        /// <summary>
        /// Resets the progress of the sequence item and its parent (and the parent of parent etc.)
        /// </summary>
        void ResetProgressCascaded();

        /// <summary>
        /// Sets the progress of the item to skipped
        /// </summary>
        void Skip();

        /// <summary>
        /// Should return a rough estimate of the sequence item duration to be used for conditions and triggers
        /// </summary>
        /// <returns></returns>
        TimeSpan GetEstimatedDuration();

        ICommand ResetProgressCommand { get; }

        /// <summary>
        /// Defines the instruction behavor in case of an error
        /// </summary>
        InstructionErrorBehavior ErrorBehavior { get; set; }

        /// <summary>
        /// How many times the instruction should retry in case of an error
        /// </summary>
        int Attempts { get; set; }
    }
}