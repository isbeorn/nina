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
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
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
    public class AutoFocusReportTest {

        [Test]
        public void GenerateReport_DefaultValues_NotNull() {
            var profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.BacklashCompensationModel).Returns(BacklashCompensationModel.ABSOLUTE);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.BacklashIn).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.BacklashOut).Returns(0);

            var report = AutoFocusReport.GenerateReport(
                profileServiceMock.Object,
                "",
                "",
                new List<ScatterErrorPoint>(),
                0,
                0,
                new OxyPlot.DataPoint(),
                new ReportAutoFocusPoint(),
                new TrendlineFitting(),
                new QuadraticFitting(),
                new HyperbolicFitting(),
                new GaussianFitting(),
                0,
                "",
                TimeSpan.Zero
            );

            report.Should().NotBeNull();
        }

        [Test]
        [SetCulture("de-DE")]
        public void GenerateReport_Fittings_Culture_ProperlyParsed() {
            var profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.BacklashCompensationModel).Returns(BacklashCompensationModel.ABSOLUTE);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.BacklashIn).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.BacklashOut).Returns(0);

            var leftPoints = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 10.5, 1,1),
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

            var trendlineFitting = new TrendlineFitting();
            trendlineFitting.Calculate(points, AFMethodEnum.STARHFR.ToString());

            points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 2.5, 1,1),
                new ScatterErrorPoint(2, 3,  1,1),
                new ScatterErrorPoint(3, 6,  1,1),
                new ScatterErrorPoint(4, 11,  1,1),
                new ScatterErrorPoint(5, 19,  1,1),
                new ScatterErrorPoint(6, 11,  1,1),
                new ScatterErrorPoint(7, 6,  1,1),
                new ScatterErrorPoint(8, 3,  1,1),
                new ScatterErrorPoint(9, 2, 1,1),
            };

            var gaussianFitting = new GaussianFitting();
            gaussianFitting.Calculate(points);

            points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 18.5, 1,1),
                new ScatterErrorPoint(2, 11,  1,1),
                new ScatterErrorPoint(3, 6,  1,1),
                new ScatterErrorPoint(4, 3,  1,1),
                new ScatterErrorPoint(5, 2,  1,1),
                new ScatterErrorPoint(6, 3,  1,1),
                new ScatterErrorPoint(7, 6,  1,1),
                new ScatterErrorPoint(8, 11,  1,1),
                new ScatterErrorPoint(9, 18, 1,1),
            };

            var hyperbolicFitting = new HyperbolicFitting();
            hyperbolicFitting.Calculate(points);

            points = new List<ScatterErrorPoint>() {
                new ScatterErrorPoint(1, 18.5, 1,1),
                new ScatterErrorPoint(2, 11,  1,1),
                new ScatterErrorPoint(3, 6,  1,1),
                new ScatterErrorPoint(4, 3,  1,1),
                new ScatterErrorPoint(5, 2,  1,1),
                new ScatterErrorPoint(6, 3,  1,1),
                new ScatterErrorPoint(7, 6,  1,1),
                new ScatterErrorPoint(8, 11,  1,1),
                new ScatterErrorPoint(9, 18, 1,1),
            };

            var quadraticFitting = new QuadraticFitting();
            quadraticFitting.Calculate(points);

            var autoFocuserName = "AF";
            var starDetectorName = "SD";
            var report = AutoFocusReport.GenerateReport(
                profileServiceMock.Object,
                autoFocuserName,
                starDetectorName,
                new List<ScatterErrorPoint>(),
                0,
                0,
                new OxyPlot.DataPoint(),
                new ReportAutoFocusPoint(),
                trendlineFitting,
               quadraticFitting,
                hyperbolicFitting,
                gaussianFitting,
                0,
                "",
                TimeSpan.FromSeconds(5)
            );

            var cultureVerification = $"{3.42}";
            cultureVerification.Should().Be("3,42");
            report.Fittings.LeftTrend.Should().NotBeNull();
            report.Fittings.LeftTrend.Should().Be("y = -2.1499999999999995 * x + 12.5");
            report.Fittings.RightTrend.Should().NotBeNull();
            report.Fittings.RightTrend.Should().Be("y = 1.999999999999998 * x + -7.999999999999989");
            report.Fittings.Gaussian.Should().NotBeNull();
            report.Fittings.Gaussian.Should().Be("y = 15.243957112372783 * exp(-1 * (x - 4.999957314392612) * (x - 4.999957314392612) / (2 * 0.9899060517698882 * 0.9899060517698882)) + 2.8527614781671335");
            report.Fittings.Hyperbolic.Should().NotBeNull();
            report.Fittings.Hyperbolic.Should().Be("y = 1 * cosh(asinh((5.012500000000002 - x) / 0.2487552158808695))");
            report.Fittings.Quadratic.Should().NotBeNull();
            report.Fittings.Quadratic.Should().Be("y = 1.0151515151515171 * x^2 + -10.184848484848501 * x + 27.50000000000004");
            report.Duration.Should().Be(TimeSpan.FromSeconds(5));
            report.AutoFocuserName.Should().Be(autoFocuserName);
            report.StarDetectorName.Should().Be(starDetectorName);
        }
    }
}