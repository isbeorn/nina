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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NINA.Sequencer.Editing {
    internal sealed class SequenceStructureSnapshot {
        private readonly ISequenceContainer root;
        private readonly Dictionary<SequenceList, ISequenceEntity[]> before;
        private readonly Dictionary<ISequenceEntity, EditLocation> locations;
        private readonly Dictionary<ISequenceEntity, ISequenceEditSnapshot> attachmentStates = new(ReferenceEqualityComparer.Instance);
        private SequenceStructureSnapshot(ISequenceContainer root) {
            this.root = root;
            before = ReadTree(root, attachmentStates);
            locations = DescribeLocations(before);
        }
        public static SequenceStructureSnapshot Capture(ISequenceContainer root) => new(root);

        public ISequenceEdit Complete(string description) {
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
            return changes.Count == 0 && configuration.Length == 0 ? null : new StructureSequenceEdit(description, changes.ToArray(), configuration, DescribeChanges(after));
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
                } else if (old.List != next.List || (old.Index != next.Index && reordered.Contains(old.List))) {
                    details.Add(string.Format(Loc.Instance["Lbl_SequenceHistory_MovedDetail"], old.Name, old.Display, next.Display));
                }
            }
            return SequenceEditDetails.Join(details);
        }

        private static Dictionary<SequenceList, ISequenceEntity[]> ReadTree(ISequenceContainer root, Dictionary<ISequenceEntity, ISequenceEditSnapshot> states = null) {
            var result = new Dictionary<SequenceList, ISequenceEntity[]>();
            var visited = new HashSet<ISequenceContainer>(ReferenceEqualityComparer.Instance);
            void CaptureAttachmentStates(IEnumerable<ISequenceEntity> entities) {
                if (states == null) return;
                foreach (ISequenceEntity entity in entities) {
                    ISequenceEditSnapshot state = (entity as ISequenceAttachmentStateProvider)?.CaptureAttachmentState();
                    if (state != null) states[entity] = state;
                }
            }
            void VisitTrigger(ISequenceTrigger trigger) {
                foreach (ISequenceContainer actions in SequenceEditContext.EditableTriggerContainers(trigger)) Visit(actions);
                if (trigger is CustomTrigger custom) {
                    var source = new SequenceList(custom, SequenceListKind.TriggerSource);
                    result[source] = source.Read();
                    CaptureAttachmentStates(result[source]);
                    if (custom.TriggerSource is ISequenceTrigger nested) VisitTrigger(nested);
                }
            }
            void Visit(ISequenceContainer container) {
                if (!visited.Add(container) || container is LinkedTemplateContainer { IsEditing: false }) return;
                var items = new SequenceList(container, SequenceListKind.Items);
                result[items] = items.Read();
                CaptureAttachmentStates(result[items]);
                foreach (ISequenceContainer child in result[items].OfType<ISequenceContainer>()) Visit(child);
                if (container is IConditionable) {
                    var conditions = new SequenceList(container, SequenceListKind.Conditions);
                    result[conditions] = conditions.Read();
                    CaptureAttachmentStates(result[conditions]);
                }
                if (container is ITriggerable) {
                    var triggers = new SequenceList(container, SequenceListKind.Triggers);
                    result[triggers] = triggers.Read();
                    CaptureAttachmentStates(result[triggers]);
                    foreach (ISequenceTrigger trigger in result[triggers].OfType<ISequenceTrigger>()) {
                        VisitTrigger(trigger);
                    }
                }
            }
            Visit(root);
            return result;
        }
    }

    internal enum SequenceListKind { Items, Conditions, Triggers, TriggerSource }

    internal sealed record SequenceList(ISequenceEntity Owner, SequenceListKind Kind) {
        private ISequenceContainer Container => (ISequenceContainer)Owner;
        public ISequenceEntity[] Read() => Kind switch {
            SequenceListKind.Items => Container.GetItemsSnapshot().Cast<ISequenceEntity>().ToArray(),
            SequenceListKind.Conditions => ((IConditionable)Owner).GetConditionsSnapshot().Cast<ISequenceEntity>().ToArray(),
            SequenceListKind.TriggerSource => ((CustomTrigger)Owner).TriggerSource is ISequenceTrigger source ? new ISequenceEntity[] { source } : Array.Empty<ISequenceEntity>(),
            _ => ((ITriggerable)Owner).GetTriggersSnapshot().Cast<ISequenceEntity>().ToArray()
        };
        public void Remove(ISequenceEntity entity) {
            if (Kind == SequenceListKind.TriggerSource) { ((CustomTrigger)Owner).TriggerSource = null; return; }
            switch (entity) {
                case ISequenceCondition condition: Container.Remove(condition); break;
                case ISequenceTrigger trigger: Container.Remove(trigger); break;
                case ISequenceItem item: Container.Remove(item); break;
            }
        }
        public void Insert(int index, ISequenceEntity entity) {
            if (Kind == SequenceListKind.TriggerSource) { ((CustomTrigger)Owner).TriggerSource = (ISequenceTrigger)entity; return; }
            if (Owner is SequenceContainer container) {
                switch (entity) {
                    case ISequenceCondition condition: container.InsertIntoSequenceBlocks(index, condition); break;
                    case ISequenceTrigger trigger: container.InsertIntoSequenceBlocks(index, trigger); break;
                    case ISequenceItem item: container.InsertIntoSequenceBlocks(index, item); break;
                }
            } else {
                // Direct interface implementations keep their own attachment semantics.
                switch (entity) {
                    case ISequenceCondition condition: ((IConditionable)Owner).Add(condition); break;
                    case ISequenceTrigger trigger: ((ITriggerable)Owner).Add(trigger); break;
                    case ISequenceItem item: Container.Add(item); break;
                }
                Reorder(entity, index);
            }
        }
        public void Reorder(ISequenceEntity entity, int index) {
            switch (Kind) {
                case SequenceListKind.Items: Move(Container.Items, (ISequenceItem)entity, index); break;
                case SequenceListKind.Conditions: Move(((IConditionable)Owner).Conditions, (ISequenceCondition)entity, index); break;
                case SequenceListKind.Triggers: Move(((ITriggerable)Owner).Triggers, (ISequenceTrigger)entity, index); break;
            }
        }
        private static void Move<T>(IList<T> list, T entity, int index) {
            int oldIndex = list.IndexOf(entity);
            if (oldIndex == index) return;
            if (list is System.Collections.ObjectModel.ObservableCollection<T> observable) { observable.Move(oldIndex, index); return; }
            list.RemoveAt(oldIndex);
            list.Insert(index, entity);
        }
    }

    internal sealed record SequenceListChange(SequenceList List, ISequenceEntity[] Before, ISequenceEntity[] After);

    internal sealed record SequenceAttachmentChange(ISequenceEditSnapshot Before, ISequenceEditSnapshot After);

    internal sealed class StructureSequenceEdit : ISequenceEdit, ISequenceEditDetails {
        private readonly SequenceListChange[] changes;
        private readonly SequenceAttachmentChange[] configuration;
        public StructureSequenceEdit(string description, SequenceListChange[] changes, SequenceAttachmentChange[] configuration, string details) {
            Details = details;
            Description = description;
            this.changes = changes;
            this.configuration = configuration;
        }
        public string Description { get; }
        public string Details { get; }
        public bool CanUndo => Matches(false);
        public bool CanRedo => Matches(true);
        private bool Matches(bool before) => changes.All(change => change.List.Read().SequenceEqual(before ? change.Before : change.After, ReferenceEqualityComparer.Instance))
            && configuration.All(change => (before ? change.Before : change.After).IsCurrent);
        public void Undo() => Apply(true);
        public void Redo() => Apply(false);
        private void Apply(bool undo) {
            SequenceContainer.ApplyEditorChange(() => {
                if (!Matches(!undo)) throw new SequenceEditConflictException();
                try { Restore(undo); }
                catch {
                    try { Restore(!undo); }
                    catch (Exception ex) { throw new InvalidOperationException("Sequence placement could not be compensated.", ex); }
                    throw;
                }
            });
        }
        private void Restore(bool before) {
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
            if (!Matches(before)) throw new InvalidOperationException("Sequence placement could not be restored.");
        }
    }
}