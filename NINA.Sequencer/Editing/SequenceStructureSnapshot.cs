#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Sequencer.Container;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NINA.Sequencer.Editing {
    internal sealed class SequenceStructureSnapshot : ISequenceEditCapture {
        private readonly string description;
        private readonly Action<Action> replayScope;
        private readonly ISequenceContainer root;
        private readonly Dictionary<SequenceList, ISequenceEntity[]> before;
        private readonly Dictionary<ISequenceEntity, EditLocation> locations;
        private readonly Dictionary<ISequenceEntity, ISequenceEditSnapshot> attachmentStates = new(ReferenceEqualityComparer.Instance);
        private SequenceStructureSnapshot(ISequenceContainer root, string description, Action<Action> replayScope) {
            this.replayScope = replayScope;
            this.description = description;
            this.root = root;
            before = ReadTree(root, attachmentStates);
            locations = DescribeLocations(before);
        }
        public static SequenceStructureSnapshot Capture(ISequenceContainer root, string description, Action<Action> replayScope) => new(root, description, replayScope);

        public ISequenceEdit Complete() {
            Dictionary<SequenceList, ISequenceEntity[]> after = ReadTree(root);
            // Detached subtrees remain intact. Their child lists still exist and must not be
            // mistaken for deleted contents merely because they left the displayed tree.
            foreach (SequenceList list in before.Keys) {
                if (!after.ContainsKey(list)) after.Add(list, list.Read());
            }
            var changes = new List<SequenceListChange>();
            foreach (var pair in after) {
                ISequenceEntity[] old = before.TryGetValue(pair.Key, out var found) ? found : Array.Empty<ISequenceEntity>();
                if (!old.SequenceEqual(pair.Value, ReferenceEqualityComparer.Instance)) changes.Add(new(pair.Key, old, pair.Value));
            }
            var configuration = attachmentStates.Where(pair => !pair.Value.IsCurrent)
                .Select(pair => new SequenceAttachmentChange(pair.Value, ((ISequenceAttachmentStateProvider)pair.Key).CaptureAttachmentState())).ToArray();
            if (configuration.Any(change => change.After == null)) throw new InvalidOperationException("The entity did not supply the completed attachment snapshot.");
            return changes.Count == 0 && configuration.Length == 0 ? null : new StateSequenceEdit(description,
                new SequencePlacementState(changes.ToArray(), configuration, true), new SequencePlacementState(changes.ToArray(), configuration, false),
                DescribeChanges(after), replayScope);
        }

        private sealed record EditLocation(SequenceList List, int Index, string Name, string Container) {
            public string Display => $"{Container} (#{Index + 1})";
        }

        private static Dictionary<ISequenceEntity, EditLocation> DescribeLocations(Dictionary<SequenceList, ISequenceEntity[]> lists) {
            var result = new Dictionary<ISequenceEntity, EditLocation>(ReferenceEqualityComparer.Instance);
            foreach (var pair in lists) {
                string container = SequenceEditDetails.Path(pair.Key.Owner);
                for (int i = 0; i < pair.Value.Length; i++) {
                    ISequenceEntity entity = pair.Value[i];
                    result.TryAdd(entity, new EditLocation(pair.Key, i, SequenceEditDetails.Name(entity), container));
                }
            }
            return result;
        }

        private string DescribeChanges(Dictionary<SequenceList, ISequenceEntity[]> after) {
            var current = DescribeLocations(after);
            var reordered = new HashSet<SequenceList>();
            foreach (var pair in before) {
                if (!after.TryGetValue(pair.Key, out var next)) continue;
                var common = new HashSet<ISequenceEntity>(pair.Value, ReferenceEqualityComparer.Instance);
                common.IntersectWith(next);
                if (!pair.Value.Where(common.Contains).SequenceEqual(next.Where(common.Contains), ReferenceEqualityComparer.Instance)) reordered.Add(pair.Key);
            }
            var details = new List<string>();
            foreach (ISequenceEntity entity in locations.Keys.Union(current.Keys, ReferenceEqualityComparer.Instance)) {
                locations.TryGetValue(entity, out EditLocation old);
                current.TryGetValue(entity, out EditLocation next);
                if (old == null) {
                    // The containing entity already describes insertion of this whole subtree.
                    if (!locations.ContainsKey(next.List.Owner) && !ReferenceEquals(next.List.Owner, root)) continue;
                    details.Add(string.Format(Loc.Instance["Lbl_SequenceHistory_AddedDetail"], next.Name, next.Display));
                } else if (next == null) {
                    if (!current.ContainsKey(old.List.Owner) && !ReferenceEquals(old.List.Owner, root)) continue;
                    details.Add(string.Format(Loc.Instance["Lbl_SequenceHistory_RemovedDetail"], old.Name, old.Display));
                } else if (!old.List.Equals(next.List) || (old.Index != next.Index && reordered.Contains(old.List))) {
                    details.Add(string.Format(Loc.Instance["Lbl_SequenceHistory_MovedDetail"], old.Name, old.Display, next.Display));
                }
            }
            return SequenceEditDetails.Join(details);
        }

        private static Dictionary<SequenceList, ISequenceEntity[]> ReadTree(ISequenceContainer root, Dictionary<ISequenceEntity, ISequenceEditSnapshot> states = null) {
            var result = new Dictionary<SequenceList, ISequenceEntity[]>();
            foreach (var (list, children) in SequenceEditorGraph.Read(root)) {
                if (!list.IsReadOnly) result.Add(list, children);
                if (states == null) continue;
                foreach (ISequenceEntity entity in children) {
                    var state = (entity as ISequenceAttachmentStateProvider)?.CaptureAttachmentState();
                    if (state != null) states[entity] = state;
                }
            }
            return result;
        }
    }

    internal sealed record SequenceListChange(SequenceList List, ISequenceEntity[] Before, ISequenceEntity[] After);

    internal sealed record SequenceAttachmentChange(ISequenceEditSnapshot Before, ISequenceEditSnapshot After);

    internal sealed class SequencePlacementState : ISequenceEditSnapshot {
        private readonly SequenceListChange[] changes;
        private readonly SequenceAttachmentChange[] configuration;
        private readonly bool before;
        public SequencePlacementState(SequenceListChange[] changes, SequenceAttachmentChange[] configuration, bool before) {
            this.changes = changes;
            this.configuration = configuration;
            this.before = before;
        }
        public string Description => null;
        public bool IsCurrent => changes.All(change => change.List.Read().SequenceEqual(before ? change.Before : change.After, ReferenceEqualityComparer.Instance))
            && configuration.All(change => (before ? change.Before : change.After).IsCurrent);
        public void Restore() {
            foreach (SequenceListChange change in changes) {
                var target = new HashSet<ISequenceEntity>(before ? change.Before : change.After, ReferenceEqualityComparer.Instance);
                foreach (ISequenceEntity entity in change.List.Read()) {
                    if (!target.Contains(entity)) change.List.Remove(entity);
                }
            }
            foreach (SequenceListChange change in changes) {
                ISequenceEntity[] target = before ? change.Before : change.After;
                for (int i = 0; i < target.Length; i++) {
                    if (change.List.Read().Contains(target[i], ReferenceEqualityComparer.Instance)) change.List.Reorder(target[i], i);
                    else change.List.Insert(i, target[i]);
                }
            }
            foreach (SequenceAttachmentChange change in configuration) (before ? change.Before : change.After).Restore();
        }
    }
}