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
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.SequenceItem.Connect;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Connect {

    [TestFixture]
    public class ConnectEquipmentTest {
        private Mock<IProfileService> profileServiceMock;
        private NINA.Profile.Profile profile;
        private AsyncObservableCollection<ProfileMeta> profiles;

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

        private CameraInfo cameraInfo;
        private GuiderInfo guiderInfo;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile {
                Id = Guid.NewGuid(),
                Name = "Active"
            };
            profile.CameraSettings.Id = "CameraId";
            profile.FilterWheelSettings.Id = "FilterWheelId";
            profile.FocuserSettings.Id = "FocuserId";
            profile.RotatorSettings.Id = "RotatorId";
            profile.TelescopeSettings.Id = "MountId";
            profile.GuiderSettings.GuiderName = "GuiderId";
            profile.SwitchSettings.Id = "SwitchId";
            profile.FlatDeviceSettings.Id = "FlatDeviceId";
            profile.WeatherDataSettings.Id = "WeatherId";
            profile.DomeSettings.Id = "DomeId";
            profile.SafetyMonitorSettings.Id = "SafetyMonitorId";

            profiles = new AsyncObservableCollection<ProfileMeta> {
                new ProfileMeta { Id = profile.Id, Name = profile.Name }
            };

            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            profileServiceMock.SetupGet(x => x.Profiles).Returns(profiles);

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

            SetupCamera(connected: false, rescanIds: new[] { profile.CameraSettings.Id });
            SetupGuider(connected: false, rescanIds: new[] { profile.GuiderSettings.GuiderName });
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        /// <summary>
        /// Verifies the Connect Equipment Get Profile Id Returns Selected Device Profile Id scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ConnectEquipment_GetProfileId_ReturnsSelectedDeviceProfileId() {
            ConnectEquipment sut = CreateConnectEquipment();

            IReadOnlyDictionary<string, string> expectedIds = new Dictionary<string, string> {
                ["Camera"] = profile.CameraSettings.Id,
                ["Filter Wheel"] = profile.FilterWheelSettings.Id,
                ["Focuser"] = profile.FocuserSettings.Id,
                ["Rotator"] = profile.RotatorSettings.Id,
                ["Telescope"] = profile.TelescopeSettings.Id,
                ["Mount"] = profile.TelescopeSettings.Id,
                ["Guider"] = profile.GuiderSettings.GuiderName,
                ["Switch"] = profile.SwitchSettings.Id,
                ["Flat Panel"] = profile.FlatDeviceSettings.Id,
                ["Weather"] = profile.WeatherDataSettings.Id,
                ["Dome"] = profile.DomeSettings.Id,
                ["Safety Monitor"] = profile.SafetyMonitorSettings.Id
            };

            foreach (KeyValuePair<string, string> expected in expectedIds) {
                sut.SelectedDevice = expected.Key;
                sut.GetProfileId().Should().Be(expected.Value, expected.Key);
            }
        }

        /// <summary>
        /// Verifies the Connect Equipment Execute Rescans And Connects Disconnected Selected Device scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ConnectEquipment_Execute_RescansAndConnectsDisconnectedSelectedDevice() {
            ConnectEquipment sut = CreateConnectEquipment();
            sut.SelectedDevice = "Camera";

            await sut.Execute(default, CancellationToken.None);

            cameraInfo.Connected.Should().BeTrue();
            cameraMediatorMock.Verify(x => x.Rescan(), Times.Once);
            cameraMediatorMock.Verify(x => x.Connect(), Times.Once);
        }

        /// <summary>
        /// Verifies the Connect Equipment Execute Skips Rescan When Device Already Connected scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ConnectEquipment_Execute_SkipsRescanWhenDeviceAlreadyConnected() {
            SetupCamera(connected: true, rescanIds: new[] { profile.CameraSettings.Id });
            ConnectEquipment sut = CreateConnectEquipment();
            sut.SelectedDevice = "Camera";

            await sut.Execute(default, CancellationToken.None);

            cameraMediatorMock.Verify(x => x.Rescan(), Times.Never);
            cameraMediatorMock.Verify(x => x.Connect(), Times.Never);
        }

        /// <summary>
        /// Verifies the Connect Equipment Execute Throws When Stored Device Is Not Found scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ConnectEquipment_Execute_ThrowsWhenStoredDeviceIsNotFound() {
            SetupCamera(connected: false, rescanIds: Array.Empty<string>());
            ConnectEquipment sut = CreateConnectEquipment();
            sut.SelectedDevice = "Camera";

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*not found*");
        }

        /// <summary>
        /// Verifies the Connect Equipment Validate Reports Missing Profile Device Id scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ConnectEquipment_Validate_ReportsMissingProfileDeviceId() {
            profile.CameraSettings.Id = "No_Device";
            ConnectEquipment sut = CreateConnectEquipment();
            sut.SelectedDevice = "Camera";

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().ContainSingle(i => i.Contains("no device id"));
        }

        /// <summary>
        /// Verifies the Connect Equipment On Deserialized Renames Legacy Telescope Selection To Mount scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ConnectEquipment_OnDeserialized_RenamesLegacyTelescopeSelectionToMount() {
            ConnectEquipment sut = CreateConnectEquipment();
            sut.SelectedDevice = "Telescope";

            sut.OnDeserialized(default);

            sut.SelectedDevice.Should().Be("Mount");
            sut.GetProfileId().Should().Be(profile.TelescopeSettings.Id);
        }

        /// <summary>
        /// Verifies the Connect All Equipment Execute Connects Devices With Stored Ids And Skips No Device Profiles scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ConnectAllEquipment_Execute_ConnectsDevicesWithStoredIdsAndSkipsNoDeviceProfiles() {
            profile.GuiderSettings.GuiderName = "No_Guider";
            ConnectAllEquipment sut = CreateConnectAllEquipment();
            sut.Devices.Clear();
            sut.Devices.Add("Camera");
            sut.Devices.Add("Guider");

            await sut.Execute(default, CancellationToken.None);

            cameraInfo.Connected.Should().BeTrue();
            cameraMediatorMock.Verify(x => x.Connect(), Times.Once);
            guiderMediatorMock.Verify(x => x.Rescan(), Times.Never);
            guiderMediatorMock.Verify(x => x.Connect(), Times.Never);
        }

        /// <summary>
        /// Verifies the Connect All Equipment Execute Aggregates Connection Failures scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task ConnectAllEquipment_Execute_AggregatesConnectionFailures() {
            SetupCamera(connected: false, rescanIds: Array.Empty<string>());
            ConnectAllEquipment sut = CreateConnectAllEquipment();
            sut.Devices.Clear();
            sut.Devices.Add("Camera");

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<AggregateException>()
                .Where(ex => ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0].Message.Contains("not found"));
        }

        /// <summary>
        /// Verifies the Disconnect Equipment Execute Disconnects Connected Selected Device scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task DisconnectEquipment_Execute_DisconnectsConnectedSelectedDevice() {
            SetupCamera(connected: true, rescanIds: new[] { profile.CameraSettings.Id });
            DisconnectEquipment sut = CreateDisconnectEquipment();
            sut.SelectedDevice = "Camera";

            await sut.Execute(default, CancellationToken.None);

            cameraInfo.Connected.Should().BeFalse();
            cameraMediatorMock.Verify(x => x.Disconnect(), Times.Once);
        }

        /// <summary>
        /// Verifies the Disconnect Equipment Execute Skips Device That Is Already Disconnected scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task DisconnectEquipment_Execute_SkipsDeviceThatIsAlreadyDisconnected() {
            SetupCamera(connected: false, rescanIds: new[] { profile.CameraSettings.Id });
            DisconnectEquipment sut = CreateDisconnectEquipment();
            sut.SelectedDevice = "Camera";

            await sut.Execute(default, CancellationToken.None);

            cameraMediatorMock.Verify(x => x.Disconnect(), Times.Never);
        }

        /// <summary>
        /// Verifies the Disconnect All Equipment Execute Disconnects Connected Devices Only scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task DisconnectAllEquipment_Execute_DisconnectsConnectedDevicesOnly() {
            SetupCamera(connected: true, rescanIds: new[] { profile.CameraSettings.Id });
            SetupGuider(connected: false, rescanIds: new[] { profile.GuiderSettings.GuiderName });
            DisconnectAllEquipment sut = CreateDisconnectAllEquipment();
            sut.Devices.Clear();
            sut.Devices.Add("Camera");
            sut.Devices.Add("Guider");

            await sut.Execute(default, CancellationToken.None);

            cameraMediatorMock.Verify(x => x.Disconnect(), Times.Once);
            guiderMediatorMock.Verify(x => x.Disconnect(), Times.Never);
        }

        /// <summary>
        /// Verifies the Switch Profile Execute Disconnects Connected Devices Selects Profile And Skips Reconnect When Disabled scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task SwitchProfile_Execute_DisconnectsConnectedDevicesSelectsProfileAndSkipsReconnectWhenDisabled() {
            SetupCamera(connected: true, rescanIds: new[] { profile.CameraSettings.Id });
            ProfileMeta targetProfile = new ProfileMeta { Id = Guid.NewGuid(), Name = "Target" };
            profiles.Add(targetProfile);
            SwitchProfile sut = CreateSwitchProfile();
            sut.Devices.Clear();
            sut.Devices.Add("Camera");
            sut.SelectedProfileId = targetProfile.Id;
            sut.Reconnect = false;

            await sut.Execute(default, CancellationToken.None);

            cameraMediatorMock.Verify(x => x.Disconnect(), Times.Once);
            cameraMediatorMock.Verify(x => x.Connect(), Times.Never);
            profileServiceMock.Verify(x => x.SelectProfile(It.Is<ProfileMeta>(p => p.Id == targetProfile.Id)), Times.Once);
        }

        /// <summary>
        /// Verifies the Switch Profile Execute Skips When No Profile Is Selected scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task SwitchProfile_Execute_SkipsWhenNoProfileIsSelected() {
            SwitchProfile sut = CreateSwitchProfile();
            sut.SelectedProfileId = Guid.Empty;

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceItemSkippedException>()
                .WithMessage("*No profile*");
        }

        /// <summary>
        /// Verifies connect-all profile lookup covers every supported device label, including the legacy telescope/mount alias.
        /// </summary>
        [Test]
        public void ConnectAllEquipment_GetProfileId_ReturnsStoredIdsForEveryDevice() {
            ConnectAllEquipment sut = CreateConnectAllEquipment();
            IReadOnlyDictionary<string, string> expectedIds = new Dictionary<string, string> {
                ["Camera"] = profile.CameraSettings.Id,
                ["Filter Wheel"] = profile.FilterWheelSettings.Id,
                ["Focuser"] = profile.FocuserSettings.Id,
                ["Rotator"] = profile.RotatorSettings.Id,
                ["Telescope"] = profile.TelescopeSettings.Id,
                ["Mount"] = profile.TelescopeSettings.Id,
                ["Guider"] = profile.GuiderSettings.GuiderName,
                ["Switch"] = profile.SwitchSettings.Id,
                ["Flat Panel"] = profile.FlatDeviceSettings.Id,
                ["Weather"] = profile.WeatherDataSettings.Id,
                ["Dome"] = profile.DomeSettings.Id,
                ["Safety Monitor"] = profile.SafetyMonitorSettings.Id
            };

            foreach (KeyValuePair<string, string> expected in expectedIds) {
                sut.GetProfileId(expected.Key).Should().Be(expected.Value, expected.Key);
            }

            sut.GetProfileId("Unknown").Should().BeNull();
        }

        /// <summary>
        /// Verifies connect-all cloning, validation, and diagnostic text preserve the instruction contract without sharing the instance.
        /// </summary>
        [Test]
        public void ConnectAllEquipment_CloneValidateAndToString_UseBaseInstructionState() {
            ConnectAllEquipment sut = CreateConnectAllEquipment();
            sut.Name = "Connect everything";

            ConnectAllEquipment clone = (ConnectAllEquipment)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be(sut.Name);
            sut.Validate().Should().BeTrue();
            sut.ToString().Should().Contain(nameof(ConnectAllEquipment));
        }

        /// <summary>
        /// Verifies disconnecting one selected device reports a failure when the mediator still reports it connected afterwards.
        /// </summary>
        [Test]
        public async Task DisconnectEquipment_Execute_ThrowsWhenDeviceRemainsConnected() {
            cameraInfo = new CameraInfo { Connected = true };
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(() => cameraInfo);
            cameraMediatorMock.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            DisconnectEquipment sut = CreateDisconnectEquipment();
            sut.SelectedDevice = "Camera";

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*Failed to disconnect*");
        }

        /// <summary>
        /// Verifies disconnect instruction migration, cloning, validation, and text include the selected device.
        /// </summary>
        [Test]
        public void DisconnectEquipment_MigrationCloneValidateAndToString_PreserveSelectedDevice() {
            DisconnectEquipment sut = CreateDisconnectEquipment();
            sut.SelectedDevice = "Telescope";

            sut.OnDeserialized(default);
            DisconnectEquipment clone = (DisconnectEquipment)sut.Clone();

            sut.SelectedDevice.Should().Be("Mount");
            clone.SelectedDevice.Should().Be("Mount");
            sut.Validate().Should().BeTrue();
            sut.Issues.Should().BeEmpty();
            sut.ToString().Should().Contain("Mount");
        }

        /// <summary>
        /// Verifies disconnect-all aggregates mediator failures when a device remains connected after disconnect is requested.
        /// </summary>
        [Test]
        public async Task DisconnectAllEquipment_Execute_AggregatesDisconnectFailures() {
            cameraInfo = new CameraInfo { Connected = true };
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(() => cameraInfo);
            cameraMediatorMock.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            DisconnectAllEquipment sut = CreateDisconnectAllEquipment();
            sut.Devices.Clear();
            sut.Devices.Add("Camera");

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<AggregateException>()
                .Where(ex => ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0].Message.Contains("Camera"));
        }

        /// <summary>
        /// Verifies disconnect-all cloning, validation, and diagnostic text preserve the instruction contract.
        /// </summary>
        [Test]
        public void DisconnectAllEquipment_CloneValidateAndToString_UseBaseInstructionState() {
            DisconnectAllEquipment sut = CreateDisconnectAllEquipment();
            sut.Name = "Disconnect everything";

            DisconnectAllEquipment clone = (DisconnectAllEquipment)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be(sut.Name);
            sut.Validate().Should().BeTrue();
            sut.ToString().Should().Contain(nameof(DisconnectAllEquipment));
        }

        /// <summary>
        /// Verifies switch-profile profile lookup covers every supported device label and reports null for unknown labels.
        /// </summary>
        [Test]
        public void SwitchProfile_GetProfileId_ReturnsStoredIdsForEveryDevice() {
            SwitchProfile sut = CreateSwitchProfile();
            IReadOnlyDictionary<string, string> expectedIds = new Dictionary<string, string> {
                ["Camera"] = profile.CameraSettings.Id,
                ["Filter Wheel"] = profile.FilterWheelSettings.Id,
                ["Focuser"] = profile.FocuserSettings.Id,
                ["Rotator"] = profile.RotatorSettings.Id,
                ["Telescope"] = profile.TelescopeSettings.Id,
                ["Guider"] = profile.GuiderSettings.GuiderName,
                ["Switch"] = profile.SwitchSettings.Id,
                ["Flat Panel"] = profile.FlatDeviceSettings.Id,
                ["Weather"] = profile.WeatherDataSettings.Id,
                ["Dome"] = profile.DomeSettings.Id,
                ["Safety Monitor"] = profile.SafetyMonitorSettings.Id
            };

            foreach (KeyValuePair<string, string> expected in expectedIds) {
                sut.GetProfileId(expected.Key).Should().Be(expected.Value, expected.Key);
            }

            sut.GetProfileId("Unknown").Should().BeNull();
        }

        /// <summary>
        /// Verifies switch-profile skips when the selected profile is already active.
        /// </summary>
        [Test]
        public async Task SwitchProfile_Execute_SkipsWhenSelectedProfileIsAlreadyActive() {
            SwitchProfile sut = CreateSwitchProfile();
            sut.SelectedProfileId = profile.Id;

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceItemSkippedException>()
                .WithMessage("*already active*");
        }

        /// <summary>
        /// Verifies switch-profile fails when the selected profile id is not present in the profile service.
        /// </summary>
        [Test]
        public async Task SwitchProfile_Execute_FailsWhenSelectedProfileIsUnknown() {
            SwitchProfile sut = CreateSwitchProfile();
            sut.Devices.Clear();
            sut.SelectedProfileId = Guid.NewGuid();

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceEntityFailedException>()
                .WithMessage("*Unknown profile*");
        }

        /// <summary>
        /// Verifies switch-profile cloning, validation, and diagnostic text preserve selected profile and reconnect settings.
        /// </summary>
        [Test]
        public void SwitchProfile_CloneValidateAndToString_PreserveSelectedProfileAndReconnect() {
            ProfileMeta targetProfile = new ProfileMeta { Id = Guid.NewGuid(), Name = "Target" };
            profiles.Add(targetProfile);
            SwitchProfile sut = CreateSwitchProfile();
            sut.SelectedProfileId = targetProfile.Id;
            sut.Reconnect = false;

            SwitchProfile clone = (SwitchProfile)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.SelectedProfileId.Should().Be(targetProfile.Id);
            clone.Reconnect.Should().BeFalse();
            sut.Validate().Should().BeTrue();
            sut.ToString().Should().Contain(nameof(SwitchProfile)).And.Contain(targetProfile.Id.ToString());
        }

        private ConnectEquipment CreateConnectEquipment() {
            return new ConnectEquipment(
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

        private ConnectAllEquipment CreateConnectAllEquipment() {
            return new ConnectAllEquipment(
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

        private DisconnectEquipment CreateDisconnectEquipment() {
            return new DisconnectEquipment(
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

        private DisconnectAllEquipment CreateDisconnectAllEquipment() {
            return new DisconnectAllEquipment(
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

        private SwitchProfile CreateSwitchProfile() {
            return new SwitchProfile(
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

        private void SetupCamera(bool connected, IReadOnlyCollection<string> rescanIds) {
            cameraInfo = new CameraInfo { Connected = connected };
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(() => cameraInfo);
            cameraMediatorMock.Setup(x => x.Rescan()).ReturnsAsync(new List<string>(rescanIds));
            cameraMediatorMock.Setup(x => x.Connect()).Callback(() => cameraInfo.Connected = true).ReturnsAsync(true);
            cameraMediatorMock.Setup(x => x.Disconnect()).Callback(() => cameraInfo.Connected = false).Returns(Task.CompletedTask);
        }

        private void SetupGuider(bool connected, IReadOnlyCollection<string> rescanIds) {
            guiderInfo = new GuiderInfo { Connected = connected };
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(() => guiderInfo);
            guiderMediatorMock.Setup(x => x.Rescan()).ReturnsAsync(new List<string>(rescanIds));
            guiderMediatorMock.Setup(x => x.Connect()).Callback(() => guiderInfo.Connected = true).ReturnsAsync(true);
            guiderMediatorMock.Setup(x => x.Disconnect()).Callback(() => guiderInfo.Connected = false).Returns(Task.CompletedTask);
        }
    }
}
