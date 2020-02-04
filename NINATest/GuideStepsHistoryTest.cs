#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyGuider;
using NINA.Utility.Enum;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class GuideStepsHistoryTest {

        [Test]
        public void GuideStepsHistory_ConstructorTest() {
            var historySize = 100;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize);

            Assert.AreEqual(historySize, gsh.HistorySize);
            Assert.AreEqual(1, gsh.PixelScale);
            Assert.AreEqual(GuiderScaleEnum.PIXELS, gsh.Scale);
            Assert.AreEqual(0, gsh.GuideSteps.Count);
            Assert.AreEqual(1, gsh.RMS.Scale);
            Assert.AreEqual(0, gsh.RMS.RA);
            Assert.AreEqual(0, gsh.RMS.Dec);
            Assert.AreEqual(0, gsh.RMS.Total);
        }

        [Test]
        public void GuideStepsHistory_AddPHDDataPointsTest() {
            var historySize = 100;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize);

            IGuideStep step1 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);

            Assert.AreEqual(300, gsh.RMS.RA);
            Assert.AreEqual(630, gsh.RMS.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.AreEqual(total, gsh.RMS.Total);
        }

        [Test]
        public void GuideStepsHistory_AddPHDDataPointsScaledTest() {
            var historySize = 100;
            var scale = 1.59;

            GuideStepsHistory gsh = new GuideStepsHistory(historySize);
            gsh.PixelScale = scale;
            gsh.Scale = GuiderScaleEnum.ARCSECONDS;

            IGuideStep step1 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);

            Assert.AreEqual(300, gsh.RMS.RA);
            Assert.AreEqual(630, gsh.RMS.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.AreEqual(total, gsh.RMS.Total);
        }

        [Test]
        public void GuideStepsHistory_ClearTest() {
            var historySize = 100;
            var scale = 1.59;

            GuideStepsHistory gsh = new GuideStepsHistory(historySize);
            gsh.PixelScale = scale;
            gsh.Scale = GuiderScaleEnum.ARCSECONDS;

            IGuideStep step1 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -25,
                DECDistanceRaw = -36
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DECDistanceRaw = -1296
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);

            gsh.Clear();

            Assert.AreEqual(0, gsh.GuideSteps.Count);
            Assert.AreEqual(0, gsh.RMS.RA);
            Assert.AreEqual(0, gsh.RMS.Dec);
            Assert.AreEqual(0, gsh.RMS.Total);
        }

        public static List<IGuideStep> steps = new List<IGuideStep>();

        [Test]
        public void GuideStepsHistory_HistorySize_AddMoreThanSizeTest() {
            var historySize = 5;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize);

            IGuideStep step1 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -1,
                DECDistanceRaw = -1
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -2,
                DECDistanceRaw = -2
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -3,
                DECDistanceRaw = -3
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -4,
                DECDistanceRaw = -4
            };

            IGuideStep step5 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -5,
                DECDistanceRaw = -5
            };

            IGuideStep step6 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -6,
                DECDistanceRaw = -6
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);
            gsh.AddGuideStep(step5);
            gsh.AddGuideStep(step6);

            Assert.AreSame(step2, gsh.GuideSteps.ElementAt(0));
            Assert.AreSame(step3, gsh.GuideSteps.ElementAt(1));
            Assert.AreSame(step4, gsh.GuideSteps.ElementAt(2));
            Assert.AreSame(step5, gsh.GuideSteps.ElementAt(3));
            Assert.AreSame(step6, gsh.GuideSteps.ElementAt(4));
        }

        [Test]
        public void GuideStepsHistory_HistorySize_ResizeTest() {
            var historySize = 5;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize);

            IGuideStep step1 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -1,
                DECDistanceRaw = -1
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -2,
                DECDistanceRaw = -2
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -3,
                DECDistanceRaw = -3
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -4,
                DECDistanceRaw = -4
            };

            IGuideStep step5 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -5,
                DECDistanceRaw = -5
            };

            IGuideStep step6 = new PHD2Guider.PhdEventGuideStep() {
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

            Assert.AreSame(step1, gsh.GuideSteps.ElementAt(0));
            Assert.AreSame(step2, gsh.GuideSteps.ElementAt(1));
            Assert.AreSame(step3, gsh.GuideSteps.ElementAt(2));
            Assert.AreSame(step4, gsh.GuideSteps.ElementAt(3));
            Assert.AreSame(step5, gsh.GuideSteps.ElementAt(4));
            Assert.AreSame(step6, gsh.GuideSteps.ElementAt(5));
        }

        [Test]
        public void GuideStepsHistory_MaxDurationY_CalculateTest() {
            var historySize = 100;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize);

            IGuideStep step1 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -1,
                DECDuration = -1
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -2,
                DECDuration = -2
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -3,
                DECDuration = -3
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -4,
                DECDuration = -4
            };

            IGuideStep step5 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -5,
                DECDuration = -5
            };

            IGuideStep step6 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -6,
                DECDuration = -6
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);
            gsh.AddGuideStep(step5);
            gsh.AddGuideStep(step6);

            Assert.AreEqual(6, gsh.MaxDurationY);
            Assert.AreEqual(-6, gsh.MinDurationY);
        }

        [Test]
        public void GuideStepsHistory_MaxDurationY_CalculateWhenMoreThanHistoryTest() {
            var historySize = 3;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize);

            IGuideStep step1 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -10,
                DECDuration = -10
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -20,
                DECDuration = -20
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -3,
                DECDuration = -3
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -4,
                DECDuration = -4
            };

            IGuideStep step5 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -5,
                DECDuration = -5
            };

            IGuideStep step6 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -6,
                DECDuration = -6
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);
            gsh.AddGuideStep(step5);
            gsh.AddGuideStep(step6);

            Assert.AreEqual(6, gsh.MaxDurationY);
            Assert.AreEqual(-6, gsh.MinDurationY);
        }

        [Test]
        public void GuideStepsHistory_MaxDurationY_CalculateWhenResizedTest() {
            var historySize = 3;
            GuideStepsHistory gsh = new GuideStepsHistory(historySize);

            IGuideStep step1 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -100,
                DECDuration = -100
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -20,
                DECDuration = -20
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -3,
                DECDuration = -3
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -4,
                DECDuration = -4
            };

            IGuideStep step5 = new PHD2Guider.PhdEventGuideStep() {
                RADuration = -5,
                DECDuration = -5
            };

            IGuideStep step6 = new PHD2Guider.PhdEventGuideStep() {
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

            Assert.AreEqual(100, gsh.MaxDurationY);
            Assert.AreEqual(-100, gsh.MinDurationY);
        }
    }
}