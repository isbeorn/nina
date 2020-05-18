#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASCOM;
using ASCOM.DeviceInterface;
using Moq;
using NINA.Model.MyFocuser;
using NUnit.Framework;

namespace NINATest.Focuser.ASCOM {

    [TestFixture]
    public class AscomFocuserTest {
        private AscomFocuser _sut;
        private Mock<IAscomFocuserProvider> _mockFocuserProvider;
        private Mock<IFocuserV3> _mockFocuser;
        private const string ID = "focuser";
        private const string NAME = "name";

        [SetUp]
        public async Task Init() {
            _mockFocuserProvider = new Mock<IAscomFocuserProvider>();
            _mockFocuser = new Mock<IFocuserV3>();
            _mockFocuser.SetupProperty(m => m.Connected, false);
            _mockFocuserProvider.Setup(m => m.GetFocuser(It.IsAny<string>())).Returns(_mockFocuser.Object);
            _sut = new AscomFocuser(ID, NAME) { FocuserProvider = _mockFocuserProvider.Object };
            var result = await _sut.Connect(new CancellationToken());
            Assert.That(result, Is.True);
        }

        [Test]
        public void TestConstructor() {
            Assert.That(_sut.Id, Is.EqualTo(ID));
            Assert.That(_sut.Name, Is.EqualTo(NAME));
        }

        [Test]
        public async Task TestConnectDriverAccessComException() {
            _mockFocuserProvider.Setup(m => m.GetFocuser(It.IsAny<string>()))
                .Throws(new DriverAccessCOMException("", 0, null));
            var sut = new AscomFocuser(ID, NAME) { FocuserProvider = _mockFocuserProvider.Object };
            var result = await sut.Connect(new CancellationToken());
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task TestConnectComException() {
            _mockFocuserProvider.Setup(m => m.GetFocuser(It.IsAny<string>()))
                .Throws(new System.Runtime.InteropServices.COMException());
            var sut = new AscomFocuser(ID, NAME) { FocuserProvider = _mockFocuserProvider.Object };
            var result = await sut.Connect(new CancellationToken());
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task TestConnectException() {
            _mockFocuserProvider.Setup(m => m.GetFocuser(It.IsAny<string>()))
                .Throws(new Exception());
            var sut = new AscomFocuser(ID, NAME) { FocuserProvider = _mockFocuserProvider.Object };
            var result = await sut.Connect(new CancellationToken());
            Assert.That(result, Is.False);
        }

        [Test]
        public void TestConnectedIntermittentDisconnect() {
            _mockFocuser.SetupProperty(m => m.Connected, false);
            var result = _sut.Connected;
            Assert.That(result, Is.False);
        }

        [Test]
        public void TestConnectedException() {
            _mockFocuser.Setup(m => m.Connected).Throws(new Exception());
            var result = _sut.Connected;
            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void TestIsMoving(bool connected, bool isMoving, bool expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.IsMoving).Returns(isMoving);

            var result = _sut.IsMoving;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(true, 5, 5)]
        [TestCase(false, 5, -1)]
        public void TestMaxIncrement(bool connected, int increment, int expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.MaxIncrement).Returns(increment);

            var result = _sut.MaxIncrement;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(true, 5, 5)]
        [TestCase(false, 5, -1)]
        public void TestMaxStep(bool connected, int increment, int expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.MaxStep).Returns(increment);

            var result = _sut.MaxStep;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(true, 5, 5)]
        [TestCase(false, 5, -1)]
        public void TestPosition(bool connected, int position, int expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.Position).Returns(position);

