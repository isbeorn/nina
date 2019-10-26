using Moq;
using NINA.Model.MyFlatDevice;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class AlnitakFlatDeviceTest {
        private AlnitakFlatDevice _sut;
        private Mock<ISerialPort> mockSerialPort;

        [SetUp]
        public async Task InitAsync() {
            _sut = new AlnitakFlatDevice("COM3");
            mockSerialPort = new Mock<ISerialPort>();
            _sut.SerialPort = mockSerialPort.Object;
            mockSerialPort.SetupProperty(m => m.PortName, "COM3");
            mockSerialPort.Setup(m => m.ReadLine()).Returns("*V99124");
            Assert.That(await _sut.Connect(new System.Threading.CancellationToken()), Is.True);
        }

        [TearDown]
        public void Dispose() {
            _sut.Disconnect();
            Assert.That(_sut.Connected, Is.False);
        }

        [Test]
        [TestCase(CoverState.NeitherOpenNorClosed, "*S99000")]
        [TestCase(CoverState.Closed, "*S99001")]
        [TestCase(CoverState.Open, "*S99002")]
        [TestCase(CoverState.Unknown, "*S99003")]
        [TestCase(CoverState.Unknown, "garbage")]
        [TestCase(CoverState.Unknown, null)]
        public void TestCoverState(CoverState coverState, string deviceResponse) {
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(_sut.CoverState, Is.EqualTo(coverState));
        }

        [Test]
        [TestCase(CoverState.Unknown, "*S99000")]
        [TestCase(CoverState.Unknown, "garbage")]
        [TestCase(CoverState.Unknown, null)]
        public void TestCoverStateDisconnected(CoverState coverState, string deviceResponse) {
            _sut.Disconnect();
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(_sut.CoverState, Is.EqualTo(coverState));
        }

        [Test]
        public void TestMinBrightness() {
            Assert.That(_sut.MinBrightness, Is.EqualTo(0));
        }

        [Test]
        public void TestMaxBrightness() {
            Assert.That(_sut.MaxBrightness, Is.EqualTo(255));
        }

        [Test]
        [TestCase(0, "*J99000")]
        [TestCase(255, "*J99255")]
        [TestCase(99, "*J99099")]
        [TestCase(0, "garbage")]
        [TestCase(0, null)]
        public void TestGetBrightness(int brightness, string deviceResponse) {
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(_sut.Brightness, Is.EqualTo(brightness));
        }

        [Test]
        [TestCase(0, "*J99000")]
        [TestCase(0, "*J99255")]
        [TestCase(0, "*J99099")]
        [TestCase(0, "garbage")]
        [TestCase(0, null)]
        public void TestGetBrightnessDisconnected(int brightness, string deviceResponse) {
            _sut.Disconnect();
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(_sut.Brightness, Is.EqualTo(brightness));
        }

        [Test]
        [TestCase(0, ">B000\r")]
        [TestCase(255, ">B255\r")]
        [TestCase(99, ">B099\r")]
        [TestCase(50, ">B050\r")]
        [TestCase(-1, ">B000\r")]
        [TestCase(256, ">B255\r")]
        public void TestSetBrightness(int brightness, string command) {
            string actual = null;
            mockSerialPort.Setup(m => m.Write(It.IsAny<string>())).Callback((string arg) => {
                actual = arg;
            });

            _sut.Brightness = brightness;
            Assert.That(actual, Is.EqualTo(command));
        }

        [Test]
        [TestCase(0, null)]
        [TestCase(255, null)]
        [TestCase(99, null)]
        [TestCase(50, null)]
        [TestCase(-1, null)]
        [TestCase(256, null)]
        public void TestSetBrightnessDisconnected(int brightness, string command) {
            string actual = null;
            mockSerialPort.Setup(m => m.Write(It.IsAny<string>())).Callback((string arg) => {
                actual = arg;
            });

            _sut.Disconnect();
            _sut.Brightness = brightness;
            Assert.That(actual, Is.EqualTo(command));
        }

        [Test]
        public async Task TestOpen() {
            mockSerialPort.SetupSequence(m => m.ReadLine()).Returns("*O99OOO")
                .Returns("*S99100") //motor running
                .Returns("*S99002") //motor stopped
                .Returns("*S99002"); //cover is open
            Assert.That(await _sut.Open(new System.Threading.CancellationToken()), Is.True);
        }

        [Test]
        public async Task TestOpenInvalidResponse() {
            mockSerialPort.SetupSequence(m => m.ReadLine()).Returns("")
                .Returns("*S99100") //motor running
                .Returns("*S99002") //motor stopped
                .Returns("*S99002"); //cover is open
            Assert.That(await _sut.Open(new System.Threading.CancellationToken()), Is.False);
        }

        [Test]
        public async Task TestClose() {
            mockSerialPort.SetupSequence(m => m.ReadLine()).Returns("*C99OOO")
                .Returns("*S99100") //motor running
                .Returns("*S99001") //motor stopped
                .Returns("*S99001"); //cover is closed
            Assert.That(await _sut.Close(new System.Threading.CancellationToken()), Is.True);
        }

        [Test]
        public async Task TestCloseInvalidResponse() {
            mockSerialPort.SetupSequence(m => m.ReadLine()).Returns("")
                .Returns("*S99100") //motor running
                .Returns("*S99001") //motor stopped
                .Returns("*S99001"); //cover is closed
            Assert.That(await _sut.Close(new System.Threading.CancellationToken()), Is.False);
        }

        [Test]
        [TestCase(">LOOO\r", "*L99OOO", true)]
        [TestCase(">LOOO\r", null, true)]
        [TestCase(">DOOO\r", "*L99OOO", false)]
        [TestCase(">DOOO\r", null, false)]
        public void TestSetLightOn(string command, string response, bool on) {
            string actual = null;
            mockSerialPort.Setup(m => m.ReadLine()).Returns(response);
            mockSerialPort.Setup(m => m.Write(It.IsAny<string>())).Callback((string arg) => {
                actual = arg;
            });
            _sut.LightOn = on;
            Assert.That(actual, Is.EqualTo(command));
        }

        [Test]
        [TestCase("*S99010", true)]
        [TestCase("*J99000", false)]
        [TestCase("garbage", false)]
        [TestCase(null, false)]
        public void TestGetLightOn(string response, bool on) {
            mockSerialPort.Setup(m => m.ReadLine()).Returns(response);
            Assert.That(_sut.LightOn, Is.EqualTo(on));
        }

        [Test]
        [TestCase("*S99010", false)]
        public void TestGetLightOnDisconnected(string response, bool on) {
            _sut.Disconnect();
            mockSerialPort.Setup(m => m.ReadLine()).Returns(response);
            Assert.That(_sut.LightOn, Is.EqualTo(on));
        }

        [Test]
        public void TestConnectedDisconnected() {
            //should be connected during setup
            Assert.That(_sut.Connected, Is.True);
            _sut.Disconnect();
            Assert.That(_sut.Connected, Is.False);
        }
    }
}