#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Editing;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;

namespace NINA.Sequencer.Serialization {
    // Only loading is batched. Public attachment, editing and explicit validation keep
    // their synchronous behavior outside this isolated, thread-local scope.
    internal sealed class SequenceDeserializationScope : IDisposable {
        [ThreadStatic] private static SequenceDeserializationScope current;
        [ThreadStatic] private static int suspensionDepth;

        private readonly SequenceDeserializationScope previous;
        private readonly int previousSuspensionDepth;
        private readonly HashSet<SequenceCondition> watchdogs = new(ReferenceEqualityComparer.Instance);
        private bool collecting = true;
        private bool deferredParentChange;
        private bool finalParentRefresh;
        private bool completed;
        private bool disposed;

        public SequenceDeserializationScope() {
            previous = current;
            previousSuspensionDepth = suspensionDepth;
            current = this;
            suspensionDepth = 0;
        }

        public static IDisposable Suspend() => new Suspension();

        public static bool IsFinalParentRefresh => suspensionDepth == 0 && current?.finalParentRefresh == true;

        public static void NotifyParentChanged(ISequenceEntity entity) {
            using var compatibility = entity.GetType().Assembly != typeof(SequenceDeserializationScope).Assembly ? Suspend() : null;
            entity.AfterParentChanged();
        }

        public static bool TryDeferParentChange(ISequenceEntity entity) {
            if (suspensionDepth != 0 || current?.collecting != true
                || entity.GetType().Assembly != typeof(SequenceDeserializationScope).Assembly) return false;
            current.deferredParentChange = true;
            return true;
        }

        public static bool TryDeferWatchdog(SequenceCondition condition) {
            if (suspensionDepth != 0 || current == null
                || condition.GetType().Assembly != typeof(SequenceDeserializationScope).Assembly) return false;
            current.watchdogs.Add(condition);
            return true;
        }

        public void Complete(ISequenceEntity root) {
            if (disposed) throw new ObjectDisposedException(nameof(SequenceDeserializationScope));
            if (completed) return;
            collecting = false;
            // A standalone linked-template asset resets its preview and link state in OnDeserialized.
            // Leave that state intact until the caller requests resolution.
            if (deferredParentChange && root != null && root.GetType() != typeof(LinkedTemplateContainer)) {
                finalParentRefresh = true;
                try { NotifyParentChanged(root); }
                finally { finalParentRefresh = false; }
                // A later sibling can register a symbol needed by an earlier consumer.
                // Validate the completed context before allowing watchdogs to observe it.
                if (root is IValidatable validatable) validatable.Validate();
            }
            if (watchdogs.Count > 0) {
                var selected = ReadGraph(root);
                foreach (var condition in watchdogs) {
                    if (selected.Contains(condition) && ItemUtility.IsInRootContainer(condition.Parent)) {
                        condition.ConditionWatchdog?.Start();
                    } else {
                        try { condition.ConditionWatchdog?.Cancel(); } catch { }
                    }
                }
            }
            completed = true;
        }

        public void Dispose() {
            if (disposed) return;
            disposed = true;
            try {
                if (!completed) {
                    foreach (var condition in watchdogs) {
                        try { condition.ConditionWatchdog?.Cancel(); } catch { }
                    }
                }
            } finally {
                watchdogs.Clear();
                current = previous;
                suspensionDepth = previousSuspensionDepth;
            }
        }

        private static HashSet<ISequenceEntity> ReadGraph(ISequenceEntity root) {
            var result = new HashSet<ISequenceEntity>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<ISequenceEntity>();
            if (root != null) pending.Push(root);
            while (pending.TryPop(out var entity)) {
                if (entity == null || !result.Add(entity)) continue;
                if (entity is ISequenceContainer container) {
                    foreach (var child in container.GetItemsSnapshot()) pending.Push(child);
                    if (container is IConditionable conditions) foreach (var child in conditions.GetConditionsSnapshot()) pending.Push(child);
                    if (container is ITriggerable triggers) foreach (var child in triggers.GetTriggersSnapshot()) pending.Push(child);
                }
                if (entity is ISequenceEditorChildProvider provider) {
                    foreach (var list in provider.GetEditorChildLists()) foreach (var child in list.Read()) pending.Push(child);
                }
                if (entity is ISequenceTriggerEditor editor) {
                    foreach (var child in editor.GetAdditionalEditorContainers()) pending.Push(child);
                }
            }
            return result;
        }

        private sealed class Suspension : IDisposable {
            public Suspension() { suspensionDepth++; }
            public void Dispose() { suspensionDepth--; }
        }
    }
}