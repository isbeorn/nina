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
        private readonly Dictionary<LinkedTemplateContainer, SequenceEditHistory> templateHistories = new();
        private SequenceEditHistory activeHistory;
        private SequenceEditHistory parentHistory;

        public SequenceEditHistory ActiveHistory => activeHistory ?? this;

        internal SequenceEditHistory ForOwner(ISequenceEntity owner) {
            if (disposed || !dispatcher.CheckAccess()) return null;
            if (parentHistory != null) return parentHistory.ForOwner(owner);
            LinkedTemplateContainer editing = null;
            IReadOnlyList<ISequenceEntity> path = OwnerPath(owner);
            if (path == null) return null;
            foreach (ISequenceEntity current in path) {
                if (current is LinkedTemplateContainer linked) {
                    if (!linked.IsEditing && !ReferenceEquals(current, owner)) return null;
                    if (linked.IsEditing) editing = linked;
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

        private IReadOnlyList<ISequenceEntity> OwnerPath(ISequenceEntity owner) {
            var path = new List<ISequenceEntity>();
            for (ISequenceEntity current = owner; current != null; current = current.Parent) {
                path.Add(current);
                if (ReferenceEquals(current, Root)) return path;
            }
            // Execution context proxies are not editor parents. Follow the owning trigger
            // so template preview/edit-session rules also apply to its action sets.
            return SequenceEditContext.FindEditorPath(Root, owner)?.Reverse().ToArray();
        }

        private void ForwardFailure(string message) => ReplayFailed?.Invoke(message);

        private void TemplateChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != nameof(LinkedTemplateContainer.IsEditing) || sender is not LinkedTemplateContainer { IsEditing: false } linked) return;
            if (templateHistories.Remove(linked, out SequenceEditHistory history)) {
                linked.PropertyChanged -= TemplateChanged;
                if (ReferenceEquals(activeHistory, history)) SetProperty(ref activeHistory, null, nameof(ActiveHistory));
                history.Dispose();
            }
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