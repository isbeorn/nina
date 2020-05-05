#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Locale;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;

namespace NINATest.FlatDevice {

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
                .Callback<ICommand>(arg => actual = arg.CommandString)
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
                .Callback<ICommand>(arg => actual = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            await _sut.Connect(new CancellationToken());

            _sut.LightOn = lightOn;
            var result = _sut.LightOn;
            Assert.That(result, Is.False);
            Assert.That(actual, Is.EqualTo(expectedCommand));
        }

        [Test]
        [TestCase(1d, 1d, "L:020", "L:020\n")]
        [TestCase(0d, 0d, "L:220", "L:220\n")]
        [TestCase(1d, 2d, "L:020", "L:020\n")]
        [TestCase(0d, -2d, "L:220", "L:220\n")]
        public async Task TestBrightness(double expected, double brightness, string response = null, string expectedCommand = null) {
            string actual = null;
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(new StatusResponse { DeviceResponse = "OK_FM" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = "V:1.3" }));
            _mockSdk.Setup(m => m.SendCommand<SetBrightnessResponse>(It.IsAny<SetBrightnessCommand>()))
                .Callback<ICommand>(arg => actual = arg.CommandString)
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
                .Callback<ICommand>(arg => actual = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            await _sut.Connect(new CancellationToken());

            _sut.Brightness = 1d;

            var result = _sut.Brightness;

            Assert.That(result, Is.EqualTo(1d));
            Assert.That(actual, Is.EqualTo("L:020\n"));
        }
    }
}