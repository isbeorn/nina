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
using NINA.Astrometry.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ISequencer = NINA.Sequencer.ISequencer;
using SequenceMediator = NINA.Sequencer.Mediator.SequenceMediator;

namespace NINA.Test.Sequencer.Mediator {

    [TestFixture]
    public class SequenceMediatorTest {
        private SequenceMediator sut;
        private Mock<ISequenceNavigationVM> navigationMock;
        private Mock<ISequence2VM> sequence2Mock;
        private Mock<ISimpleSequenceVM> simpleSequenceMock;
        private Mock<IAsyncCommand> startSequenceCommandMock;
        private Mock<ICommand> cancelSequenceCommandMock;

        [SetUp]
        public void SetUp() {
            sut = new SequenceMediator();
            navigationMock = new Mock<ISequenceNavigationVM>();
            sequence2Mock = new Mock<ISequence2VM>();
            simpleSequenceMock = new Mock<ISimpleSequenceVM>();
            startSequenceCommandMock = new Mock<IAsyncCommand>();
            cancelSequenceCommandMock = new Mock<ICommand>();

            navigationMock.SetupGet(x => x.Sequence2VM).Returns(sequence2Mock.Object);
            navigationMock.SetupGet(x => x.SimpleSequenceVM).Returns(simpleSequenceMock.Object);
            sequence2Mock.SetupGet(x => x.StartSequenceCommand).Returns(startSequenceCommandMock.Object);
            sequence2Mock.SetupGet(x => x.CancelSequenceCommand).Returns(cancelSequenceCommandMock.Object);
            startSequenceCommandMock.Setup(x => x.ExecuteAsync(It.IsAny<object>())).Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Verifies the Register Sequence Navigation Allows Only One Registration scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void RegisterSequenceNavigation_AllowsOnlyOneRegistration() {
            sut.RegisterSequenceNavigation(navigationMock.Object);

            Action act = () => sut.RegisterSequenceNavigation(navigationMock.Object);

            act.Should().Throw<Exception>()
                .WithMessage("*already registered*");
        }

        /// <summary>
        /// Verifies the Members Throw When Registered Navigation Is Not Initialized scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void MembersThrowWhenRegisteredNavigationIsNotInitialized() {
            navigationMock.SetupGet(x => x.Initialized).Returns(false);
            sut.RegisterSequenceNavigation(navigationMock.Object);

            sut.Initialized.Should().BeFalse();
            sut.GetAdvancedSequencerCurrentRunningItems().Should().BeNull();

            Action act = () => sut.SwitchToAdvancedView();

            act.Should().Throw<Exception>()
                .WithMessage("*not initialized*");
        }