            var result = _sut.Position;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void TestPositionPropertyNotImplementedException() {
            _mockFocuser.Setup(m => m.Position).Throws(new PropertyNotImplementedException());

            var result = _sut.Position;
            Assert.That(result, Is.EqualTo(-1));

            _mockFocuser.Setup(m => m.Position).Returns(5);
            result = _sut.Position;
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void TestPositionSystemNotImplementedException() {
            _mockFocuser.Setup(m => m.Position).Throws(new System.NotImplementedException());

            var result = _sut.Position;
            Assert.That(result, Is.EqualTo(-1));

            _mockFocuser.Setup(m => m.Position).Returns(5);
            result = _sut.Position;
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void TestPositionDriverException() {
            _mockFocuser.Setup(m => m.Position).Throws(new DriverException());

            var result = _sut.Position;
            Assert.That(result, Is.EqualTo(-1));

            _mockFocuser.Setup(m => m.Position).Returns(5);
            result = _sut.Position;
            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void TestPositionException() {
            _mockFocuser.Setup(m => m.Position).Throws(new Exception());

            var result = _sut.Position;
            Assert.That(result, Is.EqualTo(-1));

            _mockFocuser.Setup(m => m.Position).Returns(5);
            result = _sut.Position;
            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        [TestCase(true, 5d, 5d)]
        [TestCase(false, 5d, double.NaN)]
        public void TestStepSize(bool connected, double stepSize, double expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.StepSize).Returns(stepSize);

            var result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void TestStepSizePropertyNotImplementedException() {
            _mockFocuser.Setup(m => m.StepSize).Throws(new PropertyNotImplementedException());

            var result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(double.NaN));

            _mockFocuser.Setup(m => m.StepSize).Returns(5d);
            result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(double.NaN));
        }

        [Test]
        public void TestStepSizeSystemNotImplementedException() {
            _mockFocuser.Setup(m => m.StepSize).Throws(new System.NotImplementedException());

            var result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(double.NaN));

            _mockFocuser.Setup(m => m.StepSize).Returns(5d);
            result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(double.NaN));
        }

        [Test]
        public void TestStepSizeDriverException() {
            _mockFocuser.Setup(m => m.StepSize).Throws(new DriverException());

            var result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(double.NaN));

