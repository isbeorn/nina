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
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class AlnitakFlatDeviceTest {
        private AlnitakFlatDevice _sut;
        private Mock<IProfileService> _mockProfileService;
        private Mock<IAlnitakDevice> _mockSdk;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockProfileService = new Mock<IProfileService>();
            _mockSdk = new Mock<IAlnitakDevice>();
        }

        [SetUp]
        public void Init() {
            _mockProfileService.Reset();
            _mockProfileService.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings.PortName, "COM3");

            _mockSdk.Reset();
            _mockSdk.Setup(m => m.InitializeSerialPort(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Returns(Task.FromResult(true));
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99000" }));
            _mockSdk.Setup(m => m.SendCommand<FirmwareVersionResponse>(It.IsAny<FirmwareVersionCommand>()))
                .Returns(Task.FromResult(new FirmwareVersionResponse { DeviceResponse = "*V99124" }));
            _sut = new AlnitakFlatDevice(_mockProfileService.Object) { Sdk = _mockSdk.Object };
        }

        [Test]
        [TestCase(CoverState.NeitherOpenNorClosed, "*S99000")]
        [TestCase(CoverState.Closed, "*S99001")]
        [TestCase(CoverState.Open, "*S99002")]
        [TestCase(CoverState.Unknown, "*S99003")]
        public async Task TestCoverState(CoverState coverState, string deviceResponse) {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = deviceResponse }));
            Assert.That(_sut.CoverState, Is.EqualTo(coverState));
        }

        [Test]
        public async Task TestCoverStateInvalidResponse() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(_sut.CoverState, Is.EqualTo(CoverState.Unknown));
        }

        [Test]
        public void TestCoverStateDisconnected() {
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99000" }));
            Assert.That(_sut.CoverState, Is.EqualTo(CoverState.Unknown));
        }

        [Test]
        public async Task TestMinBrightness() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);
            Assert.That(_sut.MinBrightness, Is.EqualTo(0));
        }

        [Test]
        public async Task TestMaxBrightness() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);
            Assert.That(_sut.MaxBrightness, Is.EqualTo(255));
        }

        [Test]
        [TestCase(0.0, "*J99000")]
        [TestCase(1.0, "*J99255")]
        [TestCase(0.502, "*J99128")]
        public async Task TestGetBrightness(double brightness, string deviceResponse) {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            _mockSdk.Setup(m => m.SendCommand<GetBrightnessResponse>(It.IsAny<GetBrightnessCommand>()))
                .Returns(Task.FromResult(new GetBrightnessResponse { DeviceResponse = deviceResponse }));
            Assert.That(_sut.Brightness, Is.EqualTo(brightness));
        }

        [Test]
        public async Task TestGetBrightnessInvalidResponse() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            _mockSdk.Setup(m => m.SendCommand<GetBrightnessResponse>(It.IsAny<GetBrightnessCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(_sut.Brightness, Is.EqualTo(0d));
        }

        [Test]
        [TestCase(0, "*J99000")]
        [TestCase(0, "*J99255")]
        [TestCase(0, "*J99099")]
        public void TestGetBrightnessDisconnected(double brightness, string deviceResponse) {
            _mockSdk.Setup(m => m.SendCommand<GetBrightnessResponse>(It.IsAny<GetBrightnessCommand>()))
                .Returns(Task.FromResult(new GetBrightnessResponse { DeviceResponse = deviceResponse }));
            Assert.That(_sut.Brightness, Is.EqualTo(brightness));
        }

        [Test]
        [TestCase(0.0, ">B000\r", "*B99000")]
        [TestCase(1.0, ">B255\r", "*B99255")]
        [TestCase(0.5, ">B128\r", "*B99128")]
        [TestCase(-1.0, ">B000\r", "*B99000")]
        [TestCase(2.0, ">B255\r", "*B99255")]
        public async Task TestSetBrightness(double brightness, string command, string response) {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);
            string actual = null;

            _mockSdk.Setup(m => m.SendCommand<SetBrightnessResponse>(It.IsAny<SetBrightnessCommand>()))
                .Callback<ICommand>(arg => actual = arg.CommandString)
                .Returns(Task.FromResult(new SetBrightnessResponse { DeviceResponse = response }));
            _sut.Brightness = brightness;
            Assert.That(actual, Is.EqualTo(command));
        }

        [Test]
        [TestCase(0, null)]
        [TestCase(255, null)]
        [TestCase(99, null)]
        [TestCase(50, null)]
        [TestCase(-1, null)]
        [TestCase(256, null)]
        public void TestSetBrightnessDisconnected(int brightness, string command) {
            string actual = null;
            _mockSdk.Setup(m => m.SendCommand<SetBrightnessResponse>(It.IsAny<SetBrightnessCommand>()))
                .Callback<ICommand>(arg => actual = arg.CommandString);

            _sut.Brightness = brightness;
            Assert.That(actual, Is.EqualTo(command));
        }

        [Test]
        public async Task TestOpen() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);
            _mockSdk.Setup(m => m.SendCommand<OpenResponse>(It.IsAny<OpenCommand>()))
                .Returns(Task.FromResult(new OpenResponse { DeviceResponse = "*O99OOO" }));
            _mockSdk.SetupSequence(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99100" })) //motor running
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99002" })) //motor stopped
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99002" })); //cover is open
            Assert.That(await _sut.Open(It.IsAny<CancellationToken>(), 5), Is.True);
        }

        [Test]
        public async Task TestOpenInvalidResponse() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            _mockSdk.Setup(m => m.SendCommand<OpenResponse>(It.IsAny<OpenCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(await _sut.Open(It.IsAny<CancellationToken>()), Is.False);
        }

        [Test]
        public async Task TestClose() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            _mockSdk.Setup(m => m.SendCommand<CloseResponse>(It.IsAny<CloseCommand>()))
                .Returns(Task.FromResult(new CloseResponse { DeviceResponse = "*C99OOO" }));
            _mockSdk.SetupSequence(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99100" })) //motor running
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99001" })) //motor stopped
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = "*S99001" })); //cover is closed
            Assert.That(await _sut.Close(It.IsAny<CancellationToken>(), 5), Is.True);
        }

        [Test]
        public async Task TestCloseInvalidResponse() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            _mockSdk.Setup(m => m.SendCommand<CloseResponse>(It.IsAny<CloseCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(await _sut.Close(It.IsAny<CancellationToken>()), Is.False);
        }

        [Test]
        public async Task TestSetLightOn() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            string actual = null;
            _mockSdk.Setup(m => m.SendCommand<LightOnResponse>(It.IsAny<LightOnCommand>()))
                .Callback<ICommand>(arg => actual = arg.CommandString)
                .Returns(Task.FromResult(new LightOnResponse { DeviceResponse = "*L99OOO" }));
            _sut.LightOn = true;
            Assert.That(actual, Is.EqualTo(">LOOO\r"));
        }

        [Test]
        public async Task TestSetLightOnInvalidResponse() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            string actual = null;
            _mockSdk.Setup(m => m.SendCommand<LightOnResponse>(It.IsAny<LightOnCommand>()))
                .Callback<ICommand>(arg => actual = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            _sut.LightOn = true;
            Assert.That(actual, Is.EqualTo(">LOOO\r"));
        }

        [Test]
        public async Task TestSetLightOff() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            string actual = null;
            _mockSdk.Setup(m => m.SendCommand<LightOffResponse>(It.IsAny<LightOffCommand>()))
                .Callback<ICommand>(arg => actual = arg.CommandString)
                .Returns(Task.FromResult(new LightOffResponse { DeviceResponse = "*D99OOO" }));
            _sut.LightOn = false;
            Assert.That(actual, Is.EqualTo(">DOOO\r"));
        }

        [Test]
        public async Task TestSetLightOffInvalidResponse() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            string actual = null;
            _mockSdk.Setup(m => m.SendCommand<LightOffResponse>(It.IsAny<LightOffCommand>()))
                .Callback<ICommand>(arg => actual = arg.CommandString)
                .Throws(new InvalidDeviceResponseException());
            _sut.LightOn = false;
            Assert.That(actual, Is.EqualTo(">DOOO\r"));
        }

        [Test]
        [TestCase("*S99010", true)]
        [TestCase("*S99000", false)]
        public async Task TestGetLightOn(string response, bool on) {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = response }));
            Assert.That(_sut.LightOn, Is.EqualTo(on));
        }

        [Test]
        public async Task TestGetLightOnInvalidResponse() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Throws(new InvalidDeviceResponseException());
            Assert.That(_sut.LightOn, Is.False);
        }

        [Test]
        [TestCase("*S99010", false)]
        public void TestGetLightOnDisconnected(string response, bool on) {
            _mockSdk.Setup(m => m.SendCommand<StateResponse>(It.IsAny<StateCommand>()))
                .Returns(Task.FromResult(new StateResponse { DeviceResponse = response }));
            Assert.That(_sut.LightOn, Is.EqualTo(on));
        }

        [Test]
        public async Task TestConnectedDisconnected() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);

            _sut.Disconnect();
            Assert.That(_sut.Connected, Is.False);
        }
    }
}