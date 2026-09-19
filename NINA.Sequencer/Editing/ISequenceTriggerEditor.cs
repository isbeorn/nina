#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Container;
using System.Collections.Generic;

namespace NINA.Sequencer.Editing {
    /// <summary>Optional trigger capability for editable action sets beyond the standard TriggerRunner.</summary>
    public interface ISequenceTriggerEditor {
        /// <summary>
        /// Return existing editor-owned containers, excluding materialized runtime actions. The
        /// journal uses these for property ownership, structural edits and template-session routing.
        /// </summary>
        IEnumerable<ISequenceContainer> GetAdditionalEditorContainers();
    }
}