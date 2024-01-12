#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class PluginOptionsAccessorTest {
        private Guid pluginGuid;
        private PluginSettings pluginSettings;
        private Mock<IProfileService> mockProfileService;

        [SetUp]
        public void Setup() {
            pluginGuid = Guid.NewGuid();
            mockProfileService = new Mock<IProfileService>();
            pluginSettings = new PluginSettings();
            mockProfileService.SetupGet(ps => ps.ActiveProfile.PluginSettings).Returns(pluginSettings);
        }

        private IPluginOptionsAccessor GetSUT() {
            return new PluginOptionsAccessor(mockProfileService.Object, pluginGuid);
        }

        [Test]
        public void GetValueInt_NotExists_ReturnsDefault() {
            var sut = GetSUT();
            var defaultValue = 11;

            var value = sut.GetValueInt32("missing_name", defaultValue);
            value.Should().Be(defaultValue);
        }

        [Test]
        public void GetValueInt_Exists_ReturnsValue() {
            var sut = GetSUT();

            var defaultValue = 11;
            string valueName = "name";
            int setValue = 13;
            pluginSettings.SetValue(pluginGuid, valueName, setValue);

            var value = sut.GetValueInt32(valueName, defaultValue);
            value.Should().Be(setValue);
        }

        [Test]
        public void SetValueInt_UpdatesValue() {
            var sut = GetSUT();

            string valueName = "name";
            int setValue = 13;
            pluginSettings.SetValue(pluginGuid, valueName, setValue);

            int setValue2 = 15;
            sut.SetValueInt32(valueName, setValue2);

            var found = pluginSettings.TryGetValue(pluginGuid, valueName, out int value);
            found.Should().Be(true);
            value.Should().Be(setValue2);
        }
    }
}