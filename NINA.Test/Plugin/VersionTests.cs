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
using NINA.Astrometry.Interfaces;
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class VersionTests {

        [Test]
        //Major
        [TestCase("1.0.0.0", "1.0.0.0", true)]
        [TestCase("2.0.0.0", "1.0.0.0", false)]
        [TestCase("1.0.0.0", "2.0.0.0", true)]
        //Minor
        [TestCase("0.1.0.0", "0.1.0.0", true)]
        [TestCase("0.0.0.0", "0.1.0.0", true)]
        [TestCase("0.1.0.0", "0.0.0.0", false)]
        //Patch
        [TestCase("0.0.1.0", "0.0.1.0", true)]
        [TestCase("0.0.0.0", "0.0.1.0", true)]
        [TestCase("0.0.1.0", "0.0.0.0", false)]
        //Build
        [TestCase("0.0.0.1", "0.0.0.1", true)]
        [TestCase("0.0.0.0", "0.0.0.1", true)]
        [TestCase("0.0.0.1", "0.0.0.0", false)]
        public void PluginVersion_IsPluginCompatible1(string pluginVersionString, string applicationVersionString, bool isCompatible) {
            var pluginVersion = new PluginVersion(pluginVersionString);
            var appVersion = new Version(applicationVersionString);

            PluginVersion.IsPluginCompatible(pluginVersion, appVersion).Should().Be(isCompatible);
        }

        [Test]
        //Major
        [TestCase("1.0.0.0", "1.0.0.0", true)]
        [TestCase("2.0.0.0", "1.0.0.0", false)]
        [TestCase("1.0.0.0", "2.0.0.0", true)]
        //Minor
        [TestCase("0.1.0.0", "0.1.0.0", true)]
        [TestCase("0.0.0.0", "0.1.0.0", true)]
        [TestCase("0.1.0.0", "0.0.0.0", false)]
        //Patch
        [TestCase("0.0.1.0", "0.0.1.0", true)]
        [TestCase("0.0.0.0", "0.0.1.0", true)]
        [TestCase("0.0.1.0", "0.0.0.0", false)]
        //Build
        [TestCase("0.0.0.1", "0.0.0.1", true)]
        [TestCase("0.0.0.0", "0.0.0.1", true)]
        [TestCase("0.0.0.1", "0.0.0.0", false)]
        public void PluginVersion_IsPluginCompatible2(string pluginVersionString, string applicationVersionString, bool isCompatible) {
            var pluginVersion = new PluginVersion(pluginVersionString);
            var appVersion = new PluginVersion(applicationVersionString);

            PluginVersion.IsPluginCompatible(pluginVersion, appVersion).Should().Be(isCompatible);
        }

        [Test]
        //Major
        [TestCase("1.0.0.0", "1.0.0.0", true)]
        [TestCase("1.0.0.0", "2.0.0.0", false)]
        [TestCase("2.0.0.0", "1.0.0.0", true)]
        //Minor
        [TestCase("0.1.0.0", "0.1.0.0", true)]
        [TestCase("0.0.0.0", "0.1.0.0", false)]
        [TestCase("0.1.0.0", "0.0.0.0", true)]
        //Patch
        [TestCase("0.0.1.0", "0.0.1.0", true)]
        [TestCase("0.0.0.0", "0.0.1.0", false)]
        [TestCase("0.0.1.0", "0.0.0.0", true)]
        //Build
        [TestCase("0.0.0.1", "0.0.0.1", true)]
        [TestCase("0.0.0.0", "0.0.0.1", false)]
        [TestCase("0.0.0.1", "0.0.0.0", true)]
        public void PluginVersion_IsPluginGreaterOrEqualVersion(string left, string right, bool isGreater) {
            var leftVersion = new PluginVersion(left);
            var rightVersion = new PluginVersion(right);

            PluginVersion.IsPluginGreaterOrEqualVersion(leftVersion, rightVersion).Should().Be(isGreater);
        }
    }
}