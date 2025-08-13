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
using NINA.WPF.Base.Utility.AutoFocus;
using NUnit.Framework;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Autofocus {

    [TestFixture]
    public class QuadraticFittingTest {
        private static double TOLERANCE = 0.000000000001;

        [Test]
        public void PerfectVCurve_OnlyOnePointMinimum() {
            //(x-5)² + 2
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 18, 1,1),
                new ScatterErrorPoint(2, 11,  1,1),
                new ScatterErrorPoint(3, 6,  1,1),
                new ScatterErrorPoint(4, 3,  1,1),
                new ScatterErrorPoint(5, 2,  1,1),
                new ScatterErrorPoint(6, 3,  1,1),
                new ScatterErrorPoint(7, 6,  1,1),
                new ScatterErrorPoint(8, 11,  1,1),
                new ScatterErrorPoint(9, 18, 1,1),
            };

            var sut = new QuadraticFitting();
            sut.Calculate(points);

            sut.Minimum.X.Should().BeApproximately(5, TOLERANCE);
            sut.Minimum.Y.Should().BeApproximately(2, TOLERANCE);
            sut.Fitting(sut.Minimum.X).Should().Be(sut.Minimum.Y);
            sut.RSquared.Should().Be(1);
        }
    }
}