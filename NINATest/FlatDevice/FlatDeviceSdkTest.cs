using Moq;
using NINA.Utility.Extensions;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace NINATest.FlatDevice {

    [TestFixture]
    internal class FlatDeviceSdkTest {
        private Mock<ICommand> _mockCommand;
        private Mock<ISerialPort> _mockSerialPort;
        private Mock<ISerialPortProvider> _mockSerialPortProvider;
        private IAlnitakDevice _sut;

        [SetUp]
        public void Init() {
            _mockSerialPort = new Mock<ISerialPort>();
            _mockSerialPort.Setup(m => m.ReadLine()).Returns("*P99OOO");

            _mockSerialPortProvider = new Mock<ISerialPortProvider>();
            _mockSerialPortProvider.Setup(m => m.GetSerialPort(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Parity>(),
                    It.IsAny<int>(), It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(_mockSerialPort.Object);
            _mockSerialPortProvider.Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new ReadOnlyCollection<string>(new List<string> { "COM3" }));
            _sut = AlnitakDevice.Instance;
            _sut.SerialPortProvider = _mockSerialPortProvider.Object;
            _sut.InitializeSerialPort("AUTO");
        }

        [Test]
        [TestCase("AUTO", true, true)]
        [TestCase("AUTO", false, false)]
        [TestCase("COM3", true, true)]
        [TestCase(null, false, true)]
        [TestCase("", false, true)]
        public void TestInitializeSerialPort(string portName, bool expected, bool portsAvailable) {
            if (portsAvailable) {
                _mockSerialPortProvider.Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(new ReadOnlyCollection<string>(new List<string> { "COM3" }));
            } else {
                _mockSerialPortProvider.Setup(m => m.GetPortNames(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(new ReadOnlyCollection<string>(new List<string>()));
                _mockSerialPortProvider.Setup(m => m.GetSerialPort(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Parity>(),
                        It.IsAny<int>(), It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                        It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Returns((ISerialPort)null);
            }
            Assert.That(_sut.InitializeSerialPort(portName), Is.EqualTo(expected));
        }

        [Test]
        [TestCase(">SOOO\r", "*S99000", true)]
        [TestCase(">SOOO\r", null, false)]
        public void TestSendCommand(string command, string response, bool valid) {
            _mockCommand = new Mock<ICommand>();
            _mockCommand.Setup(m => m.CommandString).Returns(command);
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(response);

            var result = _sut.SendCommand<StateResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(StateResponse)));
            Assert.That(result.IsValid, Is.EqualTo(valid));
            _mockSerialPort.Verify(m => m.Open(), Times.Once);
            _mockSerialPort.Verify(m => m.Write(command), Times.Once);
            _mockSerialPort.Verify(m => m.Close(), Times.Once);
        }

        [Test]
        public void TestSendCommandTimeOut() {
            var command = ">SOOO\r";
            _mockCommand = new Mock<ICommand>();
            _mockCommand.Setup(m => m.CommandString).Returns(command);
            _mockSerialPort.Setup(m => m.ReadLine()).Throws(new TimeoutException());

            var result = _sut.SendCommand<StateResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(StateResponse)));
            Assert.That(result.IsValid, Is.False);
            _mockSerialPort.Verify(m => m.Open(), Times.Once);
            _mockSerialPort.Verify(m => m.Write(command), Times.Once);
            _mockSerialPort.Verify(m => m.Close(), Times.Once);
        }

        [Test]
        public void TestSendCommandOtherException() {
            var command = ">SOOO\r";
            _mockCommand = new Mock<ICommand>();
            _mockCommand.Setup(m => m.CommandString).Returns(command);
            _mockSerialPort.Setup(m => m.ReadLine()).Throws(new Exception());

            var result = _sut.SendCommand<StateResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(StateResponse)));
            Assert.That(result.IsValid, Is.False);
            _mockSerialPort.Verify(m => m.Open(), Times.Once);
            _mockSerialPort.Verify(m => m.Write(command), Times.Once);
            _mockSerialPort.Verify(m => m.Close(), Times.Once);
        }
    }
}