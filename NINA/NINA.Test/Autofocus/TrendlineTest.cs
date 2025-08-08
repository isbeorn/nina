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
using NINA.ViewModel;
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
    public class TrendlineTest {
        private static double TOLERANCE = 0.000000000001;

        [Test]
        public void NoPoints_Zero() {
            var points = new List<ScatterErrorPoint>();

            var sut = new Trendline(points);

            sut.DataPoints.Should().BeEmpty();
            sut.Slope.Should().Be(0);
            sut.Offset.Should().Be(0);
        }

        [Test]
        public void OnePoint_Zero() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(5,5,5,5)
            };

            var sut = new Trendline(points);

            sut.DataPoints.Should().BeEquivalentTo(points);
            sut.Slope.Should().Be(0);
            sut.Offset.Should().Be(0);
        }

        [Test]
        public void TwoPoints_LinearFromOrigin_SameWeight_Calculated() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(0,0,5,5),
                new ScatterErrorPoint(1,1,5,5)
            };

            var sut = new Trendline(points);

            sut.DataPoints.Should().BeEquivalentTo(points);
            sut.Slope.Should().Be(1);
            sut.Offset.Should().Be(0);
        }

        [Test]
        public void TwoPoints_LinearFromOrigin_DifferentWeight_Calculated() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(0,0,5,5),
                new ScatterErrorPoint(1,1,1,1)
            };

            var sut = new Trendline(points);

            sut.DataPoints.Should().BeEquivalentTo(points);
            sut.Slope.Should().Be(1);
            sut.Offset.Should().Be(0);
        }

        [Test]
        public void MultiplePoints_SameWeight_Calculated() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 10, 1,1),
                new ScatterErrorPoint(2, 8,  1,1),
                new ScatterErrorPoint(3, 6,  1,1),
                new ScatterErrorPoint(4, 4,  1,1)
            };

            var sut = new Trendline(points);

            sut.DataPoints.Should().BeEquivalentTo(points);
            sut.Slope.Should().BeApproximately(-2, TOLERANCE);
            sut.Offset.Should().BeApproximately(12, TOLERANCE);
        }

        [Test]
        public void MultiplePoints_DifferentWeight_Calculated() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 10, 1,1),
                new ScatterErrorPoint(2, 8,  2,2),
                new ScatterErrorPoint(3, 6,  1,1),
                new ScatterErrorPoint(4, 4,  10,10)
            };

            var sut = new Trendline(points);

            sut.DataPoints.Should().BeEquivalentTo(points);
            sut.Slope.Should().BeApproximately(-2, TOLERANCE);
            sut.Offset.Should().BeApproximately(12, TOLERANCE);
        }

        [Test]
        public void MultipleCoarsePoints_DifferentWeight_Calculated() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 10, 1,1),
                new ScatterErrorPoint(2, 8.5,  1,1000000),
                new ScatterErrorPoint(3, 5.5,  1,1000000),
                new ScatterErrorPoint(4, 4,  1,1)
            };

            var sut = new Trendline(points);

            sut.DataPoints.Should().BeEquivalentTo(points);
            sut.Slope.Should().BeApproximately(-2, TOLERANCE);
            sut.Offset.Should().BeApproximately(12, TOLERANCE);
        }
    }
}