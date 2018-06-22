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
                DecDistanceRaw = -36
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DecDistanceRaw = -1296
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -25,
                DecDistanceRaw = -36
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DecDistanceRaw = -1296
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
                DecDistanceRaw = -36
            };

            IGuideStep step2 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DecDistanceRaw = -1296
            };

            IGuideStep step3 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -25,
                DecDistanceRaw = -36
            };

            IGuideStep step4 = new PHD2Guider.PhdEventGuideStep() {
                RADistanceRaw = -625,
                DecDistanceRaw = -1296
            };

            gsh.AddGuideStep(step1);
            gsh.AddGuideStep(step2);
            gsh.AddGuideStep(step3);
            gsh.AddGuideStep(step4);

            Assert.AreEqual(300 * scale, gsh.RMS.RA);
            Assert.AreEqual(630 * scale, gsh.RMS.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2))) * scale;
            Assert.AreEqual(total, gsh.RMS.Total);
        }
    }
}