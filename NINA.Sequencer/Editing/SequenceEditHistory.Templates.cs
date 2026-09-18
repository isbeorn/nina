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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NINA.Sequencer.Editing {
    internal sealed partial class SequenceEditHistory {
        private readonly Dictionary<ISequenceEditSessionBoundary, SequenceEditHistory> templateHistories = new();
        private SequenceEditHistory activeHistory;
        private SequenceEditHistory parentHistory;

        public SequenceEditHistory ActiveHistory => activeHistory ?? this;

        internal SequenceEditHistory ForOwner(ISequenceEntity owner) => ResolveSession(owner, false);
        internal SequenceEditHistory ForContents(ISequenceContainer container) => ResolveSession(container, true);

        private SequenceEditHistory ResolveSession(ISequenceEntity owner, bool includeOwnBoundary) {
            if (disposed || !dispatcher.CheckAccess()) return null;
            if (parentHistory != null) return parentHistory.ResolveSession(owner, includeOwnBoundary);
            ISequenceEditSessionBoundary editing = null;
            IReadOnlyList<ISequenceEntity> path = OwnerPath(owner);
            if (path == null) return null;
            foreach (ISequenceEntity current in path) {
                if (current is ISequenceEditSessionBoundary linked && (includeOwnBoundary || !ReferenceEquals(current, owner))) {
                    if (!linked.IsEditing) return null;
                    editing = linked;
                }
            }
            if (ReferenceEquals(editing, Root)) editing = null;
            SequenceEditHistory selected = this;
            if (editing != null && !templateHistories.TryGetValue(editing, out selected)) {
                selected = new SequenceEditHistory(editing) { IsEnabled = IsEnabled, parentHistory = this };
                selected.FlushRequested += Flush;
                selected.ReplayFailed += ForwardFailure;
                templateHistories.Add(editing, selected);
                editing.PropertyChanged += TemplateChanged;
            }
            SetProperty(ref activeHistory, ReferenceEquals(selected, this) ? null : selected, nameof(ActiveHistory));
            return selected;
        }

        private IReadOnlyList<ISequenceEntity> OwnerPath(ISequenceEntity owner) => Graph.OwnerPath(owner);

        private void ForwardFailure(string message) => ReplayFailed?.Invoke(message);

        private void TemplateChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != nameof(ISequenceEditSessionBoundary.IsEditing) || sender is not ISequenceEditSessionBoundary { IsEditing: false } linked) return;
            CloseSession(linked);
        }

        internal void PruneDetachedSessions() {
            foreach (var boundary in templateHistories.Keys.ToArray()) {
                if (OwnerPath(boundary) != null) continue;
                CloseSession(boundary);
            }
        }

        private void CloseSession(ISequenceEditSessionBoundary linked) {
            if (!templateHistories.Remove(linked, out SequenceEditHistory history)) return;
            linked.PropertyChanged -= TemplateChanged;
            if (ReferenceEquals(activeHistory, history)) SetProperty(ref activeHistory, null, nameof(ActiveHistory));
            history.Dispose();
        }

        private void DisposeTemplateHistories() {
            foreach (var pair in templateHistories) {
                pair.Key.PropertyChanged -= TemplateChanged;
                pair.Value.Dispose();
            }
            templateHistories.Clear();
            activeHistory = null;
        }
    }
}