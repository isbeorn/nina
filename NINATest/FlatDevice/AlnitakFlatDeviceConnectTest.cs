#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Locale;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class AlnitakFlatDeviceConnectTest {
        private AlnitakFlatDevice _sut;
        private Mock<IProfileService> _mockProfileService;
        private Mock<IAlnitakDevice> _mockSdk;

        [SetUp]
        public void Init() {
            _mockProfileService = new Mock<IProfileService>();
            _mockProfileService.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings.PortName, "COM3");
            _mockSdk = new Mock<IAlnitakDevice>();
            _sut = new AlnitakFlatDevice(_mockProfileService.Object) { Sdk = _mockSdk.Object };
        }

        [TearDown]
        public void Dispose() {
            _sut.Disconnect();
            Assert.That(_sut.Connected, Is.False);
        }

        [Test]
        public async Task TestConnect() {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns(Task.FromResult(true));
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99000" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = "*V99124" }));
            Assert.That(await _sut.Connect(It.IsAny<CancellationToken>()), Is.True);
        }

        [Test]
        [TestCase("Flat-Man_XL on port COM3. Firmware version: 123", "*S10000", "*V10123", true)]
        [TestCase("Flat-Man_L on port COM3. Firmware version: 123", "*S15000", "*V15123", true)]
        [TestCase("Flat-Man on port COM3. Firmware version: 123", "*S19000", "*V19123", true)]
        [TestCase("Flip-Mask/Remote Dust Cover on port COM3. Firmware version: 123", "*S98000", "*V98123", true)]
        [TestCase("Flip-Flat on port COM3. Firmware version: 123", "*S99000", "*V99123", true)]
        public async Task TestDescription(string description, string stateResponse, string fwResponse, bool connected) {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns(Task.FromResult(true));
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = stateResponse }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = fwResponse }));
            Assert.That(await _sut.Connect(It.IsAny<CancellationToken>()), Is.EqualTo(connected));
            Assert.That(_sut.Description, Is.EqualTo(description));
        }

        [Test]
        public async Task TestDescriptionInvalidFirmwareResponse() {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns(Task.FromResult(true));
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99000" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(await _sut.Connect(It.IsAny<CancellationToken>()), Is.True);
            Assert.That(_sut.Description, Is.EqualTo("Flip-Flat on port COM3. Firmware version: " + Loc.Instance["LblNoValidFirmwareVersion"]));
        }

        [Test]
        public void TestConstructor() {
            _mockProfileService = new Mock<IProfileService>();
            _mockProfileService.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings.PortName, "");
            _sut = new AlnitakFlatDevice(_mockProfileService.Object);
            Assert.That(_sut.Id, Is.EqualTo("817b60ab-6775-41bd-97b5-3857cc676e51"));
        }

        [Test]
        public async Task TestOpenNotConnected() {
            Assert.That(await _sut.Open(It.IsAny<CancellationToken>()), Is.False);
        }

        [Test]
        public async Task TestCloseNotConnected() {
            Assert.That(await _sut.Close(It.IsAny<CancellationToken>()), Is.False);
        }
    }
}