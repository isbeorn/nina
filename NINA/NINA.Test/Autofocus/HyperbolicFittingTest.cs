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
    public class HyperbolicFittingTest {
        private static double TOLERANCE = 0.000000000001;

        [Test]
        public void PerfectVCurve_OnlyOnePointMinimum() {
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

            var sut = new HyperbolicFitting();
            sut.Calculate(points);

            sut.Minimum.X.Should().BeApproximately(5, TOLERANCE);
            sut.Minimum.Y.Should().BeApproximately(1.2, TOLERANCE);
            sut.Fitting(sut.Minimum.X).Should().Be(sut.Minimum.Y);
        }

        [Test]
        public void BadData_PreventInfiniteLoop() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1000, 18, 1,1),
                new ScatterErrorPoint(1100, 0,  1,1),
                new ScatterErrorPoint(1200, 0,  1,1)
            };

            var sut = new HyperbolicFitting();
            sut.Calculate(points);

            sut.Minimum.X.Should().Be(0);
            sut.Minimum.Y.Should().Be(0);
            sut.Fitting.Should().BeNull();
        }

        [Test]
        public void BadData2_PreventInfiniteLoop() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1000, 18, 1,1),
                new ScatterErrorPoint(1000, 18, 1,1),
                new ScatterErrorPoint(1000, 18, 1,1),
                new ScatterErrorPoint(1100, 0,  1,1),
                new ScatterErrorPoint(1200, 0,  1,1)
            };

            var sut = new HyperbolicFitting();
            sut.Calculate(points);

            sut.Minimum.X.Should().Be(0);
            sut.Minimum.Y.Should().Be(0);
            sut.Fitting.Should().BeNull();
        }

        [Test]
        public void BadData3_PreventInfiniteLoop() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(900, 18, 1,1),
                new ScatterErrorPoint(1000, 18, 1,1),
                new ScatterErrorPoint(1000, 18, 1,1),
                new ScatterErrorPoint(1100, 0,  1,1),
                new ScatterErrorPoint(1200, 0,  1,1)
            };

            var sut = new HyperbolicFitting();
            sut.Calculate(points);

            sut.Minimum.X.Should().Be(0);
            sut.Minimum.Y.Should().Be(0);
            sut.Fitting.Should().BeNull();
        }

        [Test]
        public void BadData4_PreventInfiniteLoop() {
            var points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(800, 18, 1,1),
                new ScatterErrorPoint(900, 0, 1,1),
                new ScatterErrorPoint(1000, 0, 1,1),
                new ScatterErrorPoint(1000, 18, 1,1),
                new ScatterErrorPoint(1000, 18, 1,1),
                new ScatterErrorPoint(1100, 0,  1,1),
                new ScatterErrorPoint(1200, 0,  1,1)
            };

            var sut = new HyperbolicFitting();
            sut.Calculate(points);

            sut.Minimum.X.Should().Be(0);
            sut.Minimum.Y.Should().Be(0);
            sut.Fitting.Should().BeNull();
        }
    }
}