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
using NINA.Profile;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class PluginSettingsTest {

        [Test]
        public void TryGetTypeOfField_GuidDoesNotExist_NoSuccessAndTypeIsNull() {
            var sut = new PluginSettings();

            var success = sut.TryGetTypeOfField(Guid.NewGuid(), "SomeKey", out var type);

            success.Should().BeFalse();
            type.Should().BeNull();
        }

        [Test]
        public void TryGetTypeOfField_GuidExist_KeyDoesNotExist_NoSuccessAndTypeIsNull() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();
            sut.SetValue(id, "Something", 10);

            var success = sut.TryGetTypeOfField(id, "SomeKey", out var type);

            success.Should().BeFalse();
            type.Should().BeNull();
        }

        [Test]
        public void TryGetTypeOfField_GuidExist_KeyExist_SuccessAndTypeIsReturned() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();
            sut.SetValue(id, "SomeKey", 10d);

            var success = sut.TryGetTypeOfField(id, "SomeKey", out var type);

            success.Should().BeTrue();
            type.Should().Be(typeof(double));
        }

        [Test]
        public void SetValue_TryGetValueSuccessful_Boolean() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();

            sut.SetValue(id, "SomeKey", true);

            var success = sut.TryGetValue(id, "SomeKey", out bool value);
            success.Should().BeTrue();
            value.Should().BeTrue();
        }

        [Test]
        public void SetValue_TryGetValue_IncorrectType_Unsuccessful() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();

            sut.SetValue(id, "SomeKey", true);

            var success = sut.TryGetValue(id, "SomeKey", out int value);
            success.Should().BeFalse();
            value.Should().Be(default(int));
        }

        [Test]
        public void TryGetValue_GuidDoesNotExist_Unsuccessful() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();

            var success = sut.TryGetValue(id, "SomeKey", out int value);
            success.Should().BeFalse();
            value.Should().Be(default(int));
        }

        [Test]
        public void TryGetValue_KeyDoesNotExist_Unsuccessful() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();
            sut.SetValue(id, "Something", 5);

            var success = sut.TryGetValue(id, "SomeKey", out int value);
            success.Should().BeFalse();
            value.Should().Be(default(int));
        }
    }
}