using Moq;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace NINATest.FlatDevice {

    [TestFixture]
    internal class FlatDeviceSdkTest {
        private Mock<ISerialPort> _mockSerialPort;
        private Mock<ISerialPortProvider> _mockSerialPortProvider;
        private IAlnitakDevice _sut;

        [SetUp]
        public void Init() {
            _sut = AlnitakDevice.Instance;
            _mockSerialPort = new Mock<ISerialPort>();
            _mockSerialPort.Setup(m => m.PortName).Returns("COM3");
            _mockSerialPortProvider = new Mock<ISerialPortProvider>();
            _sut.SerialPortProvider = _mockSerialPortProvider.Object;
        }

        [TearDown]
        public void TearDown() {
            _sut.Dispose(this);
        }

        [Test]
        public void TestInitializeSerialPortNullPort() {
            Assert.That(_sut.InitializeSerialPort(null, this, 0).Result, Is.False);
        }

        [Test]
        public void TestInitializeSerialPortAlreadyInitialized() {
            _sut.SerialPort = _mockSerialPort.Object;
            Assert.That(_sut.InitializeSerialPort("COM3", this, 0).Result, Is.True);
            _mockSerialPortProvider.Verify(m => m.GetSerialPort(It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<Parity>(), It.IsAny<int>(),
                It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void TestInitializeSerialPort() {
            _mockSerialPortProvider.Setup(m => m.GetSerialPort(It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<Parity>(), It.IsAny<int>(),
                It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(_mockSerialPort.Object);
            Assert.That(_sut.InitializeSerialPort("COM3", this, 0).Result, Is.True);
            _mockSerialPort.Verify(m => m.Open(), Times.Once);
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
    }
}