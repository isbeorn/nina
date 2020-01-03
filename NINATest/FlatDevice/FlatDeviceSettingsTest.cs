#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System.Linq;
using NINA.Model.MyCamera;
using NINA.Profile;
using NUnit.Framework;

namespace NINATest.FlatDevice {

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
            var key = new FlatDeviceFilterSettingsKey(name, binning: new BinningMode(binX, binY), gain);
            var value = new FlatDeviceFilterSettingsValue(brightness, time);
            _sut.AddBrightnessInfo(key, value);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(new BinningMode(binX, binY)), Is.True);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoGains().Contains(gain), Is.True);
        }

        [Test]
        [TestCase("red", 1, 1, 0, 0.34, 1.0)]
        [TestCase("red", 2, 2, 0, 0.34, 1.0)]
        [TestCase("red", 2, 1, 0, 0.34, 1.0)]
        [TestCase("", 2, 1, 0, 0.34, 1.0)]
        [TestCase("very long filter name", 10, 10, 0, 0.34, 1.0)]
        [TestCase(null, 2, 1, 0, 0.34, 1.0)]
        [TestCase(null, 2, 1, null, 0.34, 1.0)]
        public void TestBrightnessInfoKeyEquivalence(string name, short binX, short binY, short gain, double time, double brightness) {
            var key = new FlatDeviceFilterSettingsKey(name, binning: new BinningMode(binX, binY), gain);
            var value = new FlatDeviceFilterSettingsValue(brightness, time);
            _sut.AddBrightnessInfo(key, value);
            key = new FlatDeviceFilterSettingsKey(name, binning: new BinningMode(binX, binY), gain);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
        }

        [Test]
        [TestCase("red", 0, 0.34, 1.0)]
        [TestCase("red", 0, 0.34, 1.0)]
        [TestCase("red", 0, 0.34, 1.0)]
        [TestCase("", 0, 0.34, 1.0)]
        [TestCase("very long filter name", 0, 0.34, 1.0)]
        [TestCase(null, 0, 0.34, 1.0)]
        [TestCase(null, null, 0.34, 1.0)]
        public void TestBrightnessInfoKeyEquivalenceNullBinning(string name, short gain, double time, double brightness) {
            var key = new FlatDeviceFilterSettingsKey(name, binning: null, gain);
            var value = new FlatDeviceFilterSettingsValue(brightness, time);
            _sut.AddBrightnessInfo(key, value);
            key = new FlatDeviceFilterSettingsKey(name, binning: null, gain);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
        }

        [Test]
        [TestCase("red", 30, 0.34, 1.0)]
        [TestCase(null, 30, 0.34, 1.0)]
        [TestCase("red", null, 0.34, 1.0)]
        [TestCase(null, null, 0.34, 1.0)]
        public void TestAddBrightnessInfoNullBinning(string name, short gain, double time, double brightness) {
            var key = new FlatDeviceFilterSettingsKey(name, binning: null, gain);
            var value = new FlatDeviceFilterSettingsValue(brightness, time);
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
            var key = new FlatDeviceFilterSettingsKey("red", new BinningMode(1, 1), 30);
            var value = new FlatDeviceFilterSettingsValue(0.5, 0.75);
            _sut.AddBrightnessInfo(key, value);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(new BinningMode(1, 1)), Is.True);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoGains().Contains((short)30), Is.True);

            //test
            value = new FlatDeviceFilterSettingsValue(0.25, 0.6); ;
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
            var key = new FlatDeviceFilterSettingsKey("red", new BinningMode(1, 1), 30);
            var value = new FlatDeviceFilterSettingsValue(0.5, 0.75);
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