            _mockFocuser.Setup(m => m.StepSize).Returns(5d);
            result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(5d));
        }

        [Test]
        public void TestStepSizeException() {
            _mockFocuser.Setup(m => m.StepSize).Throws(new Exception());

            var result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(double.NaN));

            _mockFocuser.Setup(m => m.StepSize).Returns(5d);
            result = _sut.StepSize;
            Assert.That(result, Is.EqualTo(5d));
        }

        [Test]
        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void TestTempCompAvailable(bool connected, bool available, bool expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.TempCompAvailable).Returns(available);

            var result = _sut.TempCompAvailable;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(true, true, true, true)]
        [TestCase(true, true, false, false)]
        [TestCase(true, false, true, false)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, false)]
        public void TestGetTempComp(bool connected, bool available, bool tempComp, bool expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.TempCompAvailable).Returns(available);
            _mockFocuser.Setup(m => m.TempComp).Returns(tempComp);

            var result = _sut.TempComp;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void TestSetTempComp(bool connected, bool available, bool expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.TempCompAvailable).Returns(available);

            _sut.TempComp = expected;
            if (expected) {
                _mockFocuser.VerifySet(m => m.TempComp = It.IsAny<bool>(), Times.Once);
            } else {
                _mockFocuser.VerifySet(m => m.TempComp = It.IsAny<bool>(), Times.Never);
            }
        }

        [Test]
        [TestCase(true, 5d, 5d)]
        [TestCase(false, 5d, double.NaN)]
        public void TestTemperature(bool connected, double temperature, double expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.Temperature).Returns(temperature);

            var result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void TestTemperaturePropertyNotImplementedException() {
            _mockFocuser.Setup(m => m.Temperature).Throws(new PropertyNotImplementedException());

            var result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(double.NaN));

            _mockFocuser.Setup(m => m.Temperature).Returns(5d);
            result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(double.NaN));
        }

        [Test]
        public void TestTemperatureSystemNotImplementedException() {
            _mockFocuser.Setup(m => m.Temperature).Throws(new System.NotImplementedException());

            var result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(double.NaN));

            _mockFocuser.Setup(m => m.Temperature).Returns(5d);
            result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(double.NaN));
        }

        [Test]
        public void TestTemperatureDriverException() {
            _mockFocuser.Setup(m => m.Temperature).Throws(new DriverException());

            var result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(double.NaN));

            _mockFocuser.Setup(m => m.Temperature).Returns(5d);
            result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(5d));
        }

        [Test]
        public void TestTemperatureException() {
            _mockFocuser.Setup(m => m.Temperature).Throws(new Exception());

            var result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(double.NaN));

            _mockFocuser.Setup(m => m.Temperature).Returns(5d);
            result = _sut.Temperature;
            Assert.That(result, Is.EqualTo(5d));
        }

        [Test]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public async Task TestMove(bool connected, bool tempComp, bool expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            _mockFocuser.Setup(m => m.TempComp).Returns(tempComp);
            _mockFocuser.SetupSequence(m => m.Position).Returns(0).Returns(10);
            _mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            await _sut.Move(10, new CancellationToken());
            if (expected) {
                _mockFocuser.Verify(m => m.Move(10), Times.Once);
            } else {
                _mockFocuser.Verify(m => m.Move(It.IsAny<int>()), Times.Never);
            }
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void TestHalt(bool connected, bool expected) {
            if (!connected) _sut.Disconnect();

            _sut.Halt();
            if (expected) {
                _mockFocuser.Verify(m => m.Halt(), Times.Once);
            } else {
                _mockFocuser.Verify(m => m.Halt(), Times.Never);
            }
        }

        [Test]
        public void TestHaltMethodNotImplementedException() {
            _mockFocuser.Setup(m => m.Halt()).Throws(new MethodNotImplementedException());

            _sut.Halt();
            _sut.Halt();
            _mockFocuser.Verify(m => m.Halt(), Times.Once);
        }

        [Test]
        public void TestHaltException() {
            _mockFocuser.Setup(m => m.Halt()).Throws(new Exception());

            _sut.Halt();
            _sut.Halt();
            _mockFocuser.Verify(m => m.Halt(), Times.Exactly(2));
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void TestDescription(bool connected, bool expected) {
            if (!connected) _sut.Disconnect();
            _mockFocuser.Setup(m => m.Description).Returns("description");

            var result = _sut.Description;
            Assert.That(result, expected ? Is.EqualTo("description") : Is.EqualTo(string.Empty));
        }

        [Test]
        [TestCase(true, "info", false)]
        [TestCase(true, null, true)]
        [TestCase(false, "info", true)]
        [TestCase(false, null, true)]
        public void TestDriverInfo(bool connected, string driverInfo, bool expectedEmpty) {
            if (!connected) _sut.Disconnect();

            _mockFocuser.Setup(m => m.DriverInfo).Returns(driverInfo);

            var result = _sut.DriverInfo;
            Assert.That(result, expectedEmpty ? Is.EqualTo(string.Empty) : Is.EqualTo(driverInfo));
        }

        [Test]
        [TestCase(true, "version", false)]
        [TestCase(true, null, true)]
        [TestCase(false, "version", true)]
        [TestCase(false, null, true)]
        public void TestDriverVersion(bool connected, string driverVersion, bool expectedEmpty) {
            if (!connected) _sut.Disconnect();

            _mockFocuser.Setup(m => m.DriverVersion).Returns(driverVersion);

            var result = _sut.DriverVersion;
            Assert.That(result, expectedEmpty ? Is.EqualTo(string.Empty) : Is.EqualTo(driverVersion));
        }

        [Test]
        public void TestDispose() {
            _sut.Disconnect();
            _sut.Dispose();
            _mockFocuser.Verify(m => m.Dispose(), Times.Once);
        }

        [Test]
        public void TestSetupDialog() {
            _sut.SetupDialog();
            _mockFocuser.Verify(m => m.SetupDialog(), Times.Once);
        }

        [Test]
        public void TestSetupDialogException() {
            var sut = new AscomFocuser(ID, NAME) { FocuserProvider = _mockFocuserProvider.Object };
            _mockFocuserProvider.Setup(m => m.GetFocuser(It.IsAny<string>())).Throws(new Exception());
            sut.SetupDialog();
            _mockFocuser.Verify(m => m.SetupDialog(), Times.Never);
        }
    }
}
