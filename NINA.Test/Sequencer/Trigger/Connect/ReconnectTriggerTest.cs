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
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger.Connect;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Trigger.Connect {

    [TestFixture]
    public class ReconnectTriggerTest {
        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IRotatorMediator> rotatorMediatorMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<ISwitchMediator> switchMediatorMock;
        private Mock<IFlatDeviceMediator> flatDeviceMediatorMock;
        private Mock<IWeatherDataMediator> weatherDataMediatorMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private Mock<ISequenceMediator> sequenceMediatorMock;
        private CameraInfo cameraInfo;
        private Func<object, EventArgs, Task> downloadTimeoutHandlers;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profile.CameraSettings.Id = "CameraId";

            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);

            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            rotatorMediatorMock = new Mock<IRotatorMediator>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            switchMediatorMock = new Mock<ISwitchMediator>();
            flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            weatherDataMediatorMock = new Mock<IWeatherDataMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            sequenceMediatorMock = new Mock<ISequenceMediator>();

            SetupCamera(connected: false, rescanIds: new[] { profile.CameraSettings.Id });
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        /// <summary>
        /// Verifies the Reconnect Trigger Fires When Selected Device Is Disconnected And Runs Connect Instruction scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ReconnectTrigger_FiresWhenSelectedDeviceIsDisconnectedAndRunsConnectInstruction() {
            ReconnectTrigger sut = CreateReconnectTrigger();
            sut.SelectedDevice = "Camera";

            sut.AllowMultiplePerSet.Should().BeTrue();
            sut.ShouldTrigger(Mock.Of<ISequenceItem>(), Mock.Of<ISequenceItem>()).Should().BeTrue();

            await sut.Execute(new SequentialContainer(), default, CancellationToken.None);

            cameraInfo.Connected.Should().BeTrue();
            cameraMediatorMock.Verify(x => x.Rescan(), Times.Once);
            cameraMediatorMock.Verify(x => x.Connect(), Times.Once);
            sut.ShouldTrigger(Mock.Of<ISequenceItem>(), Mock.Of<ISequenceItem>()).Should().BeFalse();
        }

        /// <summary>
        /// Verifies the Reconnect Trigger Clone Preserves Selected Device And Legacy Telescope Selection Migrates To Mount scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ReconnectTrigger_ClonePreservesSelectedDeviceAndLegacyTelescopeSelectionMigratesToMount() {
            ReconnectTrigger sut = CreateReconnectTrigger();
            sut.SelectedDevice = "Camera";

            ReconnectTrigger clone = (ReconnectTrigger)sut.Clone();
            clone.SelectedDevice.Should().Be("Camera");
            clone.ConnectEquipmentInstruction.Should().NotBeSameAs(sut.ConnectEquipmentInstruction);

            sut.SelectedDevice = "Telescope";
            sut.OnDeserialized(default);

            sut.SelectedDevice.Should().Be("Mount");
        }

        /// <summary>
        /// Verifies the Reconnect Trigger Validate Rejects Global Trigger Section scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ReconnectTrigger_ValidateRejectsGlobalTriggerSection() {
            ReconnectTrigger sut = CreateReconnectTrigger();
            sut.AttachNewParent(new SequenceRootContainer());

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().Contain(i => i.Contains("global trigger section"));
        }

        /// <summary>
        /// Verifies the Reconnect On Download Failure Camera Timeout Triggers Reconnect And Restores Camera State scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ReconnectOnDownloadFailure_CameraTimeoutTriggersReconnectAndRestoresCameraState() {
            SetupCamera(connected: true, rescanIds: new[] { profile.CameraSettings.Id });
            cameraInfo.CoolerOn = true;
            cameraInfo.TemperatureSetPoint = -10;
            cameraInfo.CanSetTemperature = true;
            cameraInfo.DewHeaterOn = true;
            cameraInfo.HasDewHeater = true;
            ReconnectOnDownloadFailure sut = CreateReconnectOnDownloadFailure();

            sut.SequenceBlockInitialize();
            await downloadTimeoutHandlers.Invoke(this, EventArgs.Empty);

            sut.ShouldTrigger(Mock.Of<ISequenceItem>(), Mock.Of<ISequenceItem>()).Should().BeTrue();

            await sut.Execute(new SequentialContainer(), default, CancellationToken.None);

            cameraInfo.Connected.Should().BeTrue();
            sut.ShouldTrigger(Mock.Of<ISequenceItem>(), Mock.Of<ISequenceItem>()).Should().BeFalse();
            cameraMediatorMock.Verify(x => x.Disconnect(), Times.Once);
            cameraMediatorMock.Verify(x => x.Rescan(), Times.Once);
            cameraMediatorMock.Verify(x => x.Connect(), Times.Once);
            cameraMediatorMock.Verify(x => x.CoolCamera(-10, TimeSpan.Zero, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            cameraMediatorMock.Verify(x => x.SetDewHeater(true), Times.Once);

            sut.SequenceBlockTeardown();
        }

        /// <summary>
        /// Verifies the Reconnect On Download Failure Root Camera Failure Triggers And Teardown Unsubscribes scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ReconnectOnDownloadFailure_RootCameraFailureTriggersAndTeardownUnsubscribes() {
            ReconnectOnDownloadFailure sut = CreateReconnectOnDownloadFailure();
            SequenceRootContainer root = new SequenceRootContainer();
            sut.AttachNewParent(root);

            sut.SequenceBlockInitialize();
            await root.RaiseFailureEvent(Mock.Of<ISequenceItem>(), new CameraDownloadFailedException("download failed"));

            sut.ShouldTrigger(Mock.Of<ISequenceItem>(), Mock.Of<ISequenceItem>()).Should().BeTrue();

            sut.SequenceBlockTeardown();

            downloadTimeoutHandlers.Should().BeNull();
        }

        private ReconnectTrigger CreateReconnectTrigger() {
            return new ReconnectTrigger(
                profileServiceMock.Object,
                cameraMediatorMock.Object,
                filterWheelMediatorMock.Object,
                focuserMediatorMock.Object,
                rotatorMediatorMock.Object,
                telescopeMediatorMock.Object,
                guiderMediatorMock.Object,
                switchMediatorMock.Object,
                flatDeviceMediatorMock.Object,
                weatherDataMediatorMock.Object,
                domeMediatorMock.Object,
                safetyMonitorMediatorMock.Object);
        }

        private ReconnectOnDownloadFailure CreateReconnectOnDownloadFailure() {
            return new ReconnectOnDownloadFailure(
                profileServiceMock.Object,
                cameraMediatorMock.Object,
                sequenceMediatorMock.Object);
        }

        private void SetupCamera(bool connected, IReadOnlyCollection<string> rescanIds) {
            downloadTimeoutHandlers = null;
            cameraInfo = new CameraInfo { Connected = connected };
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(() => cameraInfo);
            cameraMediatorMock.Setup(x => x.Rescan()).ReturnsAsync(new List<string>(rescanIds));
            cameraMediatorMock.Setup(x => x.Connect()).Callback(() => cameraInfo.Connected = true).ReturnsAsync(true);
            cameraMediatorMock.Setup(x => x.Disconnect()).Callback(() => cameraInfo.Connected = false).Returns(Task.CompletedTask);
            cameraMediatorMock.Setup(x => x.CoolCamera(It.IsAny<double>(), It.IsAny<TimeSpan>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            cameraMediatorMock.SetupAdd(x => x.DownloadTimeout += It.IsAny<Func<object, EventArgs, Task>>()).Callback<Func<object, EventArgs, Task>>(handler => downloadTimeoutHandlers += handler);
            cameraMediatorMock.SetupRemove(x => x.DownloadTimeout -= It.IsAny<Func<object, EventArgs, Task>>()).Callback<Func<object, EventArgs, Task>>(handler => downloadTimeoutHandlers -= handler);
        }
    }
}
