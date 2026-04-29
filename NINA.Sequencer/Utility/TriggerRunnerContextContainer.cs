#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace NINA.Sequencer.Utility {

    public class TriggerRunnerContextContainer : SequentialContainer, IDeepSkyObjectContainer {
        protected readonly IDeepSkyObjectContainer deepSkyObjectContainer;

        public TriggerRunnerContextContainer(IDeepSkyObjectContainer deepSkyObjectContainer) {
            this.deepSkyObjectContainer = deepSkyObjectContainer;
        }

        public InputTarget Target {
            get => deepSkyObjectContainer?.Target;
            set {
                if (deepSkyObjectContainer != null) {
                    deepSkyObjectContainer.Target = value;
                }
            }
        }

        public NighttimeData NighttimeData => deepSkyObjectContainer?.NighttimeData;

        public override void AfterParentChanged() {
        }
    }

    internal sealed class TriggerRunnerRootContextContainer : TriggerRunnerContextContainer, ISequenceRootContainer {
        private static readonly Dictionary<string, bool> EmptyHasChanges = new Dictionary<string, bool>();
        private readonly ISequenceRootContainer root;

        public TriggerRunnerRootContextContainer(ISequenceRootContainer root, IDeepSkyObjectContainer deepSkyObjectContainer)
            : base(deepSkyObjectContainer) {
            this.root = root;
            if (root != null) {
                Items = root.Items;
            }
        }

        public string SequenceTitle {
            get => root?.SequenceTitle ?? string.Empty;
            set {
                if (root != null) {
                    root.SequenceTitle = value;
                }
            }
        }

        public Dictionary<string, bool> HasChanges => root?.HasChanges ?? EmptyHasChanges;

        public event Func<object, SequenceEntityFailureEventArgs, Task> FailureEvent {
            add {
                if (root != null) {
                    root.FailureEvent += value;
                }
            }
            remove {
                if (root != null) {
                    root.FailureEvent -= value;
                }
            }
        }

        public void AddRunningItem(ISequenceItem item) {
            root?.AddRunningItem(item);
        }

        public void RemoveRunningItem(ISequenceItem item) {
            root?.RemoveRunningItem(item);
        }

        public void AddRunningTrigger(ISequenceTrigger trigger) {
            root?.AddRunningTrigger(trigger);
        }

        public void RemoveRunningTrigger(ISequenceTrigger trigger) {
            root?.RemoveRunningTrigger(trigger);
        }

        public void SkipCurrentRunningItems() {
            root?.SkipCurrentRunningItems();
        }

        public void InterruptAndResetCurrentRunningItems() {
            root?.InterruptAndResetCurrentRunningItems();
        }

        public IReadOnlyCollection<ISequenceItem> GetCurrentRunningItems() {
            return root?.GetCurrentRunningItems() ?? ImmutableArray<ISequenceItem>.Empty;
        }

        public Task RaiseFailureEvent(ISequenceEntity sender, Exception ex) {
            return root?.RaiseFailureEvent(sender, ex) ?? Task.CompletedTask;
        }

        public bool DoesHaveChanges(string hasChangeSet) {
            return root?.DoesHaveChanges(hasChangeSet) ?? false;
        }

        public void SetChanged(string changedSet = defaultChangeSet) {
            root?.SetChanged(changedSet);
        }

        public override Task Interrupt() {
            return root?.Interrupt() ?? Task.CompletedTask;
        }
    }
}