        /// <summary>
        /// Verifies the Initialized Mediator Delegates Navigation And Sequence Operations scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task InitializedMediator_DelegatesNavigationAndSequenceOperations() {
            navigationMock.SetupGet(x => x.Initialized).Returns(true);
            sequence2Mock.SetupGet(x => x.IsRunning).Returns(false);
            sequence2Mock.Setup(x => x.SaveContainer(It.IsAny<ISequenceContainer>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            sequence2Mock.Setup(x => x.GetAdvancedSequencerSavePath()).Returns("sequence.json");
            List<IDeepSkyObjectContainer> advancedTemplates = new List<IDeepSkyObjectContainer> { Mock.Of<IDeepSkyObjectContainer>() };
            List<IDeepSkyObjectContainer> advancedTargets = new List<IDeepSkyObjectContainer> { Mock.Of<IDeepSkyObjectContainer>() };
            List<IDeepSkyObjectContainer> simpleTargets = new List<IDeepSkyObjectContainer> { Mock.Of<IDeepSkyObjectContainer>() };
            navigationMock.Setup(x => x.GetDeepSkyObjectContainerTemplates()).Returns(advancedTemplates);
            navigationMock.Setup(x => x.GetAllTargetsInAdvancedSequence()).Returns(advancedTargets);
            navigationMock.Setup(x => x.GetAllTargetsInSimpleSequence()).Returns(simpleTargets);
            SequenceRootContainer root = new SequenceRootContainer();
            UnknownSequenceItem runningItem = new UnknownSequenceItem("Running");
            root.AddRunningItem(runningItem);
            Mock<ISequencer> sequencerMock = new Mock<ISequencer>();
            sequencerMock.SetupGet(x => x.MainContainer).Returns(root);
            sequence2Mock.SetupGet(x => x.Sequencer).Returns(sequencerMock.Object);
            sut.RegisterSequenceNavigation(navigationMock.Object);

            IDeepSkyObject deepSkyObject = Mock.Of<IDeepSkyObject>();
            IDeepSkyObjectContainer container = Mock.Of<IDeepSkyObjectContainer>();
            ISequenceRootContainer rootContainer = Mock.Of<ISequenceRootContainer>();

            sut.AddSimpleTarget(deepSkyObject);
            sut.AddAdvancedTarget(container);
            sut.SetAdvancedSequence(rootContainer);
            sut.SwitchToAdvancedView();
            sut.SwitchToOverview();
            sut.AddTargetToTargetList(container);
            await sut.StartAdvancedSequence(skipValidation: true);
            sut.CancelAdvancedSequence();
            await sut.SaveContainer(container, "sequence.json", CancellationToken.None);

            navigationMock.Verify(x => x.AddSimpleTarget(deepSkyObject), Times.Once);
            navigationMock.Verify(x => x.AddAdvancedTarget(container), Times.Once);
            navigationMock.Verify(x => x.SetAdvancedSequence(rootContainer), Times.Once);
            navigationMock.Verify(x => x.SwitchToAdvancedView(), Times.Once);
            navigationMock.Verify(x => x.SwitchToOverview(), Times.Once);
            navigationMock.Verify(x => x.AddTargetToTargetList(container), Times.Once);
            startSequenceCommandMock.Verify(x => x.ExecuteAsync(true), Times.Once);
            cancelSequenceCommandMock.Verify(x => x.Execute(null), Times.Once);
            sut.GetDeepSkyObjectContainerTemplates().Should().BeSameAs(advancedTemplates);
            sut.GetAllTargetsInAdvancedSequence().Should().BeSameAs(advancedTargets);
            sut.GetAllTargetsInSimpleSequence().Should().BeSameAs(simpleTargets);
            sut.GetAdvancedSequencerSavePath().Should().Be("sequence.json");
            sut.GetAdvancedSequencerCurrentRunningItems().Should().ContainSingle().Which.Should().BeSameAs(runningItem);
        }

        /// <summary>
        /// Verifies the Start Advanced Sequence Rejects Start When Already Running scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task StartAdvancedSequence_RejectsStartWhenAlreadyRunning() {
            navigationMock.SetupGet(x => x.Initialized).Returns(true);
            sequence2Mock.SetupGet(x => x.IsRunning).Returns(true);
            sut.RegisterSequenceNavigation(navigationMock.Object);

            Func<Task> act = () => sut.StartAdvancedSequence(skipValidation: false);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*still running*");
        }

        /// <summary>
        /// Verifies the Sequence Events Are Forwarded To Advanced And Simple View Models scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceEvents_AreForwardedToAdvancedAndSimpleViewModels() {
            navigationMock.SetupGet(x => x.Initialized).Returns(true);
            sut.RegisterSequenceNavigation(navigationMock.Object);
            Func<object, EventArgs, Task> handler = (sender, args) => Task.CompletedTask;

            sut.SequenceStarting += handler;
            sut.SequenceStarting -= handler;
            sut.SequenceFinished += handler;
            sut.SequenceFinished -= handler;

            sequence2Mock.VerifyAdd(x => x.SequenceStarting += handler, Times.Once);
            sequence2Mock.VerifyRemove(x => x.SequenceStarting -= handler, Times.Once);
            sequence2Mock.VerifyAdd(x => x.SequenceFinished += handler, Times.Once);
            sequence2Mock.VerifyRemove(x => x.SequenceFinished -= handler, Times.Once);
            simpleSequenceMock.VerifyAdd(x => x.SequenceStarting += handler, Times.Once);
            simpleSequenceMock.VerifyRemove(x => x.SequenceStarting -= handler, Times.Once);
            simpleSequenceMock.VerifyAdd(x => x.SequenceFinished += handler, Times.Once);
            simpleSequenceMock.VerifyRemove(x => x.SequenceFinished -= handler, Times.Once);
        }
    }
}
