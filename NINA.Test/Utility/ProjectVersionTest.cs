#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NUnit.Framework;

namespace NINA.Test.Utility {

    [TestFixture]
    public class ProjectVersionTest {

        /// <summary>
        /// Verifies the documented release-channel display contract for release, hotfix, beta, RC, and nightly revisions.
        /// </summary>
        [Test]
        [TestCase("1.8.0.9001", "1.8 ")]
        [TestCase("1.8.1.9001", "1.8 HF1 ")]
        [TestCase("1.8.0.2004", "1.8 BETA004")]
        [TestCase("1.8.0.3001", "1.8 RC001")]
        [TestCase("1.8.0.1022", "1.8 NIGHTLY #022")]
        public void ToString_DocumentedVersionSchemes_ReturnsFriendlyName(string version, string expected) {
            ProjectVersion projectVersion = new ProjectVersion(version);

            Assert.That(projectVersion.ToString(), Is.EqualTo(expected));
        }
    }
}
