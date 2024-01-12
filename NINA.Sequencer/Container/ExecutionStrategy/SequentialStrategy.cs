#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Sequencer.Container.ExecutionStrategy {

    public class SequentialStrategy : IExecutionStrategy {

        public object Clone() {
            return new SequentialStrategy();
        }

        public async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            ISequenceItem previous = null;
            ISequenceItem next = null;
            bool canContinue = true;
            var root = ItemUtility.GetRootContainer(context);

            context.Iterations = 0;
            InitializeBlock(context);

            try {
                while (((next, canContinue) = GetNextItem(context, previous)).next != null && canContinue) {
                    StartBlock(context);

                    (next, canContinue) = GetNextItem(context, previous);
                    while (next != null && canContinue) {
                        token.ThrowIfCancellationRequested();
                        await RunTriggers(context, previous, next, progress, token);
                        await next.Run(progress, token);
                        previous = next;

                        (next, canContinue) = GetNextItem(context, previous);
                        await RunTriggersAfter(context, previous, next, progress, token);
                    }

                    FinishBlock(context);

                    if (CanContinue(context, previous, next)) {
                        foreach (var item in context.GetItemsSnapshot()) {
                            if (item is ISequenceContainer) {
                                (item as ISequenceContainer).ResetAll();
                            } else {
                                item.ResetProgress();
                            }
                        }
                    }
                }

                //Mark rest of items as skipped
                foreach (var item in context.GetItemsSnapshot().Where(x => x.Status == SequenceEntityStatus.CREATED)) {
                    item.Skip();
                }
            } finally {
                TeardownBlock(context);
            }
        }

        private void TeardownBlock(ISequenceContainer context) {
            foreach (var item in context.GetItemsSnapshot()) {
                item.SequenceBlockTeardown();
            }

            var conditionable = context as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockTeardown();
                }
            }
            var triggerable = context as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockTeardown();
                }
            }
        }

        private void InitializeBlock(ISequenceContainer context) {
            foreach (var item in context.GetItemsSnapshot()) {
                item.SequenceBlockInitialize();
            }

            var conditionable = context as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockInitialize();
                }
            }
            var triggerable = context as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockInitialize();
                }
            }
        }

        private (ISequenceItem, bool) GetNextItem(ISequenceContainer context, ISequenceItem previous) {
            var items = context.GetItemsSnapshot();
            var next = items.FirstOrDefault(x => x.Status == SequenceEntityStatus.CREATED);

            var canContinue = false;
            if (next != null) {
                canContinue = CanContinue(context, previous, next);
            }

            return (next, canContinue);
        }

        private async Task RunTriggers(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggers(previousItem, nextItem, progress, token);
            }

            if (container?.Parent != null) {
                await RunTriggers(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private async Task RunTriggersAfter(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggersAfter(previousItem, nextItem, progress, token);
            }

            if (container?.Parent != null) {
                await RunTriggersAfter(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private void StartBlock(ISequenceContainer container) {
            foreach (var item in container.GetItemsSnapshot()) {
                item.SequenceBlockStarted();
            }

            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockStarted();
                }
            }
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockStarted();
                }
            }
        }

        private void FinishBlock(ISequenceContainer container) {
            container.Iterations++;

            foreach (var item in container.GetItemsSnapshot()) {
                item.SequenceBlockFinished();
            }

            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockFinished();
                }
            }

            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockFinished();
                }
            }
        }

        private bool CanContinue(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem) {
            var conditionable = container as IConditionable;
            var canContinue = false;
            var conditions = conditionable?.GetConditionsSnapshot()?.Where(x => x.Status != SequenceEntityStatus.DISABLED).ToList();
            if (conditions != null && conditions.Count > 0) {
                canContinue = conditionable.CheckConditions(previousItem, nextItem);
            } else {
                canContinue = container.Iterations < 1;
            }

            if (container.Parent != null) {
                canContinue = canContinue && CanContinue(container.Parent, previousItem, nextItem);
            }

            return canContinue;
        }
    }
}