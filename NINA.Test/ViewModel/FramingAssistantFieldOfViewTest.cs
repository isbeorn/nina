#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.ViewModel.FramingAssistant;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class FramingAssistantFieldOfViewTest {

        [TestCase(1, 1, 1)]
        [TestCase(1, -1, 1.5)]
        [TestCase(200, 1, 180)]
        [TestCase(200, -1, 200)]
        public void AdjustFieldOfView_RespectsBothDirectionsAtMinimumAndMaximum(
            double fieldOfView,
            int delta,
            double expected) {
            FramingAssistantVM.AdjustFieldOfView(fieldOfView, delta).Should().Be(expected);
        }
    }
}
