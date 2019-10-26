using System.Threading;
using Moq;
using NINA.Model.MyFlatDevice;
using NUnit.Framework;
using System.Threading.Tasks;
using NINA.Profile;

namespace NINATest {

    [TestFixture]
    public class AlnitakFlatDeviceConnectTest {
        private AlnitakFlatDevice _sut;
        private Mock<ISerialPort> mockSerialPort;

        [SetUp]
        public void Init() {
            _sut = new AlnitakFlatDevice("COM3");
            mockSerialPort = new Mock<ISerialPort>();
            _sut.SerialPort = mockSerialPort.Object;
            mockSerialPort.SetupProperty(m => m.PortName, "COM3");
        }

        [TearDown]
        public void Dispose() {
            _sut.Disconnect();
            Assert.That(_sut.Connected, Is.False);
        }

        [Test]
        public async Task TestConnect() {
            mockSerialPort.Setup(m => m.ReadLine()).Returns("*V99124");
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);
        }

        [Test]
        [TestCase("Flat-Man_XL on port COM3. Firmware version: 123", "*V10123", true)]
        [TestCase("Flat-Man_L on port COM3. Firmware version: 123", "*V15123", true)]
        [TestCase("Flat-Man on port COM3. Firmware version: 123", "*V19123", true)]
        [TestCase("Flip-Mask/Remote Dust Cover on port COM3. Firmware version: 123", "*V98123", true)]
        [TestCase("Flip-Flat on port COM3. Firmware version: 123", "*V99123", true)]
        [TestCase(null, "garbage", false)]
        [TestCase(null, "*V99OOO", false)]
        [TestCase(null, null, false)]
        public async Task TestDescription(string description, string deviceResponse, bool connected) {
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(await _sut.Connect(new CancellationToken()), Is.EqualTo(connected));
            Assert.That(_sut.Description, Is.EqualTo(description));
        }

        [Test]
        public void TestConstructor() {
            _sut = new AlnitakFlatDevice("Alnitak;COM3", new Mock<IProfileService>().Object);
            Assert.That(_sut.Name, Is.EqualTo("Alnitak"));
        }

        [Test]
        public async Task TestOpenNotConnected() {
            Assert.That(await _sut.Open(new CancellationToken()), Is.False);
        }

        [Test]
        public async Task TestCloseNotConnected() {
            Assert.That(await _sut.Close(new CancellationToken()), Is.False);
        }
    }
}