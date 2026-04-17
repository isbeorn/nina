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
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFocuser;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class DeviceContractBehaviorTest {

        /// <summary>
        /// Verifies DeviceInfo reset restores default values for both base and derived writable properties.
        /// </summary>
        [Test]
        public void DeviceInfo_ResetCopiesDefaultDerivedState() {
            var info = new CameraInfo {
                Connected = true,
                Name = "Camera",
                DeviceId = "id",
                Temperature = -10,
                Gain = 120,
                BinX = 2,
                BinY = 2,
                SensorType = NINA.Core.Enum.SensorType.RGGB
            };

            info.Reset();

            info.Connected.Should().BeFalse();
            info.Name.Should().BeNull();
            info.DeviceId.Should().BeNull();
            info.Temperature.Should().Be(0);
            info.Gain.Should().Be(-1);
            info.BinX.Should().Be(0);
            info.BinY.Should().Be(0);
            info.SensorType.Should().Be(NINA.Core.Enum.SensorType.Monochrome);
        }

        /// <summary>
        /// Verifies DeviceInfo copy only copies writable properties available on the derived runtime type.
        /// </summary>
        [Test]
        public void DeviceInfo_CopyFromCopiesDerivedWritableProperties() {
            var source = new FocuserInfo {
                Connected = true,
                Name = "Focuser",
                Position = 1234,
                StepSize = 2.5,
                Temperature = -4.5,
                IsMoving = true
            };
            var target = new FocuserInfo();

            target.CopyFrom(source);

            target.Connected.Should().BeTrue();
            target.Name.Should().Be("Focuser");
            target.Position.Should().Be(1234);
            target.StepSize.Should().BeApproximately(2.5, 1e-10);
            target.Temperature.Should().BeApproximately(-4.5, 1e-10);
            target.IsMoving.Should().BeTrue();
        }

        /// <summary>
        /// Verifies dummy devices expose a deterministic disconnected no-device contract and reject unsupported commands.
        /// </summary>
        [Test]
        public async Task DummyDevice_ProvidesStableNoDeviceContract() {
            var device = new DummyDevice("None");

            bool connected = await device.Connect(CancellationToken.None);

            connected.Should().BeFalse();
            device.Id.Should().Be("No_Device");
            device.Name.Should().Be("None");
            device.DisplayName.Should().Be("None");
            device.Connected.Should().BeFalse();
            device.SupportedActions.Should().BeEmpty();
            device.Invoking(x => x.Action("noop", string.Empty)).Should().Throw<NotImplementedException>();
            device.Invoking(x => x.SendCommandBlind("noop", raw: false)).Should().Throw<NotImplementedException>();
        }

        /// <summary>
        /// Verifies offline placeholders preserve missing-device identity and fail connection attempts with an actionable message.
        /// </summary>
        [Test]
        public async Task OfflineDevice_PreservesOriginalIdentityAndFailsConnectWithDeviceMessage() {
            var named = new OfflineDevice("driver-id", "Camera");
            var unnamed = new OfflineDevice("driver-id", "");

            Func<Task> connect = () => named.Connect(CancellationToken.None);

            named.Name.Should().Be("Camera (OFFLINE)");
            unnamed.Name.Should().Be("driver-id (OFFLINE)");
            named.Category.Should().Be("OFFLINE");
            named.Connected.Should().BeFalse();
            await connect.Should().ThrowAsync<Exception>().WithMessage("*Camera*driver-id*");
        }
    }
}
