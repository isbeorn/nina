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
using NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class PegasusFlatMasterTest {
        private PegasusFlatMaster _sut;
        private Mock<ISerialPortProvider> _mockSerialPortProvider;
        private Mock<ISerialPort> _mockSerialPort;

        [SetUp]
        public void Init() {
            _mockSerialPort = new Mock<ISerialPort>();
            _mockSerialPortProvider = new Mock<ISerialPortProvider>();
            _sut = new PegasusFlatMaster { SerialPortProvider = _mockSerialPortProvider.Object };
        }

        [Test]
        public void TestPortNamesPortsAvailable() {
            _mockSerialPortProvider
                .Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new ReadOnlyCollection<string>(new List<string> { "COM3" }));

            var result = _sut.PortNames;
            Assert.That(result, Is.EquivalentTo(new ReadOnlyCollection<string>(new List<string> { "AUTO", "COM3" })));
        }

        [Test]
        public void TestPortNamesNoPortsAvailable() {
            _mockSerialPortProvider
                .Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new ReadOnlyCollection<string>(new List<string>()));

            var result = _sut.PortNames;
            Assert.That(result, Is.EquivalentTo(new ReadOnlyCollection<string>(new List<string> { "AUTO" })));
        }

        [Test]
        [TestCase("AUTO", true)]
        [TestCase("COM3", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestInitializeSerialPort(string portName, bool expected) {
            _mockSerialPortProvider
                .Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new ReadOnlyCollection<string>(new List<string> { "COM3" }));
            _mockSerialPortProvider.Setup(m => m.GetSerialPort(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Parity>(),
                It.IsAny<int>(), It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(_mockSerialPort.Object);
            Assert.That(_sut.InitializeSerialPort(portName, this), Is.EqualTo(expected));
        }
    }
}