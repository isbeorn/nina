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
        public async Task initAsync() {
            _sut = new AlnitakFlatDevice("COM3");
            mockSerialPort = new Mock<ISerialPort>();
            _sut.SerialPort = mockSerialPort.Object;
            mockSerialPort.Setup(m => m.ReadLine()).Returns("*P99OOO");
            Assert.That(await _sut.Connect(new System.Threading.CancellationToken()), Is.EqualTo(true));
        }

        [TearDown]
        public void Dispose() {
            _sut.Disconnect();
            Assert.AreEqual(false, _sut.Connected);
        }

        [Test]
        [TestCase(CoverState.NotOpenClosed, "*S99000")]
        [TestCase(CoverState.Closed, "*S99001")]
        [TestCase(CoverState.Open, "*S99002")]
        [TestCase(CoverState.UNKNOWN, "*S99003")]
        [TestCase(CoverState.UNKNOWN, "garbage")]
        public void TestCoverStatus(CoverState coverState, string deviceResponse) {
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
        public void TestGetBrightness(int brightness, string deviceResponse) {
            mockSerialPort.Setup(m => m.ReadLine()).Returns(deviceResponse);
            Assert.That(_sut.Brightness, Is.EqualTo(brightness));
        }
    }
}

/*            AlnitakFlatDevice sut = new AlnitakFlatDevice("COM3");
            Assert.AreEqual(true, await sut.Connect(new System.Threading.CancellationToken()));
            Assert.AreEqual(CoverState.Closed, sut.CoverState);
            */
/*            _ = sut.MaxBrightness;
            _ = sut.MinBrightness;
            _ = await sut.Connect(new System.Threading.CancellationToken());
            Console.WriteLine(sut.Description);
            _ = await sut.Open(new System.Threading.CancellationToken());
            _ = await sut.Close(new System.Threading.CancellationToken());
            Console.WriteLine(sut.CoverState);
            Console.WriteLine(sut.LightOn);
            Console.WriteLine(sut.Brightness);
            sut.LightOn = true;
            Console.WriteLine(sut.Brightness);
            sut.Brightness = 200;
            Console.WriteLine(sut.Brightness);
            sut.Brightness = sut.MinBrightness;
            Console.WriteLine(sut.Brightness);
            sut.Brightness = sut.MaxBrightness;
            Console.WriteLine(sut.Brightness);
            Console.WriteLine(sut.LightOn);
            Console.WriteLine(sut.LightOn);
            sut.LightOn = false;
            sut.Disconnect();*/