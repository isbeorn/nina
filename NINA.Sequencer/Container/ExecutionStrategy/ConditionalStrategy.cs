#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Container.ExecutionStrategy {

    public class ConditionalStrategy : IExecutionStrategy {

        public object Clone() {
            return new ConditionalStrategy();
        }

        public async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var conditional = context as ConditionalContainer
                ?? throw new ArgumentException($"{nameof(ConditionalStrategy)} requires a {nameof(ConditionalContainer)}", nameof(context));

            if (!ShouldRun(conditional)) {
                MarkCreatedItemsSkipped(conditional);
                Logger.Info($"{conditional.Name}: Conditional expression evaluated false. Skipping instruction set.");
                throw new SequenceItemSkippedException($"{conditional.Name}: Conditional expression evaluated false.");
            }

            await RunSequentiallyOnce(context, progress, token);
        }

        private static bool ShouldRun(ConditionalContainer container) {
            if (string.IsNullOrWhiteSpace(container.PredicateExpression.Definition)) {
                throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceContainer_ConditionalContainer_ExpressionRequired"]);
            }

            container.PredicateExpression.Evaluate();

            if (container.PredicateExpression.Error != null) {
                Logger.Warning($"{nameof(ConditionalContainer)}: error in PredicateExpression: {container.PredicateExpression.Error}");
                throw new SequenceEntityFailedException(container.PredicateExpression.Error);
            }

            return !string.Equals(container.PredicateExpression.ValueString, "0", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task RunSequentiallyOnce(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            ISequenceItem previous = null;
            ISequenceItem next = null;
            bool canContinue = true;

            context.Iterations = 0;
            InitializeBlock(context);

            try {
                while (((next, canContinue) = GetNextItem(context, previous)).next != null && canContinue) {
                    StartBlock(context);

                    (next, canContinue) = GetNextItem(context, previous);
                    while (next != null && canContinue) {
                        token.ThrowIfCancellationRequested();
                        await RunParentTriggers(context.Parent, previous, next, progress, token);
                        await next.Run(progress, token);
                        previous = next;

                        (next, canContinue) = GetNextItem(context, previous);
                        await RunParentTriggersAfter(context.Parent, previous, next, progress, token);
                    }

                    FinishBlock(context);

                    if (CanContinue(context, previous, next)) {
                        foreach (var item in context.GetItemsSnapshot()) {
                            if (item is ISequenceContainer container) {
                                container.ResetAll();
                            } else {
                                item.ResetProgress();
                            }
                        }
                    }
                }

                foreach (var item in context.GetItemsSnapshot().Where(x => x.Status == SequenceEntityStatus.CREATED)) {
                    item.Skip();
                }
            } finally {
                TeardownBlock(context);
            }
        }

        private static void MarkCreatedItemsSkipped(ISequenceContainer container) {
            foreach (var item in container.GetItemsSnapshot()) {
                if (item.Status == SequenceEntityStatus.DISABLED) {
                    continue;
                }

                if (item is ISequenceContainer childContainer) {
                    MarkCreatedItemsSkipped(childContainer);
                }

                if (item.Status == SequenceEntityStatus.CREATED) {
                    item.Skip();
                }
            }
        }

        private static void TeardownBlock(ISequenceContainer context) {
            foreach (var item in context.GetItemsSnapshot()) {
                item.SequenceBlockTeardown();
            }
        }

        private static void InitializeBlock(ISequenceContainer context) {
            foreach (var item in context.GetItemsSnapshot()) {
                item.SequenceBlockInitialize();
            }
        }

        private static (ISequenceItem, bool) GetNextItem(ISequenceContainer context, ISequenceItem previous) {
            var items = context.GetItemsSnapshot();
            var next = items.FirstOrDefault(x => x.Status == SequenceEntityStatus.CREATED);

            var canContinue = false;
            if (next != null) {
                canContinue = CanContinue(context, previous, next);
            }

            return (next, canContinue);
        }

        private static async Task RunParentTriggers(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggers(previousItem, nextItem, progress, token);
            }

            if (container?.Parent != null) {
                await RunParentTriggers(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private static async Task RunParentTriggersAfter(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggersAfter(previousItem, nextItem, progress, token);
            }

            if (container?.Parent != null) {
                await RunParentTriggersAfter(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private static void StartBlock(ISequenceContainer container) {
            foreach (var item in container.GetItemsSnapshot()) {
                item.SequenceBlockStarted();
            }
        }

        private static void FinishBlock(ISequenceContainer container) {
            container.Iterations++;

            foreach (var item in container.GetItemsSnapshot()) {
                item.SequenceBlockFinished();
            }
        }

        private static bool CanContinue(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem) {
            var canContinue = container.Iterations < 1;

            if (container.Parent != null) {
                canContinue = canContinue && CanContinueParent(container.Parent, previousItem, nextItem);
            }

            return canContinue;
        }

        private static bool CanContinueParent(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem) {
            var conditionable = container as IConditionable;
            var canContinue = false;
            var conditions = conditionable?.GetConditionsSnapshot()?.Where(x => x.Status != SequenceEntityStatus.DISABLED).ToList();
            if (conditions != null && conditions.Count > 0) {
                canContinue = conditionable.CheckConditions(previousItem, nextItem);
            } else {
                canContinue = container.Iterations < 1;
            }

            if (container.Parent != null) {
                canContinue = canContinue && CanContinueParent(container.Parent, previousItem, nextItem);
            }

            return canContinue;
        }
    }
}
