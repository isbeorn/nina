#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Profile;
using NINA.ViewModel.Equipment.FlatDevice;
using NUnit.Framework;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class FilterTimingTest {
        private Mock<IProfileService> _mockProfileService;
        private Mock<IFlatDeviceSettings> _mockSettings;
        private const double BRIGHTNESS = 1d;
        private const double TIME = 2d;
        private readonly FlatDeviceFilterSettingsKey _key = new FlatDeviceFilterSettingsKey(null, null, 0);

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockProfileService = new Mock<IProfileService>();
            _mockSettings = new Mock<IFlatDeviceSettings>();
        }

        [SetUp]
        public void Init() {
            _mockProfileService.Reset();
            _mockSettings.Reset();
        }

        [Test]
        public void TestStoreBrightness() {
            FlatDeviceFilterSettingsKey keyUsed = null;
            FlatDeviceFilterSettingsValue valueUsed = null;
            var sut = new FilterTiming(_mockProfileService.Object, _key, false);
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(_mockSettings.Object);
            _mockSettings.Setup(m => m.GetBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>()))
                .Returns(new FlatDeviceFilterSettingsValue(BRIGHTNESS, TIME));
            _mockSettings.Setup(m =>
                    m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(),
                        It.IsAny<FlatDeviceFilterSettingsValue>()))
                .Callback((FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) => {
                    keyUsed = key;
                    valueUsed = value;
                });

            sut.Brightness = BRIGHTNESS;
            _mockSettings.Verify(m =>
                m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            Assert.That(keyUsed, Is.EqualTo(_key));
            Assert.That(valueUsed.Time, Is.EqualTo(TIME));
            Assert.That(valueUsed.Brightness, Is.EqualTo(BRIGHTNESS));
        }

        [Test]
        public void TestStoreTime() {
            FlatDeviceFilterSettingsKey keyUsed = null;
            FlatDeviceFilterSettingsValue valueUsed = null;
            var sut = new FilterTiming(_mockProfileService.Object, _key, false);
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(_mockSettings.Object);
            _mockSettings.Setup(m => m.GetBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>()))
                .Returns(new FlatDeviceFilterSettingsValue(BRIGHTNESS, TIME));
            _mockSettings.Setup(m =>
                    m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(),
                        It.IsAny<FlatDeviceFilterSettingsValue>()))
                .Callback((FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) => {
                    keyUsed = key;
                    valueUsed = value;
                });

            sut.Time = TIME;
            _mockSettings.Verify(m =>
                m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            Assert.That(keyUsed, Is.EqualTo(_key));
            Assert.That(valueUsed.Time, Is.EqualTo(TIME));
            Assert.That(valueUsed.Brightness, Is.EqualTo(BRIGHTNESS));
        }
    }
}