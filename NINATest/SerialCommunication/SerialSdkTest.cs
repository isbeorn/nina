using Moq;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;

namespace NINATest.SerialCommunication {

    internal class TestSdk : SerialSdk {
    }

    internal class TestCacheableResponse : Response {
        public override int Ttl => 500;
    }

    [TestFixture]
    public class SerialSdkTest {
        private TestSdk _sut;
        private Mock<ISerialPort> _mockSerialPort;
        private Mock<ICommand> _mockCommand;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockCommand = new Mock<ICommand>();
            _mockSerialPort = new Mock<ISerialPort>();
        }

        [SetUp]
        public void Init() {
            _mockCommand.Reset();
            _mockCommand.Setup(m => m.CommandString).Returns(COMMAND);
            _mockCommand.Setup(m => m.HasResponse).Returns(true);
            _mockSerialPort.Reset();
            _sut = new TestSdk { SerialPort = _mockSerialPort.Object };
        }

        [Test]
        public void TestInitializeSerialPort() {
            _sut.SerialPort = null;
            var mockSerialPortProvider = new Mock<ISerialPortProvider>();
            mockSerialPortProvider.Setup(m => m.GetSerialPort(It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<Parity>(), It.IsAny<int>(),
                It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(_mockSerialPort.Object);
            _sut.SerialPortProvider = mockSerialPortProvider.Object;

            var result = _sut.InitializeSerialPort("COM3", this);

            Assert.That(result, Is.True);
            mockSerialPortProvider.Verify(m => m.GetSerialPort(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<Parity>(), It.IsAny<int>(),
                    It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Once);
            _mockSerialPort.Verify(m => m.Open(), Times.Once);
        }

        [Test]
        public void TestInitializeSerialPortNullPort() {
            _sut.SerialPort = null;
            var mockSerialPortProvider = new Mock<ISerialPortProvider>();
            _sut.SerialPortProvider = mockSerialPortProvider.Object;

            var result = _sut.InitializeSerialPort(null, this);

            Assert.That(result, Is.False);
            mockSerialPortProvider.Verify(m => m.GetSerialPort(It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<Parity>(), It.IsAny<int>(),
                It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
            _mockSerialPort.Verify(m => m.Open(), Times.Never);
        }

        [Test]
        public void TestInitializeSerialPortAlreadyInitialized() {
            _mockSerialPort.SetupProperty(m => m.PortName, "COM3");
            var mockSerialPortProvider = new Mock<ISerialPortProvider>();
            _sut.SerialPortProvider = mockSerialPortProvider.Object;

            var result = _sut.InitializeSerialPort("COM3", this);

            Assert.That(result, Is.True);
            mockSerialPortProvider.Verify(m => m.GetSerialPort(It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<Parity>(), It.IsAny<int>(),
                It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void TestInitializeSerialPortException() {
            _sut.SerialPort = null;
            var mockSerialPortProvider = new Mock<ISerialPortProvider>();
            _sut.SerialPortProvider = mockSerialPortProvider.Object;

            mockSerialPortProvider.Setup(m => m.GetSerialPort(It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<Parity>(), It.IsAny<int>(),
                    It.IsAny<StopBits>(), It.IsAny<Handshake>(), It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Throws(new Exception());

            var result = _sut.InitializeSerialPort("COM3", this);

            Assert.That(result, Is.False);
            _mockSerialPort.Verify(m => m.Open(), Times.Never);
        }

        private const string COMMAND = "something";
        private const string DEVICE_RESPONSE = "something else";
        private const string DEVICE_RESPONSE2 = "something completely different";

        [Test]
        public async Task TestSendCommand() {
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE);

            var result = await _sut.SendCommand<TestResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(TestResponse)));
            Assert.That(result.CheckDeviceResponse(DEVICE_RESPONSE), Is.True);
            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void TestSendCommandNullSerialPort() {
            _sut.SerialPort = null;

            Assert.That(async () => await _sut.SendCommand<TestResponse>(_mockCommand.Object), Throws.TypeOf<InvalidDeviceResponseException>());

            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Never);
            _mockSerialPort.Verify(m => m.ReadLine(), Times.Never);
        }

        [Test]
        public void TestSendCommandNullCommand() {
            Assert.That(async () => await _sut.SendCommand<TestResponse>(null), Throws.TypeOf<ArgumentNullException>());

            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Never);
            _mockSerialPort.Verify(m => m.ReadLine(), Times.Never);
        }

        [Test]
        public void TestSendCommandNullCommandString() {
            _mockSerialPort.Setup(m => m.Write(It.IsAny<string>())).Throws(new ArgumentNullException());

            Assert.That(async () => await _sut.SendCommand<TestResponse>(_mockCommand.Object), Throws.TypeOf<ArgumentNullException>());

            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Once);
            _mockSerialPort.Verify(m => m.ReadLine(), Times.Never);
        }

        [Test]
        public async Task TestSendCommandTimeOutOnWriteButValidRead() {
            _mockSerialPort.Setup(m => m.Write(It.IsAny<string>())).Throws(new TimeoutException());
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE);

            var result = await _sut.SendCommand<TestResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(TestResponse)));
            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task TestSendCommandTimeoutOnReadRecoverable() {
            _mockSerialPort.SetupSequence(m => m.ReadLine())
                .Throws(new TimeoutException())
                .Returns(DEVICE_RESPONSE);

            var result = await _sut.SendCommand<TestResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(TestResponse)));
            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Once);
            _mockSerialPort.Verify(m => m.ReadLine(), Times.Exactly(2));
        }

