using Moq;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;

namespace NINATest.SerialCommunication {

    [TestFixture]
    internal class ResponseCacheTest {
        private Mock<ICommand> _mockCommand;
        private Mock<Response> _mockResponse;
        private Mock<Response> _mockResponse2;
        private ResponseCache _sut;

        [SetUp]
        public void Init() {
            _mockCommand = new Mock<ICommand>();
            _mockResponse = new Mock<Response>();
            _mockResponse2 = new Mock<Response>();
            _sut = new ResponseCache();
        }

        [Test]
        public void TestAddNewCacheableResponse() {
            _mockResponse.Setup(m => m.Ttl).Returns(50);
            _mockResponse.Setup(m => m.IsValid).Returns(true);

            _sut.Add(_mockCommand.Object, _mockResponse.Object);
            var result = _sut.Get(_mockCommand.Object.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType()), Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(_mockResponse.Object));
        }

        [Test]
        public void TestDontCacheInvalidResponse() {
            _mockResponse.Setup(m => m.Ttl).Returns(50);
            _mockResponse.Setup(m => m.IsValid).Returns(false);

            _sut.Add(_mockCommand.Object, _mockResponse.Object);
            var result = _sut.Get(_mockCommand.Object.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType()), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestAddOverExistingCacheableResponse() {
            _mockResponse.Setup(m => m.Ttl).Returns(50);
            _mockResponse.Setup(m => m.IsValid).Returns(true);
            _mockResponse2.Setup(m => m.Ttl).Returns(50);
            _mockResponse2.Setup(m => m.IsValid).Returns(true);

            _sut.Add(_mockCommand.Object, _mockResponse.Object);
            _sut.Add(_mockCommand.Object, _mockResponse2.Object);
            var result = _sut.Get(_mockCommand.Object.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType()), Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(_mockResponse2.Object));
        }

        [Test]
        public void TestAddNewNonCacheableResponse() {
            _mockResponse.Setup(m => m.Ttl).Returns(0);

            _sut.Add(_mockCommand.Object, _mockResponse.Object);
            var result = _sut.Get(_mockCommand.Object.GetType());

            Assert.That(_sut.HasValidResponse(_mockCommand.Object.GetType()), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestNullInputs() {
            Assert.That(_sut.HasValidResponse(null), Is.False);
            Assert.That(_sut.Get(null), Is.Null);
            //below should not throw and exception
            _sut.Add(null, _mockResponse.Object);
            _sut.Add(_mockCommand.Object, null);
        }
    }
}