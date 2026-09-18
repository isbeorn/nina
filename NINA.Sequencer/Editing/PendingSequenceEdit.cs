#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NINA.Sequencer.Editing {
    /// <summary>One input gesture owns its capture, popup subscriptions and deferred wheel commit.</summary>
    internal sealed class PendingSequenceEdit : IDisposable {
        private readonly Action requestCommit;
        private DispatcherTimer wheelTimer;
        private bool disposed;
        public PendingSequenceEdit(SequenceEditBinding binding, Action requestCommit) {
            Binding = binding;
            this.requestCommit = requestCommit;
            if (Binding.Editor is ComboBox combo) combo.DropDownClosed += PopupClosed;
            if (Binding.Editor is DatePicker date) date.CalendarClosed += PopupClosed;
        }
        public SequenceEditBinding Binding { get; }
        public bool Interacted { get; private set; }
        public bool IsCommitting { get; private set; }
        public bool HasOpenPopup => Binding.Editor is ComboBox { IsDropDownOpen: true } or DatePicker { IsDropDownOpen: true };
        public void MarkInteracted() {
            Interacted = true;
            Binding.Session.HasPendingEdit = true;
        }
        public void CommitAfterWheelGesture() {
            wheelTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Background,
                (_, _) => { wheelTimer.Stop(); requestCommit(); }, Binding.Editor.Dispatcher);
            wheelTimer.Stop();
            wheelTimer.Start();
        }
        private void PopupClosed(object sender, EventArgs e) => requestCommit();
        public void Commit() {
            if (disposed || IsCommitting || !Interacted || !Binding.Session.IsRecording) return;
            IsCommitting = true;
            try {
                Binding.UpdateSource();
                Binding.Session.RecordApplied(Binding.Capture.Complete());
            } catch (Exception ex) { Logger.Error("Unable to record sequence edit", ex); }
            finally { IsCommitting = false; }
        }
        public void Dispose() {
            if (disposed) return;
            disposed = true;
            wheelTimer?.Stop();
            if (Binding.Editor is ComboBox combo) combo.DropDownClosed -= PopupClosed;
            if (Binding.Editor is DatePicker date) date.CalendarClosed -= PopupClosed;
            Binding.Session.HasPendingEdit = false;
        }
    }
}