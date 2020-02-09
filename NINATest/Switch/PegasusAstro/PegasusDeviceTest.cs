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
using NINA.Utility.SerialCommunication;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    internal class PegasusDeviceTest {
        private Mock<ICommand> _mockCommand;
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
        [TestCase("P1:0\n", "P1:0", true)]
        [TestCase("P1:0\n", null, false)]
        public void TestSendCommand(string command, string response, bool valid) {
            //do not use a cache-able response like status response for the test
            _mockCommand = new Mock<ICommand>();
            _mockCommand.Setup(m => m.CommandString).Returns(command);
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(response);
            _mockSerialPort.Setup(m => m.PortName).Returns("COM3");
            _sut.InitializeSerialPort("AUTO", this);

            var result = _sut.SendCommand<SetPowerResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(SetPowerResponse)));
            Assert.That(result.IsValid, Is.EqualTo(valid));
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