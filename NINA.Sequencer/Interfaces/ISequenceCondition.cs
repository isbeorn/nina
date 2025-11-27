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
using System.Windows.Input;

namespace NINA.Sequencer.Conditions {

    public interface ISequenceCondition : ISequenceEntity {

        /// <summary>
        /// Indicates if the condition can be placed multiple times into an instruction set or should only be present once
        /// </summary>
        bool AllowMultiplePerSet { get; }

        /// <summary>
        /// Determine if the condition is satisfied or not. Will be called after each processed sequence item.
        /// </summary>
        /// <param name="previousItem">The previous instruction that was executed</param>
        /// <param name="nextItem">The next item that is scheduled to be processed</param>
        /// <returns></returns>
        bool RunCheck(ISequenceItem previousItem, ISequenceItem nextItem);

        /// <summary>
        /// Resets the progress of the condition. For example when nested in a container that restarts itself the condition needs to be reset
        /// </summary>
        void ResetProgress();

        ICommand ResetProgressCommand { get; }
    }
}