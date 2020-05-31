#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
        [TestCase(0, 1, 1, 0, 0.34, 1.0)]
        [TestCase(1, 2, 2, 0, 0.34, 1.0)]
        [TestCase(2, 2, 1, 0, 0.34, 1.0)]
        [TestCase(-1, 2, 1, 0, 0.34, 1.0)]
        [TestCase(null, 2, 1, 0, 0.34, 1.0)]
        [TestCase(null, 2, 1, null, 0.34, 1.0)]
        public void TestAddBrightnessInfo(short? position, short binX, short binY, short gain, double time, double brightness) {
            var key = new FlatDeviceFilterSettingsKey(position, binning: new BinningMode(binX, binY), gain);
            var value = new FlatDeviceFilterSettingsValue(brightness, time);
            _sut.AddBrightnessInfo(key, value);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
            Assert.That(_sut.GetBrightnessInfoBinnings().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoBinnings().Contains(new BinningMode(binX, binY)), Is.True);
            Assert.That(_sut.GetBrightnessInfoGains().Count(), Is.EqualTo(1));
            Assert.That(_sut.GetBrightnessInfoGains().Contains(gain), Is.True);
        }

        [Test]
        [TestCase(0, 1, 1, 0, 0.34, 1.0)]
        [TestCase(1, 2, 2, 0, 0.34, 1.0)]
        [TestCase(2, 2, 1, 0, 0.34, 1.0)]
        [TestCase(-1, 2, 1, 0, 0.34, 1.0)]
        [TestCase(null, 2, 1, 0, 0.34, 1.0)]
        [TestCase(null, 2, 1, null, 0.34, 1.0)]
        public void TestBrightnessInfoKeyEquivalence(short? position, short binX, short binY, short gain, double time, double brightness) {
            var key = new FlatDeviceFilterSettingsKey(position, binning: new BinningMode(binX, binY), gain);
            var value = new FlatDeviceFilterSettingsValue(brightness, time);
            _sut.AddBrightnessInfo(key, value);
            key = new FlatDeviceFilterSettingsKey(position, binning: new BinningMode(binX, binY), gain);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
        }

        [Test]
        [TestCase(0, 0, 0.34, 1.0)]
        [TestCase(-1, 0, 0.34, 1.0)]
        [TestCase(null, 0, 0.34, 1.0)]
        [TestCase(null, null, 0.34, 1.0)]
        public void TestBrightnessInfoKeyEquivalenceNullBinning(short? position, short gain, double time, double brightness) {
            var key = new FlatDeviceFilterSettingsKey(position, binning: null, gain);
            var value = new FlatDeviceFilterSettingsValue(brightness, time);
            _sut.AddBrightnessInfo(key, value);
            key = new FlatDeviceFilterSettingsKey(position, binning: null, gain);
            Assert.That(_sut.GetBrightnessInfo(key), Is.EqualTo(value));
        }

        [Test]
        [TestCase(0, 30, 0.34, 1.0)]
        [TestCase(null, 30, 0.34, 1.0)]
        [TestCase(0, null, 0.34, 1.0)]
        [TestCase(null, null, 0.34, 1.0)]
        public void TestAddBrightnessInfoNullBinning(short? position, short gain, double time, double brightness) {
            var key = new FlatDeviceFilterSettingsKey(position, binning: null, gain);
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
            var key = new FlatDeviceFilterSettingsKey(0, new BinningMode(1, 1), 30);
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
            var key = new FlatDeviceFilterSettingsKey(0, new BinningMode(1, 1), 30);
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