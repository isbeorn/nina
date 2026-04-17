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
using Moq;
using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyGuider.PHD2.PhdEvents;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class GuiderModelBehaviorTest {

        /// <summary>
        /// Verifies guide-step history keeps a bounded window, recomputes RMS from raw guide data, and rescales displayed distances.
        /// </summary>
        [Test]
        public void GuideStepsHistory_AddGuideStepMaintainsWindowRmsAndDisplayScale() {
            var history = new GuideStepsHistory(historySize: 2, GuiderScaleEnum.ARCSECONDS, maxY: 4) {
                PixelScale = 2
            };

            history.AddGuideStep(CreateGuideStep(1, -1, 100, -150));
            history.AddGuideStep(CreateGuideStep(3, -3, -200, 250));
            history.AddGuideStep(CreateGuideStep(5, -5, 300, -350));

            GuideStepsHistory.HistoryStep[] steps = history.GuideSteps.ToArray();
            steps.Should().HaveCount(2);
            steps[0].RADistanceRaw.Should().Be(3);
            steps[0].RADistanceRawDisplay.Should().Be(6);
            steps[1].DECDistanceRaw.Should().Be(-5);
            steps[1].DECDistanceRawDisplay.Should().Be(-10);
            history.RMS.DataPoints.Should().Be(2);
            history.RMS.RA.Should().BeApproximately(1, 1e-10);
            history.RMS.Dec.Should().BeApproximately(1, 1e-10);
            history.MaxDurationY.Should().Be(350);
        }

        /// <summary>
        /// Verifies dither markers are appended as non-RMS guide history markers and clear resets both history and scaling state.
        /// </summary>
        [Test]
        public void GuideStepsHistory_DitherAndClearMaintainHistoryInvariants() {
            var history = new GuideStepsHistory(historySize: 5, GuiderScaleEnum.PIXELS, maxY: 4);
            history.AddGuideStep(CreateGuideStep(1, 1, 10, 20));

            history.AddDitherIndicator();
            GuideStepsHistory.HistoryStep dither = history.GuideSteps.Last();

            dither.Dither.Should().BeApproximately(0.01, 1e-10);
            history.RMS.DataPoints.Should().Be(1);

            history.Clear();
            history.GuideSteps.Should().BeEmpty();
            history.RMS.DataPoints.Should().Be(0);
            history.MaxDurationY.Should().Be(1);
        }

        /// <summary>
        /// Verifies lock positions compare by coordinates while preserving event time for diagnostics.
        /// </summary>
        [Test]
        public void LockPosition_EqualsUsesCoordinatesAndToStringIsDiagnostic() {
            var first = new LockPosition(10, 20);
            var same = new LockPosition(10, 20);
            var different = new LockPosition(11, 20);

            first.Should().Be(same);
            first.GetHashCode().Should().Be(same.GetHashCode());
            (first == same).Should().BeTrue();
            (first != different).Should().BeTrue();
            first.ToString().Should().Be("x=10 y=20");
            first.EventTime.Should().BeBefore(DateTime.Now.AddSeconds(1));
        }

        /// <summary>
        /// Verifies PHD2 guide-step event signs match NINA guider conventions and cloning preserves the received guide-step values.
        /// </summary>
        [Test]
        public void PhdEventGuideStep_AppliesDirectionSignConventionsAndClonesValues() {
            var step = new PhdEventGuideStep {
                RADistanceRaw = 1.5,
                DECDistanceRaw = -2.5,
                RADuration = 120,
                RADirection = "East",
                DECDuration = 80,
                DECDirection = "South",
                RADistanceGuide = 0.7,
                DECDistanceGuide = 0.9
            };

            IGuideStep clone = step.Clone();

            step.RADistanceRaw.Should().BeApproximately(-1.5, 1e-10);
            step.DECDistanceRaw.Should().BeApproximately(-2.5, 1e-10);
            step.RADuration.Should().Be(-120);
            step.DECDuration.Should().Be(-80);
            step.RADistanceGuideDisplay.Should().BeApproximately(0.7, 1e-10);
            step.DecDistanceGuideDisplay.Should().BeApproximately(0.9, 1e-10);
            clone.Should().NotBeSameAs(step);
            clone.RADistanceRaw.Should().Be(step.RADistanceRaw);
            clone.DECDuration.Should().Be(step.DECDuration);
        }

        /// <summary>
        /// Verifies RMS error DTOs expose both pixel and arcsecond values using the supplied guide scale.
        /// </summary>
        [Test]
        public void RMSError_ScalesPixelValuesToArcseconds() {
            var error = new RMSError(rA: 1.1, dec: 2.2, peakRA: 3.3, peakDec: 4.4, total: 5.5, scale: 1.5);

            error.RA.Pixel.Should().BeApproximately(1.1, 1e-10);
            error.RA.Arcseconds.Should().BeApproximately(1.65, 1e-10);
            error.PeakDec.Pixel.Should().BeApproximately(4.4, 1e-10);
            error.PeakDec.Arcseconds.Should().BeApproximately(6.6, 1e-10);
            error.Total.Arcseconds.Should().BeApproximately(8.25, 1e-10);
        }

        private static IGuideStep CreateGuideStep(double ra, double dec, double raDuration, double decDuration) {
            var step = new Mock<IGuideStep>();
            step.SetupProperty(x => x.RADistanceRaw, ra);
            step.SetupProperty(x => x.DECDistanceRaw, dec);
            step.SetupGet(x => x.RADuration).Returns(raDuration);
            step.SetupGet(x => x.DECDuration).Returns(decDuration);
            return step.Object;
        }
    }
}
