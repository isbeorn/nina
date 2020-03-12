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