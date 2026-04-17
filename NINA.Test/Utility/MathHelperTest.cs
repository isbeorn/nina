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
using System;

namespace NINA.Test.Utility {

    [TestFixture]
    public class MathHelperTest {

        /// <summary>
        /// Verifies hyperbolic functions against the equivalent BCL implementations over representative finite inputs.
        /// </summary>
        [Test]
        [TestCase(-2.5d)]
        [TestCase(0d)]
        [TestCase(2.5d)]
        public void HyperbolicFunctions_RepresentativeInputs_MatchSystemMath(double value) {
            Assert.That(MathHelper.HSin(value), Is.EqualTo(Math.Sinh(value)).Within(1e-12));
            Assert.That(MathHelper.HCos(value), Is.EqualTo(Math.Cosh(value)).Within(1e-12));
            Assert.That(MathHelper.HTan(value), Is.EqualTo(Math.Tanh(value)).Within(1e-12));
        }

        /// <summary>
        /// Verifies inverse hyperbolic functions are inverses of the matching hyperbolic functions within double precision.
        /// </summary>
        [Test]
        public void InverseHyperbolicFunctions_ValidDomains_InvertHyperbolicValues() {
            double sinhInput = 1.25d;
            double coshInput = 2.25d;
            double tanhInput = 0.5d;

            Assert.That(MathHelper.HArcsin(MathHelper.HSin(sinhInput)), Is.EqualTo(sinhInput).Within(1e-12));
            Assert.That(MathHelper.HArccos(MathHelper.HCos(coshInput)), Is.EqualTo(coshInput).Within(1e-12));
            Assert.That(MathHelper.HArctan(MathHelper.HTan(tanhInput)), Is.EqualTo(tanhInput).Within(1e-12));
        }
    }
}
