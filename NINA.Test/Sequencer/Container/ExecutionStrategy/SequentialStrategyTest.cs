#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Test.Sequencer.Container.ExecutionStrategy {

    [TestFixture]
    public class SequentialStrategyTest {

        [Test]
        public async Task Execute_WithoutConditions_ExecutedOnce() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var item1Mock = new Mock<ISequenceItem>();

            item1Mock
                .Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                    .Callback(() => item1Mock.SetupGet(x => x.Status).Returns(SequenceEntityStatus.FINISHED));

            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Callback(() => item2Mock.SetupGet(x => x.Status).Returns(SequenceEntityStatus.FINISHED));
            containerMock.Object.Add(item1Mock.Object);
            containerMock.Object.Add(item2Mock.Object);

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_WithCondition_AllExecutedOnce() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock
                .SetupSequence(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()))
                .Returns(true)
                .Returns(true)
                .Returns(true)
                .Returns(false);
            containerMock.Object.Add(conditionMock.Object);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_WithCondition_AllEnabledExecutedOnce() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock
                .SetupSequence(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()))
                .Returns(true)
                .Returns(true)
                .Returns(true)
                .Returns(false);
            containerMock.Object.Add(conditionMock.Object);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item3Mock = new Mock<ISequenceItem>();
            item3Mock
                .Setup(x => x.Status)
                .Returns(SequenceEntityStatus.DISABLED);
            var items = new List<ISequenceItem>() { item3Mock.Object, item1Mock.Object, item3Mock.Object, item2Mock.Object, item3Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item3Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Execute_WithCondition_OnlyFirstEnabledExecuted() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock
                .SetupSequence(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()))
                .Returns(true)
                .Returns(true)
                .Returns(false);
            containerMock.Object.Add(conditionMock.Object);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item3Mock = new Mock<ISequenceItem>();
            item3Mock
                .Setup(x => x.Status)
                .Returns(SequenceEntityStatus.DISABLED);
            var items = new List<ISequenceItem>() { item3Mock.Object, item1Mock.Object, item3Mock.Object, item2Mock.Object, item3Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
            item3Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Execute_WithCondition_OnlyFirstExecuted() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var conditionMock = new Mock<ISequenceCondition>();
            conditionMock
                .SetupSequence(x => x.RunCheck(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>()))
                .Returns(true)
                .Returns(true)
                .Returns(false);
            containerMock.Object.Add(conditionMock.Object);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2Mock.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Execute_WithOneInstruction_FirstExecutes_NoPreviousPassedToRunTriggers() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var triggerableMock = containerMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);

            var items = new List<ISequenceItem>() { item1Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            triggerableMock.Verify(x => x.RunTriggers(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_WithTwoInstructions_FirstExecutes_NoPreviousPassedToRunTriggers_SecondExecutes_PreviousItemPassed() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var triggerableMock = containerMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            triggerableMock.Verify(x => x.RunTriggers(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            triggerableMock.Verify(x => x.RunTriggers(It.Is<ISequenceItem>(item => item == item1Mock.Object), It.Is<ISequenceItem>(item => item == item2Mock.Object), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_WithOneInstruction_FirstExecutes_NoPreviousPassedToCheckConditions() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { new Mock<ISequenceCondition>().Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);

            var items = new List<ISequenceItem>() { item1Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            // Will be called twice - once on start of container and once prior to execution
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object)), Times.Exactly(2));
            // Will be called once - after execution of the single item
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == item1Mock.Object), It.Is<ISequenceItem>(item => item == null)), Times.Once);
        }

        [Test]
        public async Task Execute_WithTwoInstructions_FirstExecutes_NoPreviousPassedToCheckConditions_SecondExecutes_PreviousItemPassed() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { new Mock<ISequenceCondition>().Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            // Will be called twice - once on start of container and once prior to execution
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object)), Times.Exactly(2));
            // Will be called once - after execution of the first item
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == item1Mock.Object), It.Is<ISequenceItem>(item => item == item2Mock.Object)), Times.Once);
            // Will be called once - after execution of the last item
            conditionableMock.Verify(x => x.CheckConditions(It.Is<ISequenceItem>(item => item == item2Mock.Object), It.Is<ISequenceItem>(item => item == null)), Times.Once);
        }

        [Test]
        public async Task Execute_BlockStarted_MultipleIterations_SequenceBlockInitializeCalled() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            var conditionMock = new Mock<ISequenceCondition>();
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { conditionMock.Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var triggerableMock = containerMock.As<ITriggerable>();
            var triggerMock = new Mock<ISequenceTrigger>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { triggerMock.Object });
            triggerableMock.Setup(x => x.RunTriggers(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            item2Mock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            conditionMock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            triggerMock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
        }

        [Test]
        public async Task Execute_BlockStarted_MultipleIterations_SequenceBlockInitialize_AlsoForDisabled_Called() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            var conditionMock = new Mock<ISequenceCondition>();
            var condition2Mock = new Mock<ISequenceCondition>();
            condition2Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { conditionMock.Object, condition2Mock.Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var triggerableMock = containerMock.As<ITriggerable>();
            var triggerMock = new Mock<ISequenceTrigger>();
            var trigger2Mock = new Mock<ISequenceTrigger>();
            trigger2Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { triggerMock.Object, trigger2Mock.Object });
            triggerableMock.Setup(x => x.RunTriggers(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item3Mock = new Mock<ISequenceItem>();
            item3Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object, item3Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            item2Mock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            item3Mock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            conditionMock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            condition2Mock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            triggerMock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
            trigger2Mock.Verify(x => x.SequenceBlockInitialize(), Times.Once);
        }

        [Test]
        public async Task Execute_BlockStarted_MultipleIterations_SequenceBlockTeardownCalled() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            var conditionMock = new Mock<ISequenceCondition>();
            var condition2Mock = new Mock<ISequenceCondition>();
            condition2Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { conditionMock.Object, condition2Mock.Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var triggerableMock = containerMock.As<ITriggerable>();
            var triggerMock = new Mock<ISequenceTrigger>();
            var trigger2Mock = new Mock<ISequenceTrigger>();
            trigger2Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { triggerMock.Object, trigger2Mock.Object });
            triggerableMock.Setup(x => x.RunTriggers(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item3Mock = new Mock<ISequenceItem>();
            item3Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object, item3Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.SequenceBlockTeardown(), Times.Once);
            item2Mock.Verify(x => x.SequenceBlockTeardown(), Times.Once);
            item3Mock.Verify(x => x.SequenceBlockTeardown(), Times.Once);
            conditionMock.Verify(x => x.SequenceBlockTeardown(), Times.Once);
            condition2Mock.Verify(x => x.SequenceBlockTeardown(), Times.Once);
            triggerMock.Verify(x => x.SequenceBlockTeardown(), Times.Once);
            trigger2Mock.Verify(x => x.SequenceBlockTeardown(), Times.Once);
        }

        [Test]
        public async Task Execute_BlockStarted_MultipleIterations_SequenceBlockStartedCalled() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            var conditionMock = new Mock<ISequenceCondition>();
            var condition2Mock = new Mock<ISequenceCondition>();
            condition2Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { conditionMock.Object, condition2Mock.Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var triggerableMock = containerMock.As<ITriggerable>();
            var triggerMock = new Mock<ISequenceTrigger>();
            var trigger2Mock = new Mock<ISequenceTrigger>();
            trigger2Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { triggerMock.Object, trigger2Mock.Object });
            triggerableMock.Setup(x => x.RunTriggers(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item3Mock = new Mock<ISequenceItem>();
            item3Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object, item3Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.SequenceBlockStarted(), Times.Exactly(3));
            item2Mock.Verify(x => x.SequenceBlockStarted(), Times.Exactly(3));
            item3Mock.Verify(x => x.SequenceBlockStarted(), Times.Exactly(3));
            conditionMock.Verify(x => x.SequenceBlockStarted(), Times.Exactly(3));
            condition2Mock.Verify(x => x.SequenceBlockStarted(), Times.Exactly(3));
            triggerMock.Verify(x => x.SequenceBlockStarted(), Times.Exactly(3));
            trigger2Mock.Verify(x => x.SequenceBlockStarted(), Times.Exactly(3));
        }

        [Test]
        public async Task Execute_BlockStarted_MultipleIterations_SequenceBlockFinishedCalled() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);

            var conditionableMock = containerMock.As<IConditionable>();
            var conditionMock = new Mock<ISequenceCondition>();
            var condition2Mock = new Mock<ISequenceCondition>();
            condition2Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            conditionableMock.Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { conditionMock.Object, condition2Mock.Object });
            conditionableMock.Setup(x => x.CheckConditions(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>())).Returns(true);

            var triggerableMock = containerMock.As<ITriggerable>();
            var triggerMock = new Mock<ISequenceTrigger>();
            var trigger2Mock = new Mock<ISequenceTrigger>();
            trigger2Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { triggerMock.Object, trigger2Mock.Object });
            triggerableMock.Setup(x => x.RunTriggers(It.IsAny<ISequenceItem>(), It.IsAny<ISequenceItem>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item2Mock = new Mock<ISequenceItem>();
            item2Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED)
                .Returns(SequenceEntityStatus.FINISHED);
            var item3Mock = new Mock<ISequenceItem>();
            item3Mock.Setup(x => x.Status).Returns(SequenceEntityStatus.DISABLED);
            var items = new List<ISequenceItem>() { item1Mock.Object, item2Mock.Object, item3Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            item1Mock.Verify(x => x.SequenceBlockFinished(), Times.Exactly(3));
            item2Mock.Verify(x => x.SequenceBlockFinished(), Times.Exactly(3));
            item3Mock.Verify(x => x.SequenceBlockFinished(), Times.Exactly(3));
            conditionMock.Verify(x => x.SequenceBlockFinished(), Times.Exactly(3));
            condition2Mock.Verify(x => x.SequenceBlockFinished(), Times.Exactly(3));
            triggerMock.Verify(x => x.SequenceBlockFinished(), Times.Exactly(3));
            trigger2Mock.Verify(x => x.SequenceBlockFinished(), Times.Exactly(3));
        }

        [Test]
        public async Task Execute_WithOneInstruction_FirstExecutes_NormalTriggerableEvaluatedOnce_AfterTriggerableEvaluatedOnce() {
            var containerMock = new Mock<SequenceContainer>(new Mock<IExecutionStrategy>().Object);
            var triggerableMock = containerMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());

            var rootMock = new Mock<ISequenceRootContainer>();
            var triggerableRootMock = rootMock.As<ITriggerable>();
            triggerableRootMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());

            containerMock.Object.AttachNewParent(rootMock.Object);

            var item1Mock = new Mock<ISequenceItem>();
            item1Mock
                .SetupSequence(x => x.Status)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.CREATED)
                .Returns(SequenceEntityStatus.FINISHED);

            var items = new List<ISequenceItem>() { item1Mock.Object };
            foreach (var item in items) {
                containerMock.Object.Items.Add(item);
            }

            var sut = new SequentialStrategy();
            await sut.Execute(containerMock.Object, default, default);

            triggerableMock.Verify(x => x.RunTriggers(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            triggerableMock.Verify(x => x.RunTriggersAfter(It.Is<ISequenceItem>(item => item == item1Mock.Object), It.Is<ISequenceItem>(item => item == null), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);

            triggerableRootMock.Verify(x => x.RunTriggers(It.Is<ISequenceItem>(item => item == null), It.Is<ISequenceItem>(item => item == item1Mock.Object), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            triggerableRootMock.Verify(x => x.RunTriggersAfter(It.Is<ISequenceItem>(item => item == item1Mock.Object), It.Is<ISequenceItem>(item => item == null), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}