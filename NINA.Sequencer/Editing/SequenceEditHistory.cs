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
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace NINA.Sequencer.Editing {
    internal sealed partial class SequenceEditHistory : ObservableObject, ISequenceEditHistory, IDisposable {
        private const int Capacity = 100;
        // Entry zero is the baseline. Each following entry owns the edit that reaches its state.
        private readonly ObservableCollection<SequenceEditEntry> entries = new();
        private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private readonly List<ISequenceEdit> pending = new();
        private int position;
        private int transactionDepth;
        private int captureDepth;
        private string transactionDescription;
        private long nextState;
        private long? savedState;
        private bool replaying;
        private bool enabled = true;
        private bool disposed;
        private bool hasPendingEdit;

        public SequenceEditHistory(ISequenceContainer root) {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Graph = new SequenceEditorGraph(root);
            entries.Add(new SequenceEditEntry(nextState));
            Entries = new ReadOnlyObservableCollection<SequenceEditEntry>(entries);
            UndoCommand = new RelayCommand(() => Undo(), () => CanUndo);
            RedoCommand = new RelayCommand(() => Redo(), () => CanRedo);
            MoveToCommand = new RelayCommand<SequenceEditEntry>(entry => {
                Flush();
                // Committing a field can evict an older entry and change the selected row's index.
                if (entry != null && entries.Contains(entry)) MoveToPosition(entry.Position);
            }, entry => IsEnabled && entry != null);
            SequenceEditContext.Register(this);
            Refresh();
        }

        public ISequenceContainer Root { get; }
        internal SequenceEditorGraph Graph { get; }
        public ReadOnlyObservableCollection<SequenceEditEntry> Entries { get; }
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }
        public RelayCommand<SequenceEditEntry> MoveToCommand { get; }
        public int Position => position;
        public bool CanUndo => IsEnabled && !replaying && (position > 0 || hasPendingEdit);
        public bool CanRedo => IsEnabled && !replaying && position < entries.Count - 1;
        public string UndoDescription => position > 0 ? string.Format(Loc.Instance["Lbl_SequenceHistory_UndoAction"], entries[position].Description) : Loc.Instance["Lbl_SequenceHistory_Undo"];
        public string RedoDescription => position < entries.Count - 1 ? string.Format(Loc.Instance["Lbl_SequenceHistory_RedoAction"], entries[position + 1].Description) : Loc.Instance["Lbl_SequenceHistory_Redo"];
        public bool IsEnabled {
            get => enabled && !disposed;
            set {
                if (enabled == value) return;
                enabled = value;
                foreach (SequenceEditHistory history in templateHistories.Values) history.IsEnabled = value;
                RefreshCommands();
            }
        }

        internal event Action FlushRequested;
        internal event Action<string> ReplayFailed;
        internal bool IsRecording => IsEnabled && !replaying && dispatcher.CheckAccess();
        internal bool HasPendingEdit {
            get => hasPendingEdit;
            set {
                if (hasPendingEdit == value) return;
                hasPendingEdit = value;
                RefreshCommands();
            }
        }

        internal void Flush() => FlushRequested?.Invoke();

        public IDisposable BeginTransaction(string description) {
            dispatcher.VerifyAccess();
            if (!IsRecording) return EditScope.Empty;
            if (transactionDepth == 0) {
                Flush();
                transactionDescription = description;
            }
            transactionDepth++;
            return new EditScope(() => {
                if (--transactionDepth == 0 && pending.Count > 0) {
                    ISequenceEdit edit = pending.Count == 1 ? pending[0] : new CompositeSequenceEdit(transactionDescription, pending.ToArray());
                    pending.Clear();
                    Append(edit);
                }
            });
        }

        public void RecordApplied(ISequenceEdit edit) {
            dispatcher.VerifyAccess();
            if (!IsRecording || edit == null) return;
            if (transactionDepth > 0) pending.Add(edit);
            else Append(edit);
        }

        private void Append(ISequenceEdit edit) {
            while (entries.Count > position + 1) entries.RemoveAt(entries.Count - 1);
            entries.Add(new SequenceEditEntry(++nextState, edit));
            position++;
            if (entries.Count > Capacity + 1) {
                entries[0] = new SequenceEditEntry(entries[1].State);
                entries.RemoveAt(1);
                position--;
            }
            (Root as ISequenceRootContainer)?.SetChanged();
            Refresh();
        }

        internal void CaptureStructure(string description, Action action) {
            void Apply(Action change) {
                try { change(); }
                finally { Graph.Refresh(); PruneDetachedSessions(); }
            }
            CaptureEdit(description, () => SequenceStructureSnapshot.Capture(Root, description,
                change => SequenceContainer.ApplyEditorChange(() => Apply(change))), () => Apply(action));
        }

        internal void CaptureEdit(string description, Func<ISequenceEditCapture> capture, Action action) {
            if (!IsRecording || captureDepth > 0) { action(); return; }
            using (BeginTransaction(description)) {
                ISequenceEditCapture before = capture();
                captureDepth++;
                try { action(); }
                finally {
                    captureDepth--;
                    RecordApplied(before?.Complete());
                }
            }
        }

        public bool Undo() => Replay(true);
        public bool Redo() => Replay(false);

        private bool Replay(bool undo) {
            dispatcher.VerifyAccess();
            Flush();
            if (transactionDepth != 0 || !(undo ? CanUndo && position > 0 : CanRedo)) return false;
            ISequenceEdit edit = entries[undo ? position : position + 1].Edit;
            replaying = true;
            try {
                if (!(undo ? edit.CanUndo : edit.CanRedo)) throw new SequenceEditConflictException();
                if (undo) edit.Undo(); else edit.Redo();
                position += undo ? -1 : 1;
                (Root as ISequenceRootContainer)?.SetChanged();
                return true;
            } catch (SequenceEditConflictException) {
                ReplayFailed?.Invoke(Loc.Instance["Lbl_SequenceHistory_Conflict"]);
                return false;
            } catch (SequenceEditReplayException ex) when (ex.Restored) {
                Logger.Error("Sequence edit replay failed; starting state verified", ex);
                ReplayFailed?.Invoke(Loc.Instance["Lbl_SequenceHistory_RestoredFailure"]);
                return false;
            } catch (Exception ex) {
                Logger.Error("Sequence edit replay failed; history invalidated", ex);
                Clear();
                (Root as ISequenceRootContainer)?.SetChanged();
                ReplayFailed?.Invoke(Loc.Instance["Lbl_SequenceHistory_Failed"]);
                return false;
            } finally {
                replaying = false;
                Refresh();
            }
        }

        public void MoveTo(int target) {
            Flush();
            MoveToPosition(target);
        }

        private void MoveToPosition(int target) {
            target = Math.Clamp(target, 0, entries.Count - 1);
            while (position > target && Undo()) { }
            while (position < target && Redo()) { }
        }

        public void MarkSaved() {
            Flush();
            savedState = entries[position].State;
            Refresh();
        }

        public void Clear() {
            entries.Clear();
            entries.Add(new SequenceEditEntry(++nextState));
            pending.Clear();
            position = 0;
            savedState = null;
            Refresh();
        }

        private void Refresh() {
            for (int i = 0; i < entries.Count; i++) {
                entries[i].UpdateState(i, position, savedState);
            }
            OnPropertyChanged(nameof(Position));
            RefreshCommands();
        }

        private void RefreshCommands() {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(RedoDescription));
            OnPropertyChanged(nameof(IsEnabled));
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            MoveToCommand.NotifyCanExecuteChanged();
        }

        public void Dispose() {
            if (disposed) return;
            disposed = true;
            DisposeTemplateHistories();
            SequenceEditContext.Unregister(this);
            Clear();
            FlushRequested = null;
            ReplayFailed = null;
        }

        private sealed class EditScope : IDisposable {
            public static readonly EditScope Empty = new(null);
            private Action complete;
            public EditScope(Action complete) { this.complete = complete; }
            public void Dispose() {
                Action action = complete;
                complete = null;
                action?.Invoke();
            }
        }
    }
}