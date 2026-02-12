#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyGuider.PHD2;
using NINA.Core.Enum;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Interfaces;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyGuider.PHD2.PhdEvents;
using FluentAssertions;
using NUnit.Framework.Legacy;

namespace NINA.Test {

    [TestFixture]
    public class GuideStepsHistoryTest {

        [Test]
        public void GuideStepsHistory_ConstructorTest() {
            var historySize = 100;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.PIXELS, 4);

            Assert.That(gsh.HistorySize, Is.EqualTo(historySize));
            Assert.That(gsh.PixelScale, Is.EqualTo(1));
            Assert.That(gsh.Scale, Is.EqualTo(GuiderScaleEnum.PIXELS));
            Assert.That(gsh.GuideSteps.Count(), Is.EqualTo(0));
            Assert.That(gsh.RMS.Scale, Is.EqualTo(1));
            Assert.That(gsh.RMS.RA, Is.EqualTo(0));
            Assert.That(gsh.RMS.Dec, Is.EqualTo(0));
            Assert.That(gsh.RMS.Total, Is.EqualTo(0));
        }

        [Test]
        public void GuideStepsHistory_AddPHDDataPointsTest() {
            var historySize = 100;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.PIXELS, 4);

            IGuideStep step1 = new PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step2 = new PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            IGuideStep step3 = new PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step4 = new PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);

            Assert.That(gsh.RMS.RA, Is.EqualTo(300));
            Assert.That(gsh.RMS.Dec, Is.EqualTo(630));
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.That(gsh.RMS.Total, Is.EqualTo(total));
        }

        [Test]
        public void GuideStepsHistory_AddPHDDataPointsScaledTest() {
            var historySize = 100;
            var scale = 1.59;

            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.ARCSECONDS, 4);
            gsh.PixelScale = scale;

            IGuideStep step1 = new PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step2 = new PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            IGuideStep step3 = new PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step4 = new PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);

            Assert.That(gsh.RMS.RA, Is.EqualTo(300));
            Assert.That(gsh.RMS.Dec, Is.EqualTo(630));
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.That(gsh.RMS.Total, Is.EqualTo(total));
        }

        [Test]
        public void GuideStepsHistory_ClearTest() {
            var historySize = 100;
            var scale = 1.59;

            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.ARCSECONDS, 4);
            gsh.PixelScale = scale;

            IGuideStep step1 = new PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step2 = new PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            IGuideStep step3 = new PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step4 = new PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);

            gsh.Clear();

            Assert.That(gsh.GuideSteps.Count(), Is.EqualTo(0));
            Assert.That(gsh.RMS.RA, Is.EqualTo(0));
            Assert.That(gsh.RMS.Dec, Is.EqualTo(0));
            Assert.That(gsh.RMS.Total, Is.EqualTo(0));
        }

        public static List<IGuideStep> steps = new List<IGuideStep>();

        [Test]
        public void GuideStepsHistory_HistorySize_AddMoreThanSizeTest() {
            var historySize = 5;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.PIXELS, 4);

            IGuideStep step1 = new PhdEventGuideStep() {
                RADistanceRaw = -1,
                DECDistanceRaw = -1
            };

            IGuideStep step2 = new PhdEventGuideStep() {
                RADistanceRaw = -2,
                DECDistanceRaw = -2
            };

            IGuideStep step3 = new PhdEventGuideStep() {
                RADistanceRaw = -3,
                DECDistanceRaw = -3
            };

            IGuideStep step4 = new PhdEventGuideStep() {
                RADistanceRaw = -4,
                DECDistanceRaw = -4
            };

            IGuideStep step5 = new PhdEventGuideStep() {
                RADistanceRaw = -5,
                DECDistanceRaw = -5
            };

            IGuideStep step6 = new PhdEventGuideStep() {
                RADistanceRaw = -6,
                DECDistanceRaw = -6
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);
            gsh.AddGuideStep(step5);
            gsh.AddGuideStep(step6);

            Assert.That(gsh.GuideSteps.ElementAt(0).RADistanceRaw, Is.EqualTo(step2.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(1).RADistanceRaw, Is.EqualTo(step3.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(2).RADistanceRaw, Is.EqualTo(step4.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(3).RADistanceRaw, Is.EqualTo(step5.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(4).RADistanceRaw, Is.EqualTo(step6.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(0).DECDistanceRaw, Is.EqualTo(step2.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(1).DECDistanceRaw, Is.EqualTo(step3.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(2).DECDistanceRaw, Is.EqualTo(step4.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(3).DECDistanceRaw, Is.EqualTo(step5.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(4).DECDistanceRaw, Is.EqualTo(step6.DECDistanceRaw));
        }

        [Test]
        public void GuideStepsHistory_HistorySize_ResizeTest() {
            var historySize = 5;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.PIXELS, 4);

            IGuideStep step1 = new PhdEventGuideStep() {
                RADistanceRaw = -1,
                DECDistanceRaw = -1
            };

            IGuideStep step2 = new PhdEventGuideStep() {
                RADistanceRaw = -2,
                DECDistanceRaw = -2
            };

            IGuideStep step3 = new PhdEventGuideStep() {
                RADistanceRaw = -3,
                DECDistanceRaw = -3
            };

            IGuideStep step4 = new PhdEventGuideStep() {
                RADistanceRaw = -4,
                DECDistanceRaw = -4
            };

            IGuideStep step5 = new PhdEventGuideStep() {
                RADistanceRaw = -5,
                DECDistanceRaw = -5
            };

            IGuideStep step6 = new PhdEventGuideStep() {
                RADistanceRaw = -6,
                DECDistanceRaw = -6
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);
            gsh.AddGuideStep(step5);
            gsh.AddGuideStep(step6);

            gsh.HistorySize = 10;

            Assert.That(gsh.GuideSteps.ElementAt(0).RADistanceRaw, Is.EqualTo(step1.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(1).RADistanceRaw, Is.EqualTo(step2.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(2).RADistanceRaw, Is.EqualTo(step3.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(3).RADistanceRaw, Is.EqualTo(step4.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(4).RADistanceRaw, Is.EqualTo(step5.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(5).RADistanceRaw, Is.EqualTo(step6.RADistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(0).DECDistanceRaw, Is.EqualTo(step1.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(1).DECDistanceRaw, Is.EqualTo(step2.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(2).DECDistanceRaw, Is.EqualTo(step3.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(3).DECDistanceRaw, Is.EqualTo(step4.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(4).DECDistanceRaw, Is.EqualTo(step5.DECDistanceRaw));
            Assert.That(gsh.GuideSteps.ElementAt(5).DECDistanceRaw, Is.EqualTo(step6.DECDistanceRaw));
        }

        [Test]
        public void GuideStepsHistory_MaxDurationY_CalculateTest() {
            var historySize = 100;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.PIXELS, 4);

            IGuideStep step1 = new PhdEventGuideStep() {
                RADuration = -1,
                DECDuration = -1
            };

            IGuideStep step2 = new PhdEventGuideStep() {
                RADuration = -2,
                DECDuration = -2
            };

            IGuideStep step3 = new PhdEventGuideStep() {
                RADuration = -3,
                DECDuration = -3
            };

            IGuideStep step4 = new PhdEventGuideStep() {
                RADuration = -4,
                DECDuration = -4
            };

            IGuideStep step5 = new PhdEventGuideStep() {
                RADuration = -5,
                DECDuration = -5
            };

            IGuideStep step6 = new PhdEventGuideStep() {
                RADuration = -6,
                DECDuration = -6
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);
            gsh.AddGuideStep(step5);
            gsh.AddGuideStep(step6);

            Assert.That(gsh.MaxDurationY, Is.EqualTo(6));
            Assert.That(gsh.MinDurationY, Is.EqualTo(-6));
        }

        [Test]
        public void GuideStepsHistory_MaxDurationY_CalculateWhenMoreThanHistoryTest() {
            var historySize = 3;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.PIXELS, 4);

            IGuideStep step1 = new PhdEventGuideStep() {
                RADuration = -10,
                DECDuration = -10
            };

            IGuideStep step2 = new PhdEventGuideStep() {
                RADuration = -20,
                DECDuration = -20
            };

            IGuideStep step3 = new PhdEventGuideStep() {
                RADuration = -3,
                DECDuration = -3
            };

            IGuideStep step4 = new PhdEventGuideStep() {
                RADuration = -4,
                DECDuration = -4
            };

            IGuideStep step5 = new PhdEventGuideStep() {
                RADuration = -5,
                DECDuration = -5
            };

            IGuideStep step6 = new PhdEventGuideStep() {
                RADuration = -6,
                DECDuration = -6
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);
            gsh.AddGuideStep(step5);
            gsh.AddGuideStep(step6);

            Assert.That(gsh.MaxDurationY, Is.EqualTo(6));
            Assert.That(gsh.MinDurationY, Is.EqualTo(-6));
        }

        [Test]
        public void GuideStepsHistory_MaxDurationY_CalculateWhenResizedTest() {
            var historySize = 3;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.PIXELS, 4);

            IGuideStep step1 = new PhdEventGuideStep() {
                RADuration = -100,
                DECDuration = -100
            };

            IGuideStep step2 = new PhdEventGuideStep() {
                RADuration = -20,
                DECDuration = -20
            };

            IGuideStep step3 = new PhdEventGuideStep() {
                RADuration = -3,
                DECDuration = -3
            };

            IGuideStep step4 = new PhdEventGuideStep() {
                RADuration = -4,
                DECDuration = -4
            };

            IGuideStep step5 = new PhdEventGuideStep() {
                RADuration = -5,
                DECDuration = -5
            };

            IGuideStep step6 = new PhdEventGuideStep() {
                RADuration = -6,
                DECDuration = -6
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);
            gsh.AddGuideStep(step5);
            gsh.AddGuideStep(step6);

            gsh.HistorySize = 100;

            Assert.That(gsh.MaxDurationY, Is.EqualTo(100));
            Assert.That(gsh.MinDurationY, Is.EqualTo(-100));
        }
    }
}