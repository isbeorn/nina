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
using NINA.Model.MySwitch.PegasusAstro;
using NINA.Utility.SerialCommunication;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    public class PowerSwitchTest {
        private PegasusAstroPowerSwitch _sut;

        private Mock<IPegasusDevice> _mockSdk;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockSdk = new Mock<IPegasusDevice>();
        }

        [SetUp]
        public void Init() {
            _mockSdk.Reset();
        }

        [Test]
        public void TestConstructor() {
            _sut = new PegasusAstroPowerSwitch(1) { Sdk = _mockSdk.Object };

            Assert.That(_sut.Id, Is.EqualTo(1));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", 1d, 0d, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1011:111111:0:0:0:0:300:0:0:0:0:0:0100000:0", 0d, 1d, true)]
        public async Task TestPoll(string deviceResponse, double expected, double amps, bool overCurrent) {
            _sut = new PegasusAstroPowerSwitch(1) { Sdk = _mockSdk.Object };

            var response = new StatusResponse { DeviceResponse = deviceResponse };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(response));
            var result = await _sut.Poll();
            Assert.That(result, Is.True);
            Assert.That(_sut.Value, Is.EqualTo(expected));
            Assert.That(_sut.CurrentAmps, Is.EqualTo(amps));
            Assert.That(_sut.ExcessCurrent, Is.EqualTo(overCurrent));
        }

        [Test]
        public async Task TestPollInvalidResponse() {
            _sut = new PegasusAstroPowerSwitch(1) { Sdk = _mockSdk.Object };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Throws(new InvalidDeviceResponseException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task TestPollSerialPortClosed() {
            _sut = new PegasusAstroPowerSwitch(1) { Sdk = _mockSdk.Object };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Throws(new SerialPortClosedException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase(0d, "P2:0\n")]
        [TestCase(1d, "P2:1\n")]
        public async Task TestSetValue(double value, string expectedCommand) {
            _sut = new PegasusAstroPowerSwitch(1) { Sdk = _mockSdk.Object };
            var command = string.Empty;
            _mockSdk.Setup(m => m.SendCommand<SetPowerResponse>(It.IsAny<SetPowerCommand>()))
                .Callback<ICommand>(arg => { command = arg.CommandString; });
            _sut.TargetValue = value;
            await _sut.SetValue();
            Assert.That(command, Is.EqualTo(expectedCommand));
        }
    }

    public class UsbSwitchTest {
        private PegasusAstroUsbSwitch _sut;
        private Mock<IPegasusDevice> _mockSdk;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockSdk = new Mock<IPegasusDevice>();
        }

        [SetUp]
        public void Init() {
            _mockSdk.Reset();
        }

        [Test]
        public void TestConstructor() {
            _sut = new PegasusAstroUsbSwitch(1) { Sdk = _mockSdk.Object };
            Assert.That(_sut.Id, Is.EqualTo(1));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", 1d)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:101111:0:0:0:0:300:0:0:0:0:0:0100000:0", 0d)]
        public async Task TestPoll(string deviceResponse, double expected) {
            _sut = new PegasusAstroUsbSwitch(1) { Sdk = _mockSdk.Object };
            var response = new StatusResponse { DeviceResponse = deviceResponse };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(response));
            var result = await _sut.Poll();
            Assert.That(result, Is.True);
            Assert.That(_sut.Value, Is.EqualTo(expected));
        }

        [Test]
        public async Task TestPollInvalidResponse() {
            _sut = new PegasusAstroUsbSwitch(1) { Sdk = _mockSdk.Object };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Throws(new InvalidDeviceResponseException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task TestPollSerialPortClosed() {
            _sut = new PegasusAstroUsbSwitch(1) { Sdk = _mockSdk.Object };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Throws(new SerialPortClosedException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase(0d, "U2:0\n")]
        [TestCase(1d, "U2:1\n")]
        public async Task TestSetValue(double value, string expectedCommand) {
            _sut = new PegasusAstroUsbSwitch(1) { Sdk = _mockSdk.Object };
            var command = string.Empty;
            _mockSdk.Setup(m => m.SendCommand<SetUsbPowerResponse>(It.IsAny<SetUsbPowerCommand>()))
                .Callback<ICommand>(arg => { command = arg.CommandString; });
            _sut.TargetValue = value;
            await _sut.SetValue();
            Assert.That(command, Is.EqualTo(expectedCommand));
        }
    }
}