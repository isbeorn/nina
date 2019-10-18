using Moq;
using NINA.Model.MyFlatDevice;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NINATest
{
    [TestFixture]
    public class AlnitakFlatDeviceTest
    {
        private AlnitakFlatDevice _sut;
        private Mock<ISerialPort> mockSerialPort;

        [SetUp]
        public async Task InitAsync()
        {
            _sut = new AlnitakFlatDevice("COM3");
            mockSerialPort = new Mock<ISerialPort>();
            _sut.SerialPort = mockSerialPort.Object;
            mockSerialPort.SetupProperty(m => m.PortName, "COM3");
            mockSerialPort.Setup(m => m.ReadLine()).Returns("*P99OOO");
            Assert.That(await _sut.Connect(new System.Threading.CancellationToken()), Is.True);
        }

        [TearDown]
        public void Dispose()
        {
            _sut.Disconnect();
            Assert.AreEqual(false, _sut.Connected);
        }

        [Test]
        public async Task TestConnect()
        {
            Assert.That(await _sut.Connect(new System.Threading.CancellationToken()), Is.True);
        }

        [Test]
        [TestCase(CoverState.NotOpenClosed, "*S99000")]
        [TestCase(CoverState.Closed, "*S99001")]
        [TestCase(CoverState.Open, "*S99002")]
        [TestCase(CoverState.Unknown, "*S99003")]
        [TestCase(CoverState.Unknown, "garbage")]
        public void TestCoverStatus(CoverState coverState, string deviceResponse)
        {
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(_sut.CoverState, Is.EqualTo(coverState));
        }

        [Test]
        public void TestMinBrightness()
        {
            Assert.That(_sut.MinBrightness, Is.EqualTo(0));
        }

        [Test]
        public void TestMaxBrightness()
        {
            Assert.That(_sut.MaxBrightness, Is.EqualTo(255));
        }

        [Test]
        [TestCase(0, "*J99000")]
        [TestCase(255, "*J99255")]
        [TestCase(99, "*J99099")]
        [TestCase(0, "garbage")]
        public void TestGetBrightness(int brightness, string deviceResponse)
        {
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(_sut.Brightness, Is.EqualTo(brightness));
        }

        [Test]
        [TestCase(0, ">B000\r")]
        [TestCase(255, ">B255\r")]
        [TestCase(99, ">B099\r")]
        [TestCase(50, ">B050\r")]
        public void TestSetBrightness(int brightness, string command)
        {
            string actual = null;
            mockSerialPort.Setup(m => m.Write(It.IsAny<string>())).Callback((string arg) =>
            {
                actual = arg;
            });

            _sut.Brightness = brightness;
            Assert.That(actual, Is.EqualTo(command));
        }

        [Test]
        [TestCase("Flat-Man_XL on port COM3. Firmware version: 123", "*P10OOO")]
        [TestCase("Flat-Man_L on port COM3. Firmware version: 123", "*P15OOO")]
        [TestCase("Flat-Man on port COM3. Firmware version: 123", "*P19OOO")]
        [TestCase("Flip-Mask/Remote Dust Cover on port COM3. Firmware version: 123", "*P98OOO")]
        [TestCase("Flip-Flat on port COM3. Firmware version: 123", "*P99OOO")]
        // [TestCase("Unknown device on port COM3. Firmware version: 123", "garbage")] won't reach this code path because only valid responses from device get evaluated
        public async Task TestDescription(string description, string deviceResponse)
        {
            mockSerialPort.SetupSequence(m => m.ReadLine()).Returns(deviceResponse).Returns("*V99123");
            Assert.That(await _sut.Connect(new System.Threading.CancellationToken()), Is.True);
            Assert.That(_sut.Description, Is.EqualTo(description));
        }

        [Test]
        public async Task TestOpen()
        {
            mockSerialPort.SetupSequence(m => m.ReadLine()).Returns("*O99OOO")
                .Returns("*S99100") //motor running
                .Returns("*S99002") //motor stopped
                .Returns("*S99002"); //cover is open
            Assert.That(await _sut.Open(new System.Threading.CancellationToken()), Is.True);
        }

        [Test]
        public async Task TestClose()
        {
            mockSerialPort.SetupSequence(m => m.ReadLine()).Returns("*C99OOO")
                .Returns("*S99100") //motor running
                .Returns("*S99001") //motor stopped
                .Returns("*S99001"); //cover is closed
            Assert.That(await _sut.Close(new System.Threading.CancellationToken()), Is.True);
        }

        [Test]
        [TestCase(">LOOO\r", true)]
        [TestCase(">DOOO\r", false)]
        public void TestLightOn(string command, bool on)
        {
            string actual = null;
            mockSerialPort.Setup(m => m.ReadLine()).Returns("*L99OOO");
            mockSerialPort.Setup(m => m.Write(It.IsAny<string>())).Callback((string arg) =>
            {
                actual = arg;
            });
            _sut.LightOn = true;
            Assert.That(actual, Is.EqualTo(">LOOO\r"));
        }

        [Test]
        public void TestConnectedDisconnected()
        {
            //should be connected during setup
            Assert.That(_sut.Connected, Is.True);
            _sut.Disconnect();
            Assert.That(_sut.Connected, Is.False);
        }
    }
}