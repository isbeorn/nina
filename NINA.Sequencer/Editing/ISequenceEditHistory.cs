#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Sequencer.Editing {
    /// <summary>
    /// Optional integration for custom sequence editors. Register configuration edits that have
    /// already been applied, on the editor dispatcher. Do not register equipment or runtime actions.
    /// Obtain the current history with SequenceEditContext.GetHistory on an editor element.
    /// </summary>
    public interface ISequenceEditHistory {
        IDisposable BeginTransaction(string description);
        void RecordApplied(ISequenceEdit edit);
    }

    /// <summary>
    /// A reversible configuration change. Replay must preserve entity identity and must not execute
    /// instructions. Implementations must restore their starting state if a replay operation throws.
    /// </summary>
    public interface ISequenceEdit {
        string Description { get; }
        bool CanUndo { get; }
        bool CanRedo { get; }
        void Undo();
        void Redo();
    }
}