#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer {

    [TestFixture]
    public class SequencerTest {

        [Test]
        public void ctor_MainContainerSet() {
            var rootMock = new Mock<ISequenceRootContainer>();
            var sut = new NINA.Sequencer.Sequencer(rootMock.Object);

            sut.MainContainer.Should().Be(rootMock.Object);
        }

        [Test]
        public async Task Start_NoIssues_MainContainerExecuted() {
            var rootMock = new Mock<ISequenceRootContainer>();
            rootMock.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>());
            rootMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());
            var sut = new NINA.Sequencer.Sequencer(rootMock.Object);

            var progress = new Progress<ApplicationStatus>();
            var ct = new CancellationToken();
            await sut.Start(progress, ct);

            rootMock.Verify(x => x.Run(It.Is<IProgress<ApplicationStatus>>(p => p == progress), It.Is<CancellationToken>(c => c == ct)), Times.Once);
        }

        [Test]
        public async Task Start_NoIssues_HasChildTriggers_ItemsGetInitialized() {
            var rootMock = new Mock<ISequenceRootContainer>();
            var childContainerMock = new Mock<ISequenceContainer>();
            childContainerMock.SetupGet(x => x.Issues).Returns(new List<string>());
            var itemMock = new Mock<ISequenceItem>();
            childContainerMock.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { itemMock.Object });

            var items = new List<ISequenceItem>() {
                childContainerMock.Object
            };

            rootMock.Setup(x => x.GetItemsSnapshot()).Returns(items);
            rootMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());
            var sut = new NINA.Sequencer.Sequencer(rootMock.Object);

            var progress = new Progress<ApplicationStatus>();
            var ct = new CancellationToken();
            await sut.Start(progress, ct);

            itemMock.Verify(x => x.Initialize(), Times.Once);
        }

        [Test]
        public async Task Start_NoIssues_HasChildTriggers_TriggersGetInitialized() {
            var rootMock = new Mock<ISequenceRootContainer>();
            var childContainerMock = new Mock<ISequenceContainer>();
            childContainerMock.SetupGet(x => x.Issues).Returns(new List<string>());
            childContainerMock.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>());

            var triggerMock = new Mock<ISequenceTrigger>();
            childContainerMock.As<ITriggerable>().Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { triggerMock.Object });

            var items = new List<ISequenceItem>() {
                childContainerMock.Object
            };

            rootMock.Setup(x => x.GetItemsSnapshot()).Returns(items);
            rootMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());
            var sut = new NINA.Sequencer.Sequencer(rootMock.Object);

            var progress = new Progress<ApplicationStatus>();
            var ct = new CancellationToken();
            await sut.Start(progress, ct);

            triggerMock.Verify(x => x.Initialize(), Times.Once);
        }

        [Test]
        public async Task Start_NoIssues_HasChildConditions_ConditionsGetInitialized() {
            var rootMock = new Mock<ISequenceRootContainer>();
            var childContainerMock = new Mock<ISequenceContainer>();
            childContainerMock.SetupGet(x => x.Issues).Returns(new List<string>());
            childContainerMock.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>());

            var conditionMock = new Mock<ISequenceCondition>();
            childContainerMock.As<IConditionable>().Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { conditionMock.Object });

            var items = new List<ISequenceItem>() {
                childContainerMock.Object
            };

            rootMock.Setup(x => x.GetItemsSnapshot()).Returns(items);
            rootMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());
            var sut = new NINA.Sequencer.Sequencer(rootMock.Object);

            var progress = new Progress<ApplicationStatus>();
            var ct = new CancellationToken();
            await sut.Start(progress, ct);

            conditionMock.Verify(x => x.Initialize(), Times.Once);
        }

        [Test]
        public async Task Start_NoIssues_HasChildTriggers_TriggersGetTeardowned() {
            var rootMock = new Mock<ISequenceRootContainer>();
            var childContainerMock = new Mock<ISequenceContainer>();
            childContainerMock.SetupGet(x => x.Issues).Returns(new List<string>());
            childContainerMock.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>());

            var triggerMock = new Mock<ISequenceTrigger>();
            childContainerMock.As<ITriggerable>().Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { triggerMock.Object });

            var items = new List<ISequenceItem>() {
                childContainerMock.Object
            };

            rootMock.Setup(x => x.GetItemsSnapshot()).Returns(items);
            rootMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());
            var sut = new NINA.Sequencer.Sequencer(rootMock.Object);

            var progress = new Progress<ApplicationStatus>();
            var ct = new CancellationToken();
            await sut.Start(progress, ct);

            triggerMock.Verify(x => x.Teardown(), Times.Once);
        }

        [Test]
        public async Task Start_NoIssues_HasChildConditions_ConditionsGetTeardowned() {
            var rootMock = new Mock<ISequenceRootContainer>();
            var childContainerMock = new Mock<ISequenceContainer>();
            childContainerMock.SetupGet(x => x.Issues).Returns(new List<string>());
            childContainerMock.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>());

            var conditionMock = new Mock<ISequenceCondition>();
            childContainerMock.As<IConditionable>().Setup(x => x.GetConditionsSnapshot()).Returns(new List<ISequenceCondition>() { conditionMock.Object });

            var items = new List<ISequenceItem>() {
                childContainerMock.Object
            };

            rootMock.Setup(x => x.GetItemsSnapshot()).Returns(items);
            rootMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());
            var sut = new NINA.Sequencer.Sequencer(rootMock.Object);

            var progress = new Progress<ApplicationStatus>();
            var ct = new CancellationToken();
            await sut.Start(progress, ct);

            conditionMock.Verify(x => x.Teardown(), Times.Once);
        }

        [Test]
        public async Task Start_NoIssues_HasChildTriggers_ItemsGetTeardowned() {
            var rootMock = new Mock<ISequenceRootContainer>();
            var childContainerMock = new Mock<ISequenceContainer>();
            childContainerMock.SetupGet(x => x.Issues).Returns(new List<string>());
            var itemMock = new Mock<ISequenceItem>();
            childContainerMock.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { itemMock.Object });

            var items = new List<ISequenceItem>() {
                childContainerMock.Object
            };

            rootMock.Setup(x => x.GetItemsSnapshot()).Returns(items);
            rootMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());
            var sut = new NINA.Sequencer.Sequencer(rootMock.Object);

            var progress = new Progress<ApplicationStatus>();
            var ct = new CancellationToken();
            await sut.Start(progress, ct);

            itemMock.Verify(x => x.Teardown(), Times.Once);
        }
    }
}