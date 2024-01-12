#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Core.Locale;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Profile.Interfaces;
using NINA.Core.Utility.SerialCommunication;
using NUnit.Framework;
using NINA.Equipment.SDK.FlatDeviceSDKs.PegasusAstroSDK;

namespace NINA.Test.FlatDevice {

    [TestFixture]
    public class PegasusAstroFlatmasterTest {
        private PegasusAstroFlatMaster _sut;
        private Mock<IPegasusFlatMaster> _mockSdk;
        private Mock<IProfileService> _mockProfileService;

        [SetUp]
        public void Init() {
            _mockSdk = new Mock<IPegasusFlatMaster>();
            _mockProfileService = new Mock<IProfileService>();
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.PortName).Returns("COM3");
            _sut = new PegasusAstroFlatMaster(_mockProfileService.Object) { Sdk = _mockSdk.Object };
        }

        [Test]
        public void TestPortNameWithStoredSettings() {
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.PortName).Returns("COM3");
            var sut = new PegasusAstroFlatMaster(_mockProfileService.Object);
            Assert.That(sut.PortName, Is.EqualTo("COM3"));
        }

        [Test]
        [TestCase(false, false, "OK_FM", "V:1.3", "FlatMaster on port COM3. Firmware version: 1.3")]
        [TestCase(true, true, "OK_FM", "V:1.3", "FlatMaster on port COM3. Firmware version: 1.3")]
        public async Task TestConnect(bool validPort, bool expected, string statusResponse, string firmwareResponse, string expectedDescription) {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(validPort);
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(new StatusResponse { DeviceResponse = statusResponse }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = firmwareResponse }));
            var result = await _sut.Connect(new CancellationToken());
            Assert.That(result, Is.EqualTo(expected));
            if (!result) return;
            Assert.That(_sut.Description, Is.EqualTo(expectedDescription));
        }

        [Test]
        public async Task TestConnectFirmwareResponseInvalid() {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(new StatusResponse { DeviceResponse = "OK_FM" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Throws(new InvalidDeviceResponseException());
            var result = await _sut.Connect(new CancellationToken());
            Assert.That(result, Is.True);
            Assert.That(_sut.Description, Is.EqualTo("FlatMaster on port COM3. Firmware version: " + Loc.Instance["LblNoValidFirmwareVersion"]));
        }

        [Test]
        public void TestDisconnect() {
            _sut.Disconnect();
            _mockSdk.Verify(m => m.Dispose(It.IsAny<object>()), Times.Once);
        }

        [Test]
        [TestCase(true, true, "E:1", "E:1\n")]
        [TestCase(false, false, "E:0", "E:0\n")]
        public async Task TestLightOn(bool lightOn, bool expected, string response, string expectedCommand) {
            string actual = null;
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(new StatusResponse { DeviceResponse = "OK_FM" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = "V:1.3" }));
            _mockSdk.Setup(m => m.SendCommand<OnOffResponse>(It.IsAny<OnOffCommand>()))
                .Callback<ISerialCommand>(arg => actual = arg.CommandString)
                .Returns(Task.FromResult(new OnOffResponse { DeviceResponse = response }));
            await _sut.Connect(new CancellationToken());

            _sut.LightOn = lightOn;
            var result = _sut.LightOn;
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(actual, Is.EqualTo(expectedCommand));
        }

        [Test]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void TestLightOnDisconnected(bool lightOn, bool expected) {
            _sut.LightOn = lightOn;
            var result = _sut.LightOn;
            Assert.That(result, Is.EqualTo(expected));
            _mockSdk.Verify(m => m.SendCommand<OnOffResponse>(It.IsAny<OnOffCommand>()), Times.Never);
        }

        [Test]
        [TestCase(true, "E:1\n")]
        [TestCase(false, "E:0\n")]
        public async Task TestLightOnInvalidResponse(bool lightOn, string expectedCommand) {
            string actual = null;
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(new StatusResponse { DeviceResponse = "OK_FM" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = "V:1.3" }));
            _mockSdk.Setup(m => m.SendCommand<OnOffResponse>(It.IsAny<OnOffCommand>()))
                .Callback<ISerialCommand>(arg => actual = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            await _sut.Connect(new CancellationToken());

            _sut.LightOn = lightOn;
            var result = _sut.LightOn;
            Assert.That(result, Is.False);
            Assert.That(actual, Is.EqualTo(expectedCommand));
        }

        [Test]
        [TestCase(20, 0, "L:220", "L:220\n")]
        [TestCase(220, 300, "L:020", "L:020\n")]
        [TestCase(20, 20, "L:220", "L:220\n")]
        [TestCase(220, 220, "L:020", "L:020\n")]
        [TestCase(50, 50, "L:190", "L:190\n")]
        [TestCase(100, 100, "L:140", "L:140\n")]
        public async Task TestBrightness(int expected, int brightness, string response = null, string expectedCommand = null) {
            string actual = null;
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(new StatusResponse { DeviceResponse = "OK_FM" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = "V:1.3" }));
            _mockSdk.Setup(m => m.SendCommand<SetBrightnessResponse>(It.IsAny<SetBrightnessCommand>()))
                .Callback<ISerialCommand>(arg => actual = arg.CommandString)
                .Returns(Task.FromResult(new SetBrightnessResponse { DeviceResponse = response }));
            await _sut.Connect(new CancellationToken());

            _sut.Brightness = brightness;

            var result = _sut.Brightness;

            Assert.That(result, Is.EqualTo(expected));
            Assert.That(actual, Is.EqualTo(expectedCommand));
        }

        [Test]
        public async Task TestBrightnessInvalidResponse() {
            string actual = null;
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(new StatusResponse { DeviceResponse = "OK_FM" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = "V:1.3" }));
            _mockSdk.Setup(m => m.SendCommand<SetBrightnessResponse>(It.IsAny<SetBrightnessCommand>()))
                .Callback<ISerialCommand>(arg => actual = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            await _sut.Connect(new CancellationToken());

            _sut.Brightness = 20;

            var result = _sut.Brightness;

            Assert.That(result, Is.EqualTo(20));
            Assert.That(actual, Is.EqualTo("L:220\n"));
        }
    }
}