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
using NINA.Core.Utility.Notification;
using NINA.Sequencer;
using NINA.Sequencer.Editing;
using System.ComponentModel;

namespace NINA.ViewModel.Sequencer {
    internal partial class Sequence2VM {
        [ObservableProperty]
        private SequenceEditHistory editHistory;

        private void ObserveEditHistory(ISequencer previous, ISequencer current) {
            if (previous is INotifyPropertyChanged old) old.PropertyChanged -= HistoryRootChanged;
            if (current is INotifyPropertyChanged next) next.PropertyChanged += HistoryRootChanged;
            ResetEditHistory(current);
        }

        private void HistoryRootChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(ISequencer.MainContainer) && !ReferenceEquals(EditHistory?.Root, ((ISequencer)sender).MainContainer)) {
                if (EditHistory?.Root is INotifyPropertyChanged old) old.PropertyChanged -= Sequencer_PropertyChanged;
                AttachSequencerINPC();
                ResetEditHistory((ISequencer)sender);
            }
        }

        private void ResetEditHistory(ISequencer current) {
            EditHistory?.Dispose();
            EditHistory = current?.MainContainer == null ? null : new SequenceEditHistory(current.MainContainer) { IsEnabled = !IsLocked };
            if (EditHistory != null) EditHistory.ReplayFailed += message => Notification.ShowError(message);
        }

        partial void OnIsLockedChanged(bool value) {
            EditHistory?.Flush();
            if (EditHistory != null) EditHistory.IsEnabled = !value;
        }
    }
}