using Moq;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System;

namespace NINATest.SerialCommunication {

    internal class TestSdk : SerialSdk {
    }

    internal class TestCacheableResponse : Response {
        public override int Ttl => 50;
    }

    [TestFixture]
    public class SerialSdkTest {
        private Mock<ICommand> _mockCommand;
        private Mock<ISerialPort> _mockSerialPort;
        private TestSdk _sut;
        private const string COMMAND = "something";
        private const string DEVICE_RESPONSE = "something else";
        private const string DEVICE_RESPONSE2 = "something completely different";

        [SetUp]
        public void Init() {
            _mockCommand = new Mock<ICommand>();
            _mockCommand.Setup(m => m.CommandString).Returns(COMMAND);
            _mockSerialPort = new Mock<ISerialPort>();
            _sut = new TestSdk { SerialPort = _mockSerialPort.Object };
        }

        [Test]
        public void TestSendCommand() {
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE);
            var result = _sut.SendCommand<TestResponse>(_mockCommand.Object);
            Assert.That(result, Is.TypeOf(typeof(TestResponse)));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CheckDeviceResponse(DEVICE_RESPONSE), Is.True);
            _mockSerialPort.Verify(m => m.Write(COMMAND), Times.Once);
        }

        [Test]
        public void TestSendCommandTimeOut() {
            _mockSerialPort.Setup(m => m.ReadLine()).Throws(new TimeoutException());

            var result = _sut.SendCommand<TestResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(TestResponse)));
            Assert.That(result.IsValid, Is.False);
            _mockSerialPort.Verify(m => m.Write(COMMAND), Times.Once);
        }

        [Test]
        public void TestSendCommandOtherException() {
            _mockSerialPort.Setup(m => m.ReadLine()).Throws(new Exception());

            var result = _sut.SendCommand<TestResponse>(_mockCommand.Object);

            Assert.That(result, Is.TypeOf(typeof(TestResponse)));
            Assert.That(result.IsValid, Is.False);
            _mockSerialPort.Verify(m => m.Write(COMMAND), Times.Once);
        }

        [Test]
        public void TestResponseCacheNotCacheableResponse() {
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE);
            var result1 = _sut.SendCommand<TestResponse>(_mockCommand.Object);
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE2);
            var result2 = _sut.SendCommand<TestResponse>(_mockCommand.Object);
            Assert.That(result1.Equals(result2), Is.False);
        }

        [Test]
        public void TestResponseCacheCacheableResponse() {
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE);
            var result1 = _sut.SendCommand<TestCacheableResponse>(_mockCommand.Object);
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(DEVICE_RESPONSE2);
            var result2 = _sut.SendCommand<TestCacheableResponse>(_mockCommand.Object);
            Assert.That(result1.Equals(result2), Is.True);
        }
    }
}