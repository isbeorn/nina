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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Model.MyWeatherData;
using NINA.Profile;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;

namespace NINATest.WeatherData.PegasusAstro {

    [TestFixture]
    public class UltimatePowerboxV2Test {
        private Mock<IProfileService> _mockProfileService;
        private Mock<IPegasusDevice> _mockSdk;
        private UltimatePowerboxV2 _sut;

        [SetUp]
        public async Task Init() {
            _mockProfileService = new Mock<IProfileService>();
            _mockProfileService.SetupProperty(m => m.ActiveProfile.SwitchSettings.Upbv2PortName, "COM3");
            _mockSdk = new Mock<IPegasusDevice>();
            _sut = new UltimatePowerboxV2(_mockProfileService.Object) { Sdk = _mockSdk.Object };
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(new FirmwareVersionResponse { DeviceResponse = "1.3" });
            await _sut.Connect(new CancellationToken());
        }

        [Test]
        public void TestConstructor() {
            _mockProfileService.SetupProperty(m => m.ActiveProfile.SwitchSettings.Upbv2PortName, null);
            var sut = new UltimatePowerboxV2(_mockProfileService.Object) { Sdk = _mockSdk.Object };
            Assert.That(sut.PortName, Is.EqualTo("AUTO"));
        }

        [Test]
        [TestCase(true, "1.3", true, "Ultimate Powerbox V2 on port COM3. Firmware version: ")]
        [TestCase(false, "1.3", false)]
        [TestCase(false, "XXX")]
        public async Task TestConnectAsync(bool expected, string commandString = "1.3", bool portAvailable = true, string description = null) {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(portAvailable);
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(new FirmwareVersionResponse { DeviceResponse = commandString });
            var sut = new UltimatePowerboxV2(_mockProfileService.Object) { Sdk = _mockSdk.Object };
            var result = await sut.Connect(new CancellationToken());
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(sut.Connected, Is.EqualTo(expected));
            if (!expected) return;
            Assert.That(double.TryParse(commandString, NumberStyles.Float, CultureInfo.InvariantCulture, out var version), Is.True);
            Assert.That(sut.Description, Is.EqualTo($"{description}{version}"));
        }

        [Test]
        public async Task TestConnectExceptionAsync() {
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Throws(new Exception());
            var sut = new UltimatePowerboxV2(_mockProfileService.Object) { Sdk = _mockSdk.Object };
            var result = await sut.Connect(new CancellationToken());
            Assert.That(result, Is.False);
            Assert.That(sut.Connected, Is.False);
        }

        [Test]
        public void TestDisconnect() {
            _sut.Disconnect();
            _mockSdk.Verify(m => m.Dispose(It.IsAny<object>()), Times.Once);
            Assert.That(_sut.Connected, Is.False);
        }

        [Test]
        [TestCase("UPB:12.2:0.2:2:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", 14.7)]
        [TestCase("XXX", double.NaN)]
        public void TestDewPoint(string deviceResponse, double expected) {
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(new StatusResponse { DeviceResponse = deviceResponse });
            var result = _sut.DewPoint;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase("UPB:12.2:0.2:2:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", 23.2)]
        [TestCase("XXX", double.NaN)]
        public void TestDewTemperature(string deviceResponse, double expected) {
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(new StatusResponse { DeviceResponse = deviceResponse });
            var result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase("UPB:12.2:0.2:2:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", 59d)]
        [TestCase("XXX", double.NaN)]
        public void TestHumidity(string deviceResponse, double expected) {
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(new StatusResponse { DeviceResponse = deviceResponse });
            var result = _sut.Humidity;
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}