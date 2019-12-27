using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NUnit.Framework;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class AlnitakFlatDeviceConnectTest {
        private AlnitakFlatDevice _sut;
        private Mock<ISerialPort> _mockSerialPort;
        private Mock<IProfileService> _mockProfileService;

        [SetUp]
        public void Init() {
            _mockProfileService = new Mock<IProfileService>();
            _mockProfileService.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings.PortName, "");
            _sut = new AlnitakFlatDevice(_mockProfileService.Object);
            _mockSerialPort = new Mock<ISerialPort>();
            _sut.SerialPort = _mockSerialPort.Object;
            _mockSerialPort.SetupProperty(m => m.PortName, "COM3");
        }

        [TearDown]
        public void Dispose() {
            _sut.Disconnect();
            Assert.That(_sut.Connected, Is.False);
        }

        [Test]
        public async Task TestConnect() {
            _mockSerialPort.Setup(m => m.ReadLine()).Returns("*V99124");
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
            _mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(await _sut.Connect(new CancellationToken()), Is.EqualTo(connected));
            Assert.That(_sut.Description, Is.EqualTo(description));
        }

        [Test]
        public void TestConstructor() {
            _mockProfileService = new Mock<IProfileService>();
            _mockProfileService.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings.PortName, "");
            _sut = new AlnitakFlatDevice(_mockProfileService.Object);
            Assert.That(_sut.Id, Is.EqualTo("817b60ab-6775-41bd-97b5-3857cc676e51"));
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