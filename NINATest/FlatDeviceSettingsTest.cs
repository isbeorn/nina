using System.Linq;
using NINA.Model.MyCamera;
using NINA.Profile;
using NUnit.Framework;

namespace NINATest {

    [TestFixture]
    public class FlatDeviceSettingsTest {
        private FlatDeviceSettings _sut;

        [SetUp]
        public void Init() {
            _sut = new FlatDeviceSettings();
        }

        [Test]
        [TestCase("red", 1, 1, 0, 0.34, 1.0)]
        [TestCase("red", 2, 2, 0, 0.34, 1.0)]
        [TestCase("red", 2, 1, 0, 0.34, 1.0)]
        [TestCase("", 2, 1, 0, 0.34, 1.0)]
        [TestCase("very long filter name", 10, 10, 0, 0.34, 1.0)]
        [TestCase(null, 2, 1, 0, 0.34, 1.0)]
        [TestCase(null, 2, 1, null, 0.34, 1.0)]
        public void TestAddBrightnessInfo(string name, short binX, short binY, short gain, double time, double brightness) {
            var key = (name, binning: new BinningMode(binX, binY), gain);
            var value = (time, brightness);
            _sut.AddBrightnessInfo(key, value);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(new BinningMode(binX, binY)), Is.True);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoGains().Contains(gain), Is.True);
        }

        [Test]
        [TestCase("red", 30, 0.34, 1.0)]
        [TestCase(null, 30, 0.34, 1.0)]
        [TestCase("red", null, 0.34, 1.0)]
        [TestCase(null, null, 0.34, 1.0)]
        public void TestAddBrightnessInfoNullBinning(string name, short gain, double time, double brightness) {
            (string name, BinningMode binning, short gain) key = (name, binning: null, gain);
            var value = (time, brightness);
            _sut.AddBrightnessInfo(key, value);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(null), Is.True);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoGains().Contains(gain), Is.True);
        }

        [Test]
        public void TestUpdateBrightnessInfo() {
            //setup
            (string name, BinningMode binning, short gain) key = ("red", new BinningMode(1, 1), 30);
            var value = (0.5, 0.75);
            _sut.AddBrightnessInfo(key, value);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(new BinningMode(1, 1)), Is.True);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoGains().Contains((short)30), Is.True);

            //test
            value = (0.25, 0.6);
            _sut.AddBrightnessInfo(key, value);

            //Assert
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(new BinningMode(1, 1)), Is.True);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoGains().Contains((short)30), Is.True);
        }

        [Test]
        public void TestClearBrightnessInfo() {
            //setup
            (string name, BinningMode binning, short gain) key = ("red", new BinningMode(1, 1), 30);
            var value = (0.5, 0.75);
            _sut.AddBrightnessInfo(key, value);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(new BinningMode(1, 1)), Is.True);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoGains().Contains((short)30), Is.True);

            //test
            _sut.ClearBrightnessInfo();

            //Assert
            Assert.That(_sut.GetBrightnessInfo(key), Is.Null);
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(0));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(new BinningMode(1, 1)), Is.False);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(0));
            Assert.That(_sut.GetBrightnessInfoGains().Contains((short)30), Is.False);
        }
    }
}