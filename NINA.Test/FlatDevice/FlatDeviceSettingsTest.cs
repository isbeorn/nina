#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using System.Linq;
using FluentAssertions;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;

namespace NINA.Test.FlatDevice {

    [TestFixture]
    public class FlatDeviceSettingsTest {
        private FlatDeviceSettings _sut;

        [SetUp]
        public void Init() {
            _sut = new FlatDeviceSettings();
        }

        [Test]
        [TestCase(0, 1, 1, 0, 0.34, 1)]
        [TestCase(1, 2, 2, 0, 0.34, 1)]
        [TestCase(2, 2, 1, 0, 0.34, 1)]
        [TestCase(-1, 2, 1, 0, 0.34, 1)]
        [TestCase(null, 2, 1, 0, 0.34, 1)]
        [TestCase(null, 2, 1, -1, 0.34, 1)]
        public void TrainedFlatExposureSetting_GeneralIterations(short? position, short binX, short binY, short gain, double time, int brightness) {
            _sut.AddTrainedFlatExposureSetting(position, new BinningMode(binX, binY), gain, -1, brightness, time);

            var info = _sut.GetTrainedFlatExposureSetting(position, new BinningMode(binX, binY), gain, -1);
            info.Time.Should().Be(time);
            info.Brightness.Should().Be(brightness);
            info.Binning.X.Should().Be(binX);
            info.Binning.Y.Should().Be(binY);
            info.Filter.Should().Be(position ?? -1);
            info.Gain.Should().Be(gain);
            info.Offset.Should().Be(-1);
        }

        [Test]
        [TestCase(0, 0, 0.34, 1)]
        [TestCase(-1, 0, 0.34, 1)]
        [TestCase(null, 0, 0.34, 1)]
        [TestCase(null, -1, 0.34, 1)]
        public void TrainedFlatExposureSetting_NullBinning(short? position, short gain, double time, int brightness) {
            _sut.AddTrainedFlatExposureSetting(position, null, gain, -1, brightness, time);

            var info = _sut.GetTrainedFlatExposureSetting(position, null, gain, -1);
            info.Time.Should().Be(time);
            info.Brightness.Should().Be(brightness);
            info.Binning.X.Should().Be(1);
            info.Binning.Y.Should().Be(1);
            info.Filter.Should().Be(position ?? -1);
            info.Gain.Should().Be(gain);
            info.Offset.Should().Be(-1);
        }

        [Test]
        [TestCase(0, 1, 1, 0, 0.34, 1)]
        [TestCase(1, 2, 2, 0, 0.34, 1)]
        [TestCase(2, 2, 1, 0, 0.34, 1)]
        [TestCase(-1, 2, 1, 0, 0.34, 1)]
        [TestCase(null, 2, 1, 0, 0.34, 1)]
        [TestCase(null, 2, 1, -1, 0.34, 1)]
        public void TrainedFlatExposureSetting_UpdateSetting(short? position, short binX, short binY, short gain, double time, int brightness) {
            _sut.AddTrainedFlatExposureSetting(position, new BinningMode(binX, binY), gain, -1, 0, 0);

            _sut.AddTrainedFlatExposureSetting(position, new BinningMode(binX, binY), gain, -1, brightness, time);

            var info = _sut.GetTrainedFlatExposureSetting(position, new BinningMode(binX, binY), gain, -1);
            info.Time.Should().Be(time);
            info.Brightness.Should().Be(brightness);
            info.Binning.X.Should().Be(binX);
            info.Binning.Y.Should().Be(binY);
            info.Filter.Should().Be(position ?? -1);
            info.Gain.Should().Be(gain);
            info.Offset.Should().Be(-1);
        }

        [Test]
        public void TrainedFlatExposureSetting_AddMultiple() {
            _sut.AddTrainedFlatExposureSetting(1, new BinningMode(1, 1), 10, -1, 50, 20);

            _sut.AddTrainedFlatExposureSetting(1, new BinningMode(1, 1), 20, -1, 100, 40);

            var info = _sut.GetTrainedFlatExposureSetting(1, new BinningMode(1, 1), 10, -1);
            info.Time.Should().Be(20);
            info.Brightness.Should().Be(50);
            info.Binning.X.Should().Be(1);
            info.Binning.Y.Should().Be(1);
            info.Filter.Should().Be(1);
            info.Gain.Should().Be(10);
            info.Offset.Should().Be(-1);

            var info2 = _sut.GetTrainedFlatExposureSetting(1, new BinningMode(1, 1), 20, -1);
            info2.Time.Should().Be(40);
            info2.Brightness.Should().Be(100);
            info2.Binning.X.Should().Be(1);
            info2.Binning.Y.Should().Be(1);
            info2.Filter.Should().Be(1);
            info2.Gain.Should().Be(20);
            info2.Offset.Should().Be(-1);
        }
    }
}