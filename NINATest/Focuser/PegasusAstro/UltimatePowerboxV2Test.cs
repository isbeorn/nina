#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Locale;
using NINA.Model.MyFocuser;
using NINA.Profile;
using NINA.Utility.SerialCommunication;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;

namespace NINATest.Focuser.PegasusAstro {

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
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = "1.3" }));
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(new StatusResponse()));
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
        public async Task TestConnectAsync(bool expected, string commandString = "1.3", bool portAvailable = true, string description = null) {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(portAvailable);
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = commandString }));
            var sut = new UltimatePowerboxV2(_mockProfileService.Object) { Sdk = _mockSdk.Object };
            var result = await sut.Connect(new CancellationToken());
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(sut.Connected, Is.EqualTo(expected));
            if (!expected) return;
            Assert.That(double.TryParse(commandString, NumberStyles.Float, CultureInfo.InvariantCulture, out var version), Is.True);
            Assert.That(sut.Description, Is.EqualTo($"{description}{version}"));
        }

        [Test]
        public async Task TestConnectAsyncFirmwareException() {
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>())).Returns(true);
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Throws(new InvalidDeviceResponseException());
            var sut = new UltimatePowerboxV2(_mockProfileService.Object) { Sdk = _mockSdk.Object };
            var result = await sut.Connect(new CancellationToken());
            Assert.That(result, Is.True);
            Assert.That(sut.Connected, Is.True);
            Assert.That(sut.Description, Is.EqualTo("Ultimate Powerbox V2 on port COM3. Firmware version: " + Loc.Instance["LblNoValidFirmwareVersion"]));
        }

        [Test]
        public void TestDisconnect() {
            _sut.Disconnect();
            _mockSdk.Verify(m => m.Dispose(It.IsAny<object>()), Times.Once);
            Assert.That(_sut.Connected, Is.False);
        }

        [Test]
        [TestCase("1", true)]
        [TestCase("0", false)]
        public void TestIsMoving(string deviceResponse, bool expected) {
            _mockSdk.Setup(m => m.SendCommand<StepperMotorIsMovingResponse>(It.IsAny<StepperMotorIsMovingCommand>()))
                .Returns(Task.FromResult(new StepperMotorIsMovingResponse { DeviceResponse = deviceResponse }));
            Assert.That(_sut.IsMoving, Is.EqualTo(expected));
        }

        [Test]
        public void TestIsMovingInvalidResponse() {
            _mockSdk.Setup(m => m.SendCommand<StepperMotorIsMovingResponse>(It.IsAny<StepperMotorIsMovingCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(_sut.IsMoving, Is.False);
        }

        [Test]
        [TestCase("0", 0)]
        [TestCase("-430000", -430000)]
        [TestCase("4500000", 4500000)]
        public void TestPosition(string deviceResponse, int expected) {
            _mockSdk.Setup(m => m.SendCommand<StepperMotorGetCurrentPositionResponse>(It.IsAny<StepperMotorGetCurrentPositionCommand>()))
                .Returns(Task.FromResult(new StepperMotorGetCurrentPositionResponse { DeviceResponse = deviceResponse }));
            Assert.That(_sut.Position, Is.EqualTo(expected));
        }

        [Test]
        public void TestPositionInvalidResponse() {
            _mockSdk.Setup(m => m.SendCommand<StepperMotorGetCurrentPositionResponse>(It.IsAny<StepperMotorGetCurrentPositionCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(_sut.Position, Is.EqualTo(0));
        }

        [Test]
        [TestCase("0", 0)]
        [TestCase("-43.2", -43.2)]
        [TestCase("45", 45d)]
        public void TestTemperature(string deviceResponse, double expected) {
            _mockSdk.Setup(m => m.SendCommand<StepperMotorTemperatureResponse>(It.IsAny<StepperMotorTemperatureCommand>()))
                .Returns(Task.FromResult(new StepperMotorTemperatureResponse { DeviceResponse = deviceResponse }));
            Assert.That(_sut.Temperature, Is.EqualTo(expected));
        }

        [Test]
        public void TestTemperatureInvalidResponse() {
            _mockSdk.Setup(m => m.SendCommand<StepperMotorTemperatureResponse>(It.IsAny<StepperMotorTemperatureCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(_sut.Temperature, Is.EqualTo(0d));
        }

        [Test]
        [TestCase(0, "SM:0\n")]
        [TestCase(-430000, "SM:-430000\n")]
        [TestCase(45000000, "SM:45000000\n")]
        public async Task TestMove(int position, string expected) {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorMoveToPositionResponse>(It.IsAny<StepperMotorMoveToPositionCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Returns(Task.FromResult(new StepperMotorMoveToPositionResponse { DeviceResponse = $"SM:{position}" }));
            await _sut.Move(position, new CancellationToken());
            Assert.That(command, Is.EqualTo(expected));
        }

        [Test]
        public async Task TestMoveInvalidResponse() {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorMoveToPositionResponse>(It.IsAny<StepperMotorMoveToPositionCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            await _sut.Move(0, new CancellationToken());
            Assert.That(command, Is.EqualTo("SM:0\n"));
        }

        [Test]
        public void TestHalt() {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorHaltResponse>(It.IsAny<StepperMotorHaltCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Returns(Task.FromResult(new StepperMotorHaltResponse { DeviceResponse = "SH" }));
            _sut.Halt();
            Assert.That(command, Is.EqualTo("SH\n"));
        }

        [Test]
        public void TestHaltInvalidResponse() {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorHaltResponse>(It.IsAny<StepperMotorHaltCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            _sut.Halt();
            Assert.That(command, Is.EqualTo("SH\n"));
        }

        [Test]
        public void TestDispose() {
            _sut.Dispose();
            Assert.That(_sut.Connected, Is.False);
            _mockSdk.Verify(m => m.Dispose(It.IsAny<object>()), Times.Once);
        }

        [Test]
        [TestCase("clockWise", "SR:0\n", "SR:0")]
        [TestCase("antiClockWise", "SR:1\n", "SR:1")]
        public void TestSetMotorDirection(string direction, string expected, string deviceResponse) {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorDirectionResponse>(It.IsAny<StepperMotorDirectionCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Returns(Task.FromResult(new StepperMotorDirectionResponse { DeviceResponse = deviceResponse }));
            _sut.SetMotorDirection(direction);
            Assert.That(command, Is.EqualTo(expected));
        }

        [Test]
        public void TestSetMotorDirectionInvalidResponse() {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorDirectionResponse>(It.IsAny<StepperMotorDirectionCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            _sut.SetMotorDirection("antiClockWise");
            Assert.That(command, Is.EqualTo("SR:1\n"));
        }

        [Test]
        [TestCase("0", "SC:0\n", "SC:0")]
        [TestCase("-43000", "SC:-43000\n", "SC:-43000")]
        [TestCase("450000", "SC:450000\n", "SC:450000")]
        public void TestSetCurrentPosition(string position, string expected, string deviceResponse) {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorSetCurrentPositionResponse>(It.IsAny<StepperMotorSetCurrentPositionCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Returns(Task.FromResult(new StepperMotorSetCurrentPositionResponse { DeviceResponse = deviceResponse }));
            _sut.SetCurrentPosition(position);
            Assert.That(command, Is.EqualTo(expected));
        }

        [Test]
        public void TestSetCurrentPositionInvalidResponse() {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorSetCurrentPositionResponse>(It.IsAny<StepperMotorSetCurrentPositionCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            _sut.SetCurrentPosition("450000");
            Assert.That(command, Is.EqualTo("SC:450000\n"));
        }

        [Test]
        [TestCase("0", "SS:0\n")]
        [TestCase("-43000", "SS:-43000\n")]
        [TestCase("450000", "SS:450000\n")]
        public void TestSetMaximumSpeed(string speed, string expected) {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorSetMaximumSpeedResponse>(It.IsAny<StepperMotorSetMaximumSpeedCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Returns(Task.FromResult(new StepperMotorSetMaximumSpeedResponse()));
            _sut.SetMaximumSpeed(speed);
            Assert.That(command, Is.EqualTo(expected));
        }

        [Test]
        public void TestSetMaximumSpeedInvalidResponse() {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorSetMaximumSpeedResponse>(It.IsAny<StepperMotorSetMaximumSpeedCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Returns(Task.FromResult((StepperMotorSetMaximumSpeedResponse)null));
            _sut.SetMaximumSpeed("0");
            Assert.That(command, Is.EqualTo("SS:0\n"));
        }

        [Test]
        [TestCase("0", "SB:0\n", "SB:0")]
        [TestCase("-43000", "SB:-43000\n", "SB:-43000")]
        [TestCase("450000", "SB:450000\n", "SB:450000")]
        public void TestSetBacklashSteps(string steps, string expected, string deviceResponse) {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorSetBacklashStepsResponse>(It.IsAny<StepperMotorSetBacklashStepsCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Returns(Task.FromResult(new StepperMotorSetBacklashStepsResponse { DeviceResponse = deviceResponse }));
            _sut.SetBacklashSteps(steps);
            Assert.That(command, Is.EqualTo(expected));
        }

        [Test]
        public void TestSetBacklashStepsInvalidResponse() {
            string command = null;
            _mockSdk.Setup(m => m.SendCommand<StepperMotorSetBacklashStepsResponse>(It.IsAny<StepperMotorSetBacklashStepsCommand>()))
                .Callback<ICommand>(arg => command = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            _sut.SetBacklashSteps("450000");
            Assert.That(command, Is.EqualTo("SB:450000\n"));
        }
    }
}