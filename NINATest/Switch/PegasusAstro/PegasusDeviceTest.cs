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
using Moq;
using NINA.Utility.SerialCommunication;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Threading.Tasks;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    internal class PegasusDeviceTest {
        private Mock<ISerialPort> _mockSerialPort;
        private Mock<ISerialPortProvider> _mockSerialPortProvider;
        private IPegasusDevice _sut;

        [SetUp]
        public void Init() {
            _mockSerialPort = new Mock<ISerialPort>();

            _mockSerialPortProvider = new Mock<ISerialPortProvider>();
            _mockSerialPortProvider.Setup(m => m.GetSerialPort(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Parity>(),
                    It.IsAny<int>(), It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(_mockSerialPort.Object);
            _mockSerialPortProvider.Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new ReadOnlyCollection<string>(new List<string> { "COM3" }));
            _sut = PegasusDevice.Instance;
            _sut.SerialPortProvider = _mockSerialPortProvider.Object;
        }

        [TearDown]
        public void TearDown() {
            _sut.Dispose(this);
        }

        [Test]
        [TestCase("AUTO", true, true)]
        [TestCase("AUTO", false, false)]
        [TestCase("COM3", true, true)]
        [TestCase("", true, false)]
        [TestCase(null, true, false)]
        public void TestInitializeSerialPort(string portName, bool portsAvailable, bool expected) {
            if (portsAvailable) {
                _mockSerialPortProvider
                    .Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(new ReadOnlyCollection<string>(new List<string> { "COM3" }));
                _mockSerialPort.Setup(m => m.PortName).Returns("COM3");
            } else {
                _mockSerialPortProvider
                    .Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(new ReadOnlyCollection<string>(new List<string>()));
                _mockSerialPortProvider.Setup(m => m.GetSerialPort(It.IsAny<string>(), It.IsAny<int>(),
                        It.IsAny<Parity>(),
                        It.IsAny<int>(), It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                        It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Returns((ISerialPort)null);
            }

            Assert.That(_sut.InitializeSerialPort(portName, this), Is.EqualTo(expected));
        }

        [Test]
        public void TestGetPortNames() {
            _mockSerialPortProvider
                .Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new ReadOnlyCollection<string>(new List<string> { "COM3" }));
            var result = _sut.PortNames;
            Assert.That(result, Is.EquivalentTo(new ReadOnlyCollection<string>(new List<string> { "AUTO", "COM3" })));
        }
    }
}