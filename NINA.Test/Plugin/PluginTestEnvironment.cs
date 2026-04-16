#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NUnit.Framework;

namespace NINA.Test.Plugin {

    [SetUpFixture]
    public class PluginTestEnvironment {
        private string originalApplicationTempPath;
        private string pluginTestRoot;

        /// <summary>
        /// Redirects plugin filesystem tests to a workspace-local runtime root before NINA.Plugin
        /// constants are initialized, preventing tests from touching a user's real plugin folders.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp() {
            originalApplicationTempPath = CoreUtil.APPLICATIONTEMPPATH;
            pluginTestRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PluginTestRuntime", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pluginTestRoot);
            CoreUtil.APPLICATIONTEMPPATH = pluginTestRoot;
        }

        /// <summary>
        /// Restores the global application temp path and removes the isolated plugin runtime root
        /// created for plugin loader and installer tests.
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown() {
            CoreUtil.APPLICATIONTEMPPATH = originalApplicationTempPath;
            if (Directory.Exists(pluginTestRoot)) {
                Directory.Delete(pluginTestRoot, true);
            }
        }
    }
}
