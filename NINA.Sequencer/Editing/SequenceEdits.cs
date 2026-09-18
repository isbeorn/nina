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
using System.Collections.Generic;
using System.Linq;

namespace NINA.Sequencer.Editing {
    internal sealed class SequenceEditConflictException : Exception { }

    internal sealed class SequenceEditReplayException : Exception {
        public SequenceEditReplayException(bool restored, Exception inner) : base("Sequence edit replay failed.", inner) { Restored = restored; }
        public bool Restored { get; }
    }

    internal sealed class PropertySequenceEdit<T> : StateSequenceEdit {
        public PropertySequenceEdit(string description, Func<T> read, Action<T> write, T before, T after, Func<T, T, bool> equal = null, string details = null)
            : base(description, new SequenceEditSnapshot<T>(read, write, before, equal), new SequenceEditSnapshot<T>(read, write, after, equal), details) { }
    }

    internal class StateSequenceEdit : ISequenceEdit, ISequenceEditDetails {
        private readonly Action<Action> replayScope;
        private readonly ISequenceEditSnapshot before;
        private readonly ISequenceEditSnapshot after;
        public StateSequenceEdit(string description, ISequenceEditSnapshot before, ISequenceEditSnapshot after, string details = null, Action<Action> replayScope = null) {
            Description = description;
            Details = details;
            this.replayScope = replayScope;
            this.before = before;
            this.after = after;
        }
        public string Description { get; }
        public string Details { get; }
        public bool CanUndo => after.IsCurrent;
        public bool CanRedo => before.IsCurrent;
        public void Undo() => Apply(after, before);
        public void Redo() => Apply(before, after);
        private void Apply(ISequenceEditSnapshot expected, ISequenceEditSnapshot value) {
            if (replayScope == null) Restore(expected, value);
            else replayScope(() => Restore(expected, value));
        }
        private static void Restore(ISequenceEditSnapshot expected, ISequenceEditSnapshot value) {
            if (!expected.IsCurrent) throw new SequenceEditConflictException();
            try {
                value.Restore();
                if (!value.IsCurrent) throw new InvalidOperationException("The edit setter rejected the restored value.");
            } catch (Exception failure) {
                try {
                    expected.Restore();
                    if (!expected.IsCurrent) throw new InvalidOperationException("The setter rejected compensation.");
                } catch (Exception compensation) { throw new SequenceEditReplayException(false, new AggregateException(failure, compensation)); }
                throw new SequenceEditReplayException(true, failure);
            }
        }
    }

    internal sealed class CompositeSequenceEdit : ISequenceEdit, ISequenceEditDetails {
        private readonly ISequenceEdit[] edits;
        public CompositeSequenceEdit(string description, ISequenceEdit[] edits) {
            Description = description;
            this.edits = edits;
            Details = SequenceEditDetails.Join(edits.Select(edit => (edit as ISequenceEditDetails)?.Details ?? edit.Description));
        }
        public string Details { get; }
        public string Description { get; }
        // Check each operation immediately before applying it. Earlier operations in a transaction
        // can establish the preconditions of later ones (including multiple edits of one property).
        public bool CanUndo => edits.Length > 0 && edits[^1].CanUndo;
        public bool CanRedo => edits.Length > 0 && edits[0].CanRedo;
        public void Undo() => Apply(true);
        public void Redo() => Apply(false);
        private void Apply(bool undo) {
            var applied = new List<ISequenceEdit>();
            try {
                foreach (ISequenceEdit edit in undo ? edits.Reverse() : edits) {
                    if (!(undo ? edit.CanUndo : edit.CanRedo)) throw new SequenceEditConflictException();
                    if (undo) edit.Undo(); else edit.Redo();
                    applied.Add(edit);
                }
            } catch {
                try {
                    foreach (ISequenceEdit edit in applied.AsEnumerable().Reverse()) {
                        if (undo) edit.Redo(); else edit.Undo();
                        if (!(undo ? edit.CanUndo : edit.CanRedo)) throw new InvalidOperationException("Transaction compensation could not be verified.");
                    }
                } catch (Exception ex) { throw new InvalidOperationException("The transaction could not be compensated.", ex); }
                throw;
            }
        }
    }
}