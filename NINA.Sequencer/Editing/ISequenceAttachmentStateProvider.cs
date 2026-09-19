#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

#nullable enable

namespace NINA.Sequencer.Editing {
    /// <summary>Optional capture of configuration changed by parent attachment or detachment hooks.</summary>
    /// <remarks>
    /// <para>The journal already handles ordinary add, remove and move operations, including parents
    /// and positions. Implement this only when those operations also change configuration.</para>
    /// <para>Examples include a symbol identifier cleared by a scope collision, or explicit coordinate
    /// definitions replaced when entering a target container. Capture those affected values only.</para>
    /// <para>The journal restores the entity's placement through its normal hooks first, then restores
    /// this configuration. Inherited values and runtime state must remain live.</para>
    /// </remarks>
    public interface ISequenceAttachmentStateProvider {
        /// <summary>Captures configuration that parent hooks can change.</summary>
        /// <returns>
        /// An immutable snapshot of affected configuration, or <see langword="null"/> when there is
        /// no attachment-related configuration to capture.
        /// </returns>
        /// <remarks>
        /// Called before a structural edit and again if the captured configuration changed.
        /// Provide snapshots at both points when capturing a change. Do not include the parent,
        /// list position, execution progress or current variable values in the snapshot.
        /// </remarks>
        ISequenceEditSnapshot? CaptureAttachmentState();
    }
}