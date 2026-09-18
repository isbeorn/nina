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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NINA.Sequencer.Editing {
    internal sealed class SequenceEditorGraph {
        private readonly ISequenceContainer root;
        // Only non-parent ownership edges need indexing. Weak keys do not retain removed children.
        private ConditionalWeakTable<ISequenceEntity, SequenceList> owners = new();
        public SequenceEditorGraph(ISequenceContainer root) { this.root = root; Refresh(); }
        public void Refresh() {
            owners = new();
            foreach (var (list, children) in Read(root, true)) {
                foreach (ISequenceEntity child in children) {
                    if (!ReferenceEquals(child.Parent, list.Owner)) owners.TryAdd(child, list);
                }
            }
        }
        public IReadOnlyList<ISequenceEntity> OwnerPath(ISequenceEntity entity, bool refreshIfMissing = true) {
            IReadOnlyList<ISequenceEntity> Find() {
                var path = new List<ISequenceEntity>();
                var visited = new HashSet<ISequenceEntity>(ReferenceEqualityComparer.Instance);
                for (ISequenceEntity current = entity; current != null && visited.Add(current);) {
                    path.Add(current);
                    if (ReferenceEquals(current, root)) return path;
                    current = owners.TryGetValue(current, out SequenceList list) && list.Read().Contains(current, ReferenceEqualityComparer.Instance)
                        ? list.Owner : current.Parent;
                }
                return null;
            }
            var result = Find();
            if (result != null || !refreshIfMissing) return result;
            // External structural changes may introduce new plugin-owned action sets.
            Refresh();
            return Find();
        }
        internal static IEnumerable<(SequenceList List, ISequenceEntity[] Children)> Read(ISequenceEntity root, bool includeReadOnlyContents = false) {
            var visited = new HashSet<ISequenceEntity>(ReferenceEqualityComparer.Instance);
            IEnumerable<(SequenceList, ISequenceEntity[])> Visit(ISequenceEntity entity) {
                if (!visited.Add(entity) || (!includeReadOnlyContents && entity is ISequenceEditSessionBoundary { IsEditing: false })) yield break;
                foreach (ISequenceEditorChildList children in ChildLists(entity)) {
                    var list = new SequenceList(entity, children);
                    var items = list.Read();
                    yield return (list, items);
                    foreach (ISequenceEntity child in items) foreach (var entry in Visit(child)) yield return entry;
                }
            }
            return Visit(root);
        }
        private static IEnumerable<ISequenceEditorChildList> ChildLists(ISequenceEntity entity) {
            if (entity is ISequenceContainer container) {
                var insertion = container as ISequenceEditorInsertion;
                yield return SequenceEditorChildList.Collection("Items", () => container.Items, container.GetItemsSnapshot,
                    container.Add, item => container.Remove(item), insertion == null ? null : insertion.InsertIntoSequenceBlocks);
                if (container is IConditionable conditions) yield return SequenceEditorChildList.Collection("Conditions", () => conditions.Conditions, conditions.GetConditionsSnapshot,
                    conditions.Add, item => container.Remove(item), insertion == null ? null : insertion.InsertIntoSequenceBlocks);
                if (container is ITriggerable triggers) yield return SequenceEditorChildList.Collection("Triggers", () => triggers.Triggers, triggers.GetTriggersSnapshot,
                    triggers.Add, item => container.Remove(item), insertion == null ? null : insertion.InsertIntoSequenceBlocks);
            }
            if (entity is ISequenceEditorChildProvider provider) foreach (var list in provider.GetEditorChildLists()) yield return list;
            if (entity is ISequenceTriggerEditor editor) yield return SequenceEditorChildList.Owned("AdditionalActions", () => editor.GetAdditionalEditorContainers());
        }
    }
}