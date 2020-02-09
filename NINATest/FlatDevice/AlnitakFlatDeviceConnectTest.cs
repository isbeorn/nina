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

using Moq;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
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
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<PingResponse>(It.IsAny<PingCommand>()))
                .Returns(new PingResponse { DeviceResponse = "*P99000" });
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(new StateResponse { DeviceResponse = "*S99000" });
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(new FirmwareVersionResponse { DeviceResponse = "*V99124" });
            Assert.That(await _sut.Connect(It.IsAny<CancellationToken>()), Is.True);
        }

        [Test]
        [TestCase("Flat-Man_XL on port COM3. Firmware version: 123", "*S10000", "*V10123", true)]
        [TestCase("Flat-Man_L on port COM3. Firmware version: 123", "*S15000", "*V15123", true)]
        [TestCase("Flat-Man on port COM3. Firmware version: 123", "*S19000", "*V19123", true)]
        [TestCase("Flip-Mask/Remote Dust Cover on port COM3. Firmware version: 123", "*S98000", "*V98123", true)]
        [TestCase("Flip-Flat on port COM3. Firmware version: 123", "*S99000", "*V99123", true)]
        [TestCase(null, "garbage", "garbage", false)]
        [TestCase("Flip-Flat on port COM3. Firmware version: No valid firmware version.", "*S99000", "*V99XXX", true)]
        [TestCase(null, null, null, false)]
        public async Task TestDescription(string description, string stateResponse, string fwResponse, bool connected) {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<PingResponse>(It.IsAny<PingCommand>()))
                .Returns(new PingResponse { DeviceResponse = "*P99000" });
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(new StateResponse { DeviceResponse = stateResponse });
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(new FirmwareVersionResponse { DeviceResponse = fwResponse });
            Assert.That(await _sut.Connect(It.IsAny<CancellationToken>()), Is.EqualTo(connected));
            Assert.That(_sut.Description, Is.EqualTo(description));
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