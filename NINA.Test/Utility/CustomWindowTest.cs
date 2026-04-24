#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility.WindowService;
using System.Threading;
using System.Windows;

namespace NINA.Test.Utility {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class CustomWindowTest {

        /// <summary>
        /// Verifies the custom first-paint workaround does not implicitly hide modal dialogs and force a null dialog result.
        /// </summary>
        [Test]
        public void ShowDialog_LoadedHandlerCanSetDialogResult() {
            EnsureApplication();
            CustomWindow sut = new CustomWindow();
            sut.Loaded += (sender, args) => sut.DialogResult = true;

            bool? result = sut.ShowDialog();

            Assert.That(result, Is.True);
        }

        private static void EnsureApplication() {
            if (Application.Current == null) {
                _ = new Application {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }
        }
    }
}
