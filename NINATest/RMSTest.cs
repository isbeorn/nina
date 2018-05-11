using Microsoft.VisualStudio.TestTools.UnitTesting;
using NINA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {
    [TestClass]
    public class RMSTest {

        [TestMethod]
        public void RMS_DefaultConstructorTest() {
            RMS rms = new RMS();

            Assert.AreEqual(1, rms.Scale);
            Assert.AreEqual(0, rms.RA);
            Assert.AreEqual(0, rms.Dec);
            Assert.AreEqual(0, rms.Total);
        }

        [TestMethod]
        public void RMS_AddSingleValue_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(10, 10);

            Assert.AreEqual(0, rms.RA);
            Assert.AreEqual(0, rms.Dec);
            Assert.AreEqual(0, rms.Total);
        }

        [TestMethod]
        public void RMS_AddMultipleDataPoints_CalculateCorrect() {
            RMS rms = new RMS();
            
            rms.AddDataPoint(25, 1296);
            rms.AddDataPoint(625, 36);
            rms.AddDataPoint(25, 1296);
            rms.AddDataPoint(625, 36);

            Assert.AreEqual(300, rms.RA);
            Assert.AreEqual(630, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.AreEqual(total, rms.Total);
        }

        [TestMethod]
        public void RMS_AddMultipleDataPoints2_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            Assert.AreEqual(300, rms.RA);
            Assert.AreEqual(630, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.AreEqual(total, rms.Total);
        }

        [TestMethod]
        public void RMS_AddMultipleDataPointsAndSetScale_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            var scale = 1.59;
            rms.SetScale(scale);

            Assert.AreEqual(300 * scale, rms.RA);
            Assert.AreEqual(630 * scale, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2))) * scale;
            Assert.AreEqual(total, rms.Total);
        }

        [TestMethod]
        public void RMS_AddValuesAndClear_AllResetExceptScale() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            var scale = 1.59;
            rms.SetScale(scale);

            rms.Clear();

            Assert.AreEqual(scale, rms.Scale);
            Assert.AreEqual(0, rms.RA);
            Assert.AreEqual(0, rms.Dec);
            Assert.AreEqual(0, rms.Total);
        }
    }
}