        [Test]
        public void TestSendCommandTimeoutOnReadNonRecoverable() {
            _mockSerialPort.Setup(m => m.ReadLine()).Throws(new TimeoutException());

            Assert.That(async () => await _sut.SendCommand<TestResponse>(_mockCommand.Object), Throws.TypeOf<TimeoutException>());
            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Once);
            _mockSerialPort.Verify(m => m.DiscardInBuffer(), Times.Once);
        }

        [Test]
        public void TestSendCommandTimeoutOnReadNonRecoverableExceptionDuringCleanup() {
            _mockSerialPort.Setup(m => m.ReadLine()).Throws(new TimeoutException());
            _mockSerialPort.Setup(m => m.BytesToRead).Throws(new Exception());

            Assert.That(async () => await _sut.SendCommand<TestResponse>(_mockCommand.Object), Throws.TypeOf<SerialPortClosedException>());

            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Once);
            _mockSerialPort.Verify(m => m.DiscardInBuffer(), Times.Never);
        }

        [Test]
        public void TestSendCommandWriteInvalidOperationException() {
            _mockSerialPort.Setup(m => m.Write(It.IsAny<string>())).Throws(new InvalidOperationException());

            Assert.That(async () => await _sut.SendCommand<TestResponse>(_mockCommand.Object), Throws.TypeOf<SerialPortClosedException>());

            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Once);
            _mockSerialPort.Verify(m => m.ReadLine(), Times.Never);
        }

        [Test]
        public void TestSendCommandReadInvalidOperationException() {
            _mockSerialPort.Setup(m => m.ReadLine()).Throws(new InvalidOperationException());

            Assert.That(async () => await _sut.SendCommand<TestResponse>(_mockCommand.Object), Throws.TypeOf<SerialPortClosedException>());

            _mockSerialPort.Verify(m => m.Write(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task TestResponseCacheNotCacheableResponse() {
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE);
            var result1 = await _sut.SendCommand<TestResponse>(_mockCommand.Object);

            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE2);
            var result2 = await _sut.SendCommand<TestResponse>(_mockCommand.Object);

            Assert.That(result1.Equals(result2), Is.False);
        }

        [Test]
        public async Task TestResponseCacheCacheableResponse() {
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE);
            var result1 = await _sut.SendCommand<TestCacheableResponse>(_mockCommand.Object);

            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE2);
            var result2 = await _sut.SendCommand<TestCacheableResponse>(_mockCommand.Object);

            Assert.That(result1.Equals(result2), Is.True);
        }

        [Test]
        public async Task TestResponseHasNoResponse() {
            _mockCommand.Setup(m => m.HasResponse).Returns(false);
            var result = await _sut.SendCommand<TestCacheableResponse>(_mockCommand.Object);
            Assert.That(result, Is.Null);
            _mockSerialPort.Verify(m => m.ReadLine(), Times.Never);
        }

        [Test]
        public void TestDispose() {
            _mockSerialPort.SetupProperty(m => m.PortName, "COM3");
            var portSuccess = _sut.InitializeSerialPort("COM3", this);
            Assert.That(portSuccess, Is.True);

            _sut.Dispose(this);
            _mockSerialPort.Verify(m => m.Close(), Times.Once);
            Assert.That(_sut.SerialPort, Is.Null);
        }

        [Test]
        public void TestDisposePortIsAlreadyClosed() {
            _mockSerialPort.SetupProperty(m => m.PortName, "COM3");
            var portSuccess = _sut.InitializeSerialPort("COM3", this);
            Assert.That(portSuccess, Is.True);
            _mockSerialPort.Setup(m => m.Close()).Throws(new IOException());

            _sut.Dispose(this);
            Assert.That(_sut.SerialPort, Is.Null);
        }
    }
}