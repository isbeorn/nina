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
    /// <summary>
    /// The captured configuration values needed to undo or redo an edit.
    /// </summary>
    /// <remarks>
    /// <para>Copy only affected configuration values. Do not capture execution progress, evaluated
    /// expression results, current variable values or completed equipment actions.</para>
    /// <para>Keep captured values immutable. Resolve live nested objects from their owning entity
    /// when checking or restoring; parent hooks can replace those objects.</para>
    /// </remarks>
    public interface ISequenceEditSnapshot {
        /// <summary>
        /// Gets a compact, localized description of the captured value for the history list,
        /// or <see langword="null"/> when unavailable.
        /// </summary>
        string? Description { get; }

        /// <summary>Gets whether the affected live configuration matches the captured values.</summary>
        /// <remarks>Check only the captured configuration, allowing unrelated runtime state to change.</remarks>
        bool IsCurrent { get; }

        /// <summary>Restores the captured configuration through normal editing paths.</summary>
        /// <remarks>
        /// Do not execute instructions or restore unrelated runtime state. The journal can call this
        /// after another restore partially failed, to compensate that failure with the opposite snapshot.
        /// Throw if the captured configuration cannot be restored.
        /// </remarks>
        void Restore();
    }
}