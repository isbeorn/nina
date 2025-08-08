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
using NUnit.Framework;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.WPF.Base.Utility.AutoFocus;

namespace NINA.Test.Autofocus {

    [TestFixture]
    public class TrendlineFittingTest {
        private static double TOLERANCE = 0.000000000001;

        [Test]
        public void PerfectVCurve_OnlyOnePointMinimum() {
            var leftPoints = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 10, 1,1),
                new ScatterErrorPoint(2, 8,  1,1),
                new ScatterErrorPoint(3, 6,  1,1),
                new ScatterErrorPoint(4, 4,  1,1)
            };
            var rightPoints = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(9, 10, 1,1),
                new ScatterErrorPoint(8, 8,  1,1),
                new ScatterErrorPoint(7, 6,  1,1),
                new ScatterErrorPoint(6, 4,  1,1)
            };

            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(5, 2,  1,1),
            };
            points.AddRange(leftPoints);
            points.AddRange(rightPoints);

            var sut = new TrendlineFitting();
            sut.Calculate(points, AFMethodEnum.STARHFR.ToString());

            sut.Intersection.X.Should().BeApproximately(5, TOLERANCE);
            sut.Intersection.Y.Should().BeApproximately(2, TOLERANCE);
            sut.LeftTrend.DataPoints.Should().BeEquivalentTo(leftPoints);
            sut.RightTrend.DataPoints.Should().BeEquivalentTo(rightPoints);
        }

        [Test]
        public void PerfectVCurve_FlatTipWithMultiplePoints() {
            var leftPoints = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 10, 1,1),
                new ScatterErrorPoint(2, 8,  1,1),
                new ScatterErrorPoint(3, 6,  1,1),
                new ScatterErrorPoint(4, 4,  1,1)
            };
            var rightPoints = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(11, 10, 1,1),
                new ScatterErrorPoint(10, 8,  1,1),
                new ScatterErrorPoint(9, 6,  1,1),
                new ScatterErrorPoint(8, 4,  1,1)
            };

            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(5, 2.1,  1,1),
                new ScatterErrorPoint(6, 2,  1,1),
                new ScatterErrorPoint(7, 2.1,  1,1),
            };
            points.AddRange(leftPoints);
            points.AddRange(rightPoints);

            var sut = new TrendlineFitting();
            sut.Calculate(points, AFMethodEnum.STARHFR.ToString());

            sut.Intersection.X.Should().BeApproximately(6, TOLERANCE);
            sut.Intersection.Y.Should().BeApproximately(0, TOLERANCE);
            sut.LeftTrend.DataPoints.Should().BeEquivalentTo(leftPoints);
            sut.RightTrend.DataPoints.Should().BeEquivalentTo(rightPoints);
        }
    }
}