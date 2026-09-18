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
using System.ComponentModel;

namespace NINA.Sequencer.Editing {
    /// <summary>
    /// Optional boundary for generated/read-only contents that have their own temporary edit session.
    /// The container instance belongs to its parent's history. Only its contents use this session.
    /// Raise PropertyChanged for IsEditing when opening, saving or cancelling the session.
    /// </summary>
    public interface ISequenceEditSessionBoundary : ISequenceContainer, INotifyPropertyChanged {
        bool IsEditing { get; }
    }
}