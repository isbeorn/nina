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
        private readonly ISequenceEntity subject;
        private readonly SequenceEditOperation? operation;
        private readonly ISequenceContainer root;
        private readonly Dictionary<SequenceList, ISequenceEntity[]> before;
        private readonly Dictionary<ISequenceEntity, EditLocation> locations;
        private readonly Dictionary<ISequenceEntity, ISequenceEditSnapshot> attachmentStates = new(ReferenceEqualityComparer.Instance);
        private SequenceStructureSnapshot(ISequenceContainer root, string description, Action<Action> replayScope, ISequenceEntity subject, SequenceEditOperation? operation) {
            this.replayScope = replayScope;
            this.description = description;
            this.root = root;
            this.subject = subject;
            this.operation = operation;
            before = ReadTree(root, attachmentStates);
            locations = DescribeLocations(before);
        }
        public static SequenceStructureSnapshot Capture(ISequenceContainer root, string description, Action<Action> replayScope,
            ISequenceEntity subject = null, SequenceEditOperation? operation = null) => new(root, description, replayScope, subject, operation);

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
            if (changes.Count == 0 && configuration.Length == 0) return null;
            PlacementChange[] placements = DescribeChanges(after).ToArray();
            string title = description;
            if (placements.Length == 1) {
                var placement = placements[0];
                string label = placement.Before == null
                    ? operation == SequenceEditOperation.Duplicate ? "Lbl_SequenceHistory_DuplicateAction" : "Lbl_SequenceHistory_AddAction"
                    : placement.After == null ? "Lbl_SequenceHistory_DeleteAction" : "Lbl_SequenceHistory_MoveAction";
                title = string.Format(Loc.Instance[label], placement.Name);
            }
            return new StateSequenceEdit(title,
                new SequencePlacementState(changes.ToArray(), configuration, true), new SequencePlacementState(changes.ToArray(), configuration, false),
                SequenceEditDetails.Join(placements.Select(change => change.Details)), replayScope,
                SequenceEditDetails.Join(placements.Select(change => change.Summary(placements.Length > 1))));
        }

        private sealed record EditLocation(SequenceList List, int Index, string Name, string Container, string ShortContainer) {
            public string Display => $"{Container} (#{Index + 1})";
        }

        private sealed record PlacementChange(EditLocation Before, EditLocation After) {
            public string Name => (Before ?? After).Name;
            public string Details => Before == null
                ? string.Format(Loc.Instance["Lbl_SequenceHistory_AddedDetail"], Name, After.Display)
                : After == null ? string.Format(Loc.Instance["Lbl_SequenceHistory_RemovedDetail"], Name, Before.Display)
                : string.Format(Loc.Instance["Lbl_SequenceHistory_MovedDetail"], Name, Before.Display, After.Display);

            public string Summary(bool includeName) {
                string location = Before != null && After != null && !Before.List.Equals(After.List)
                    ? string.Format(Loc.Instance["Lbl_SequenceHistory_ValueChange"], Before.ShortContainer, After.ShortContainer)
                    : (Before ?? After).ShortContainer;
                string position = Before != null && After != null && Before.Index != After.Index
                    ? string.Format(Loc.Instance["Lbl_SequenceHistory_PositionChange"], Before.Index + 1, After.Index + 1)
                    : string.Format(Loc.Instance["Lbl_SequenceHistory_Position"], (Before ?? After).Index + 1);
                return SequenceEditDetails.Join(new[] { includeName ? $"{Name}: {location}" : location, position });
            }
        }

        private static Dictionary<ISequenceEntity, EditLocation> DescribeLocations(Dictionary<SequenceList, ISequenceEntity[]> lists) {
            var result = new Dictionary<ISequenceEntity, EditLocation>(ReferenceEqualityComparer.Instance);
            var containers = new Dictionary<ISequenceEntity, (string Full, string Short)>(ReferenceEqualityComparer.Instance);
            foreach (SequenceList list in lists.Keys) {
                if (!containers.ContainsKey(list.Owner)) containers.Add(list.Owner,
                    (SequenceEditDetails.Path(list.Owner), SequenceEditDetails.ShortPath(list.Owner)));
            }
            // Repeated short captions in different branches need their ancestors for context.
            var ambiguous = containers.Values.GroupBy(path => path.Short)
                .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
            foreach (var pair in lists) {
                var container = containers[pair.Key.Owner];
                string shortPath = ambiguous.Contains(container.Short) ? SequenceEditDetails.ShortPath(pair.Key.Owner, includeAncestors: true) : container.Short;
                for (int i = 0; i < pair.Value.Length; i++) {
                    ISequenceEntity entity = pair.Value[i];
                    result.TryAdd(entity, new EditLocation(pair.Key, i, SequenceEditDetails.Name(entity), container.Full, shortPath));
                }
            }
            return result;
        }

        private IEnumerable<PlacementChange> DescribeChanges(Dictionary<SequenceList, ISequenceEntity[]> after) {
            var current = DescribeLocations(after);
            var reordered = new HashSet<SequenceList>();
            var subjectReorders = new HashSet<SequenceList>();
            foreach (var pair in before) {
                if (!after.TryGetValue(pair.Key, out var next)) continue;
                var common = new HashSet<ISequenceEntity>(pair.Value, ReferenceEqualityComparer.Instance);
                common.IntersectWith(next);
                if (pair.Value.Where(common.Contains).SequenceEqual(next.Where(common.Contains), ReferenceEqualityComparer.Instance)) continue;
                reordered.Add(pair.Key);
                // Hide displaced neighbors only when the selected item explains the entire reorder.
                if (subject != null && common.Remove(subject)
                    && pair.Value.Where(common.Contains).SequenceEqual(next.Where(common.Contains), ReferenceEqualityComparer.Instance)) subjectReorders.Add(pair.Key);
            }
            foreach (ISequenceEntity entity in locations.Keys.Union(current.Keys, ReferenceEqualityComparer.Instance)) {
                locations.TryGetValue(entity, out EditLocation old);
                current.TryGetValue(entity, out EditLocation next);
                if (old == null) {
                    // The containing entity already describes insertion of this whole subtree.
                    if (!locations.ContainsKey(next.List.Owner) && !ReferenceEquals(next.List.Owner, root)) continue;
                    yield return new PlacementChange(null, next);
                } else if (next == null) {
                    if (!current.ContainsKey(old.List.Owner) && !ReferenceEquals(old.List.Owner, root)) continue;
                    yield return new PlacementChange(old, null);
                } else if (!old.List.Equals(next.List) || (old.Index != next.Index && reordered.Contains(old.List)
                    && (!subjectReorders.Contains(old.List) || ReferenceEquals(entity, subject)))) {
                    yield return new PlacementChange(old, next);
                }
            }
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