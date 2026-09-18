#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Core.Locale;
using System;

namespace NINA.Sequencer.Editing {
    /// <summary>A retained journal state with immutable edit details and observable sidebar markers.</summary>
    internal sealed partial class SequenceEditEntry : ObservableObject {
        public SequenceEditEntry(long state, ISequenceEdit edit = null) {
            State = state;
            Edit = edit;
            Description = edit?.Description ?? Loc.Instance["Lbl_SequenceHistory_Initial"];
            Details = (edit as ISequenceEditDetails)?.Details;
            Timestamp = edit == null ? null : DateTime.Now;
        }

        internal long State { get; }
        internal ISequenceEdit Edit { get; }
        public string Description { get; }
        public string Details { get; }
        public DateTime? Timestamp { get; }
        public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

        [ObservableProperty]
        private int position;

        [ObservableProperty]
        private bool isCurrent;

        [ObservableProperty]
        private bool isSaved;

        [ObservableProperty]
        private bool isApplied;

        internal void UpdateState(int index, int cursor, long? savedState) {
            Position = index;
            IsCurrent = index == cursor;
            IsSaved = State == savedState;
            IsApplied = index <= cursor;
        }
    }
}