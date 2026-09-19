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
    /// Optional custom capture for bound fields whose configuration cannot be restored by ordinary
    /// single-property undo, or which must be excluded from configuration history.
    /// </summary>
    /// <remarks>
    /// <para>Ordinary scalar and expression-definition bindings are captured automatically. Most
    /// entities do not need this interface. For example, changing exposure time needs no provider.</para>
    /// <para>Implement this when one setter changes several configuration values, or a mutable value
    /// requires a custom copy and restore operation. Capture all affected values together.</para>
    /// <para>For example, selecting a calculated time provider also replaces the manual clock fields.
    /// Its snapshot must preserve the provider selection and original manual time. A coordinate edit
    /// can similarly need both the complete coordinates and the expression definitions it replaces.</para>
    /// <para>The editor owns transactions, no-op detection, conflict checks and replay. This provider
    /// supplies configuration snapshots only; it must not record entries or subscribe to runtime changes.</para>
    /// </remarks>
    public interface ISequenceCustomPropertyEditProvider {
        /// <summary>Captures custom configuration state for the addressed field, if needed.</summary>
        /// <param name="source">The resolved writable binding source, possibly a nested configuration object.</param>
        /// <param name="propertyName">The writable member on <paramref name="source"/>.</param>
        /// <param name="snapshot">
        /// An immutable copy of all configuration affected by this field. When returning
        /// <see langword="true"/>, <see langword="null"/> explicitly excludes the field from history.
        /// </param>
        /// <returns>
        /// <see langword="false"/> to keep automatic capture for this field;
        /// <see langword="true"/> with a snapshot to use custom capture; or
        /// <see langword="true"/> with <see langword="null"/> to exclude a runtime-only field.
        /// </returns>
        /// <remarks>
        /// Called before a user edit and, if configuration changed, after the binding commits.
        /// Supply a snapshot at both points for a custom field. Return <see langword="false"/>
        /// for ordinary fields, even when other fields on the same entity need custom capture.
        /// </remarks>
        bool TryCapturePropertyState(object source, string propertyName, out ISequenceEditSnapshot? snapshot);
    }
}