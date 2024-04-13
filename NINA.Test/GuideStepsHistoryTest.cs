#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

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

            ClassicAssert.AreEqual(historySize, gsh.HistorySize);
            ClassicAssert.AreEqual(1, gsh.PixelScale);
            ClassicAssert.AreEqual(GuiderScaleEnum.PIXELS, gsh.Scale);
            ClassicAssert.AreEqual(0, gsh.GuideSteps.Count);
            ClassicAssert.AreEqual(1, gsh.RMS.Scale);
            ClassicAssert.AreEqual(0, gsh.RMS.RA);
            ClassicAssert.AreEqual(0, gsh.RMS.Dec);
            ClassicAssert.AreEqual(0, gsh.RMS.Total);
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

            ClassicAssert.AreEqual(300, gsh.RMS.RA);
            ClassicAssert.AreEqual(630, gsh.RMS.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            ClassicAssert.AreEqual(total, gsh.RMS.Total);
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

            ClassicAssert.AreEqual(300, gsh.RMS.RA);
            ClassicAssert.AreEqual(630, gsh.RMS.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            ClassicAssert.AreEqual(total, gsh.RMS.Total);
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

            ClassicAssert.AreEqual(0, gsh.GuideSteps.Count);
            ClassicAssert.AreEqual(0, gsh.RMS.RA);
            ClassicAssert.AreEqual(0, gsh.RMS.Dec);
            ClassicAssert.AreEqual(0, gsh.RMS.Total);
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

            ClassicAssert.AreEqual(step2.RADistanceRaw, gsh.GuideSteps.ElementAt(0).RADistanceRaw);
            ClassicAssert.AreEqual(step3.RADistanceRaw, gsh.GuideSteps.ElementAt(1).RADistanceRaw);
            ClassicAssert.AreEqual(step4.RADistanceRaw, gsh.GuideSteps.ElementAt(2).RADistanceRaw);
            ClassicAssert.AreEqual(step5.RADistanceRaw, gsh.GuideSteps.ElementAt(3).RADistanceRaw);
            ClassicAssert.AreEqual(step6.RADistanceRaw, gsh.GuideSteps.ElementAt(4).RADistanceRaw);
            ClassicAssert.AreEqual(step2.DECDistanceRaw, gsh.GuideSteps.ElementAt(0).DECDistanceRaw);
            ClassicAssert.AreEqual(step3.DECDistanceRaw, gsh.GuideSteps.ElementAt(1).DECDistanceRaw);
            ClassicAssert.AreEqual(step4.DECDistanceRaw, gsh.GuideSteps.ElementAt(2).DECDistanceRaw);
            ClassicAssert.AreEqual(step5.DECDistanceRaw, gsh.GuideSteps.ElementAt(3).DECDistanceRaw);
            ClassicAssert.AreEqual(step6.DECDistanceRaw, gsh.GuideSteps.ElementAt(4).DECDistanceRaw);
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

            ClassicAssert.AreEqual(step1.RADistanceRaw, gsh.GuideSteps.ElementAt(0).RADistanceRaw);
            ClassicAssert.AreEqual(step2.RADistanceRaw, gsh.GuideSteps.ElementAt(1).RADistanceRaw);
            ClassicAssert.AreEqual(step3.RADistanceRaw, gsh.GuideSteps.ElementAt(2).RADistanceRaw);
            ClassicAssert.AreEqual(step4.RADistanceRaw, gsh.GuideSteps.ElementAt(3).RADistanceRaw);
            ClassicAssert.AreEqual(step5.RADistanceRaw, gsh.GuideSteps.ElementAt(4).RADistanceRaw);
            ClassicAssert.AreEqual(step6.RADistanceRaw, gsh.GuideSteps.ElementAt(5).RADistanceRaw);
            ClassicAssert.AreEqual(step1.DECDistanceRaw, gsh.GuideSteps.ElementAt(0).DECDistanceRaw);
            ClassicAssert.AreEqual(step2.DECDistanceRaw, gsh.GuideSteps.ElementAt(1).DECDistanceRaw);
            ClassicAssert.AreEqual(step3.DECDistanceRaw, gsh.GuideSteps.ElementAt(2).DECDistanceRaw);
            ClassicAssert.AreEqual(step4.DECDistanceRaw, gsh.GuideSteps.ElementAt(3).DECDistanceRaw);
            ClassicAssert.AreEqual(step5.DECDistanceRaw, gsh.GuideSteps.ElementAt(4).DECDistanceRaw);
            ClassicAssert.AreEqual(step6.DECDistanceRaw, gsh.GuideSteps.ElementAt(5).DECDistanceRaw);
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

            ClassicAssert.AreEqual(6, gsh.MaxDurationY);
            ClassicAssert.AreEqual(-6, gsh.MinDurationY);
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

            ClassicAssert.AreEqual(6, gsh.MaxDurationY);
            ClassicAssert.AreEqual(-6, gsh.MinDurationY);
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

            ClassicAssert.AreEqual(100, gsh.MaxDurationY);
            ClassicAssert.AreEqual(-100, gsh.MinDurationY);
        }

        [Test]
        //[TestCase(5, new int[] { 100, 1000, 100, 1000, 100, 2, 2, 2, 2, 2, 2 }, 1, 0)]
        [TestCase(5, new int[] { 100, 1000, 100, 1000, 100, 5, 1, 6, 1, 2, 1 }, 2, 1.9390)]
        public void ScaleChange(int historySize, int[] input, double arcsecPerPix, double expected) {
            GuideStepsHistory gsh = new GuideStepsHistory(historySize, GuiderScaleEnum.PIXELS, 4);
            gsh.PixelScale = arcsecPerPix;
            foreach (var val in input) {
                var step = new PhdEventGuideStep() {
                    RADistanceRaw = val
                };
                gsh.AddGuideStep(step);
            }

            gsh.RMS.Total.Should().BeApproximately(expected, 0.0001);
            gsh.Scale = GuiderScaleEnum.ARCSECONDS;
            gsh.RMS.Total.Should().BeApproximately(expected, 0.0001);
            gsh.RMS.TotalText.Should().Be($"Tot: {Math.Round(expected, 2):0.00} ({Math.Round(expected * arcsecPerPix, 2):0.00}\")");
            gsh.Scale = GuiderScaleEnum.PIXELS;
            gsh.RMS.Total.Should().BeApproximately(expected, 0.0001);
        }
    }
}