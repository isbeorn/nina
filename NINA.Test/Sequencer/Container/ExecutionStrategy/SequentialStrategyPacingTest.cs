#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Container.ExecutionStrategy {

    [TestFixture]
    [NonParallelizable]
    public class SequentialStrategyPacingTest {

        [Test]
        public async Task Execute_RapidIterations_PacesSuccessiveRepeatsAndPreservesCount() {
            var container = CreateLoop(13);
            var item = CreateItem();
            container.Add(item.Object);
            var elapsed = Stopwatch.StartNew();

            await new SequentialStrategy().Execute(container, default, default);

            // Three repeat boundaries after the initial allowance must be paced.
            elapsed.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(700));
            container.Iterations.Should().Be(13);
            item.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(13));
            item.Verify(x => x.ResetProgress(), Times.Exactly(12));
        }

        [TestCase(1)]
        [TestCase(9)]
        [TestCase(10)]
        public async Task Execute_ShortFiniteLoop_DoesNotIntroduceAnAsynchronousWait(int iterations) {
            var container = CreateLoop(iterations);
            container.Add(CreateItem().Object);

            var execution = new SequentialStrategy().Execute(container, default, default);

            execution.IsCompletedSuccessfully.Should().BeTrue();
            await execution;
            container.Iterations.Should().Be(iterations);
        }

        [Test]
        public async Task Execute_SlowerIteration_ClearsRapidRepeatCount() {
            var container = CreateLoop(19);
            var executions = 0;
            var item = CreateItem(() => {
                if (++executions == 10) {
                    // Keep the instruction synchronous so any yielded task comes from pacing.
                    Thread.Sleep(125);
                }
            });
            container.Add(item.Object);

            var execution = new SequentialStrategy().Execute(container, default, default);

            execution.IsCompletedSuccessfully.Should().BeTrue();
            await execution;
            executions.Should().Be(19);
        }

        [Test]
        public async Task Execute_CancelDuringPacing_DoesNotResetChildrenAndRunsTeardown() {
            var container = CreateLoop(20);
            var item = CreateItem();
            container.Add(item.Object);
            using var cancellation = new CancellationTokenSource();

            var execution = new SequentialStrategy().Execute(container, default, cancellation.Token);
            try {
                execution.IsCompleted.Should().BeFalse("the synchronous instructions should yield at the pacing delay");
                container.Iterations.Should().Be(10);
            } finally {
                cancellation.Cancel();
            }

            Func<Task> complete = () => execution.WaitAsync(TimeSpan.FromSeconds(5));
            await complete.Should().ThrowAsync<OperationCanceledException>();
            item.Verify(x => x.ResetProgress(), Times.Exactly(9));
            item.Verify(x => x.SequenceBlockTeardown(), Times.Once);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Execute_NestedTimeCutoff_PacesRestartsAndPreservesFittingSibling(bool includeSibling) {
            var outer = CreateLoop(15);
            var inner = new SequentialContainer();
            outer.Add(CreateTimeCondition());
            outer.Add(inner);
            inner.Add(CreateTimeCondition());
            var setup = CreateItem();
            var exposure = CreateItem(estimatedSeconds: 180);
            var sibling = CreateItem(estimatedSeconds: 20);
            inner.Add(setup.Object);
            inner.Add(exposure.Object);
            if (includeSibling) {
                outer.Add(sibling.Object);
            }
            var elapsed = Stopwatch.StartNew();

            await new SequentialStrategy().Execute(outer, default, default);

            elapsed.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(700));
            outer.Iterations.Should().Be(15);
            setup.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(15));
            exposure.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
            sibling.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), includeSibling ? Times.Exactly(15) : Times.Never());
        }

        [Test]
        public async Task Execute_NestedFiniteLoops_PreservesInnerResetAndIterationCounts() {
            var outer = CreateLoop(12);
            var inner = CreateLoop(2);
            var item = CreateItem();
            outer.Add(inner);
            inner.Add(item.Object);

            await new SequentialStrategy().Execute(outer, default, default);

            outer.Iterations.Should().Be(12);
            inner.Iterations.Should().Be(2);
            item.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(24));
        }

        [Test]
        public async Task Execute_DeadlineExpiresDuringPacing_RechecksBeforeRunningNextItem() {
            var container = CreateLoop(20);
            var condition = CreateTimeCondition();
            var item = CreateItem();
            container.Add(condition);
            container.Add(item.Object);

            var execution = new SequentialStrategy().Execute(container, default, default);
            Mock.Get(condition.DateTime).SetupGet(x => x.Now).Returns(new DateTime(2026, 10, 8, 18, 2, 0));
            await execution;

            container.Iterations.Should().Be(10);
            item.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
            item.Object.Status.Should().Be(SequenceEntityStatus.SKIPPED);
        }

        [Test]
        public async Task Execute_AfterReset_DoesNotRetainRapidRepeatCount() {
            var container = CreateLoop(11);
            container.Add(CreateItem().Object);
            var strategy = new SequentialStrategy();
            await strategy.Execute(container, default, default);
            container.ResetAll();
            ((LoopCondition)container.Conditions[0]).Iterations = 2;

            var execution = strategy.Execute(container, default, default);

            execution.IsCompletedSuccessfully.Should().BeTrue();
            await execution;
            container.Iterations.Should().Be(2);
        }

        private static SequentialContainer CreateLoop(int iterations) {
            var container = new SequentialContainer();
            container.Add(new LoopCondition { Iterations = iterations });
            return container;
        }

        private static Mock<ISequenceItem> CreateItem(Action? execute = null, int estimatedSeconds = 0) {
            var item = new Mock<ISequenceItem>();
            item.SetupProperty(x => x.Status, SequenceEntityStatus.CREATED);
            item.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromSeconds(estimatedSeconds));
            item.Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Callback(() => {
                    execute?.Invoke();
                    item.Object.Status = SequenceEntityStatus.FINISHED;
                })
                .Returns(Task.CompletedTask);
            item.Setup(x => x.ResetProgress()).Callback(() => item.Object.Status = SequenceEntityStatus.CREATED);
            item.Setup(x => x.Skip()).Callback(() => item.Object.Status = SequenceEntityStatus.SKIPPED);
            return item;
        }

        private static TimeCondition CreateTimeCondition() {
            var clock = new Mock<ICustomDateTime>();
            clock.SetupGet(x => x.Now).Returns(new DateTime(2026, 10, 8, 18, 0, 0));
            var provider = new Mock<IDateTimeProvider>();
            provider.Setup(x => x.GetDateTime(It.IsAny<ISequenceEntity>())).Returns(new DateTime(2026, 10, 8, 18, 1, 0));
            provider.Setup(x => x.GetRolloverTime(It.IsAny<ISequenceEntity>())).Returns(new TimeOnly(12, 0));
            return new TimeCondition(new[] { provider.Object }, provider.Object) { DateTime = clock.Object };
        }
    }
}