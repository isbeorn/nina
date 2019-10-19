using Moq;
using NINA.Model.MyFlatDevice;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class AlnitakFlatDeviceConnectTest {
        private AlnitakFlatDevice _sut;
        private Mock<ISerialPort> mockSerialPort;

        [SetUp]
        public async Task InitAsync() {
            _sut = new AlnitakFlatDevice("COM3");
            mockSerialPort = new Mock<ISerialPort>();
            _sut.SerialPort = mockSerialPort.Object;
            mockSerialPort.SetupProperty(m => m.PortName, "COM3");
        }

        [TearDown]
        public void Dispose() {
            _sut.Disconnect();
            Assert.AreEqual(false, _sut.Connected);
        }

        [Test]
        public async Task TestConnect() {
            mockSerialPort.Setup(m => m.ReadLine()).Returns("*V99124");
            Assert.That(await _sut.Connect(new System.Threading.CancellationToken()), Is.True);
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
            _sut.Disconnect();
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(await _sut.Connect(new System.Threading.CancellationToken()), Is.EqualTo(connected));
            Assert.That(_sut.Description, Is.EqualTo(description));
        }
    }
}