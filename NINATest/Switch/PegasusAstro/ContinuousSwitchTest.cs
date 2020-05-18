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
using NINA.Model.MySwitch.PegasusAstro;
using NINA.Utility.SerialCommunication;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    public class VariablePowerSwitchTest {
        private VariablePowerSwitch _sut;
        private Mock<IPegasusDevice> _mockSdk;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockSdk = new Mock<IPegasusDevice>();
        }

        [SetUp]
        public void Init() {
            _mockSdk.Reset();
            _sut = new VariablePowerSwitch { Sdk = _mockSdk.Object };
        }

        [Test]
        public void TestConstructor() {
            Assert.That(_sut.Minimum, Is.EqualTo(3d));
            Assert.That(_sut.Maximum, Is.EqualTo(12d));
            Assert.That(_sut.StepSize, Is.EqualTo(1d));
        }

        [Test]
        public async Task TestPoll() {
            var response = new PowerStatusResponse { DeviceResponse = "PS:1111:8" };
            _mockSdk.Setup(m => m.SendCommand<PowerStatusResponse>(It.IsAny<PowerStatusCommand>()))
                .Returns(Task.FromResult(response));
            var result = await _sut.Poll();
            Assert.That(result, Is.True);
            Assert.That(_sut.Value, Is.EqualTo(8));
        }

        [Test]
        public async Task TestPollInvalidResponse() {
            _mockSdk.Setup(m => m.SendCommand<PowerStatusResponse>(It.IsAny<PowerStatusCommand>()))
                .Throws(new InvalidDeviceResponseException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task TestPollSerialPortClosed() {
            _mockSdk.Setup(m => m.SendCommand<PowerStatusResponse>(It.IsAny<PowerStatusCommand>()))
                .Throws(new SerialPortClosedException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase(5d, "P8:5\n")]
        [TestCase(20d, "P8:12\n")]
        [TestCase(0d, "P8:3\n")]
        public async Task TestSetValue(double value, string expectedCommand) {
            var command = string.Empty;
            _mockSdk
                .Setup(m => m.SendCommand<SetVariableVoltageResponse>(It.IsAny<SetVariableVoltageCommand>()))
                .Callback<ICommand>(arg => { command = arg.CommandString; });
            _sut.TargetValue = value;
            await _sut.SetValue();
            Assert.That(command, Is.EqualTo(expectedCommand));
        }
    }

    [TestFixture]
    public class DewHeaterTest {
        private DewHeater _sut;
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
            _sut = new DewHeater(0) { Sdk = _mockSdk.Object };
            Assert.That(_sut.Id, Is.EqualTo(0));
            Assert.That(_sut.Minimum, Is.EqualTo(0d));
            Assert.That(_sut.Maximum, Is.EqualTo(100d));
            Assert.That(_sut.StepSize, Is.EqualTo(1d));
        }

        [Test]
        [TestCase(0, "UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", 0d, 0d, false, false)]
        [TestCase(0, "UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:128:0:0:0:0:0:0:300:0:0:0000100:0", 50d, 1d, true, false)]
        [TestCase(2, "UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:128:0:0:0:0:0:0:600:0000000:4", 50d, 1d, false, true)]
        public async Task TestPoll(short heater, string deviceResponse, double dutyCycle, double amps, bool overCurrent, bool autoDewOn) {
            _sut = new DewHeater(heater) { Sdk = _mockSdk.Object };
            var response = new StatusResponse { DeviceResponse = deviceResponse };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(response));
            var result = await _sut.Poll();
            Assert.That(result, Is.True);
            Assert.That(_sut.Value, Is.EqualTo(dutyCycle));
            Assert.That(_sut.CurrentAmps, Is.EqualTo(amps));
            Assert.That(_sut.ExcessCurrent, Is.EqualTo(overCurrent));
            Assert.That(_sut.AutoDewOn, Is.EqualTo(autoDewOn));
        }

        [Test]
        public async Task TestPollInvalidResponse() {
            _sut = new DewHeater(0) { Sdk = _mockSdk.Object };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Throws(new InvalidDeviceResponseException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task TestPollSerialPortClosed() {
            _sut = new DewHeater(0) { Sdk = _mockSdk.Object };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Throws(new SerialPortClosedException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase(0, 50d, false, "P5:128\n", "PD:0\n")]
        [TestCase(0, 0d, true, "P5:000\n", "PD:2\n")]
        [TestCase(1, 50d, true, "P6:128\n", "PD:3\n")]
        [TestCase(2, 50d, true, "P7:128\n", "PD:4\n")]
        public async Task TestSetValue(short heater, double value, bool autoDew, string expectedDewHeaterCommand, string expectedAutoDewCommand) {
            _sut = new DewHeater(heater) { Sdk = _mockSdk.Object };
            var dewCommand = string.Empty;
            var autoDewCommand = string.Empty;
            _mockSdk.Setup(m => m.SendCommand<SetDewHeaterPowerResponse>(It.IsAny<SetDewHeaterPowerCommand>()))
                .Callback<ICommand>(arg => { dewCommand = arg.CommandString; });
            var response = new StatusResponse { DeviceResponse = "UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0" };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(response));
            _mockSdk.Setup(m => m.SendCommand<SetAutoDewResponse>(It.IsAny<SetAutoDewCommand>()))
                .Callback<ICommand>(arg => { autoDewCommand = arg.CommandString; });
            _sut.TargetValue = value;
            _sut.AutoDewOnTarget = autoDew;
            await _sut.SetValue();
            Assert.That(dewCommand, Is.EqualTo(expectedDewHeaterCommand));
            Assert.That(autoDewCommand, Is.EqualTo(expectedAutoDewCommand));
        }
    }
}
