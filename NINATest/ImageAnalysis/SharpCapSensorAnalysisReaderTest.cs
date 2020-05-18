using System;
using System.IO;
using NINA.Utility.Astrometry;
using NINA.Utility.ImageAnalysis;
using NUnit.Framework;

namespace NINATest.ImageAnalysis {
    [TestFixture]
    public class SharpCapSensorAnalysisReaderTest {
        private string sensorAnalysisTestDataPath;
        private ISharpCapSensorAnalysisReader reader;
        private static string AA_SENSOR = "AA1600MPROTEC - MONO12";
        private static string ZWO_SENSOR = "ZWO ASI1600MM Pro - MONO16";

        [SetUp]
        public void TestSetup() {
            this.sensorAnalysisTestDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"ImageAnalysis\TestData");
            this.reader = new DefaultSharpCapSensorAnalysisReader();
        }

        [Test]
        public void ReadAllSensors_Test() {
            var sensorAnalysisData = this.reader.Read(this.sensorAnalysisTestDataPath);
            Assert.AreEqual(2, sensorAnalysisData.Count);
            Assert.IsTrue(sensorAnalysisData.ContainsKey(AA_SENSOR));
            Assert.IsTrue(sensorAnalysisData.ContainsKey(ZWO_SENSOR));
        }

        [Test]
        public void MinGain_Test() {
            var sensorAnalysisData = this.reader.Read(this.sensorAnalysisTestDataPath);
            Assert.AreEqual(2, sensorAnalysisData.Count);
            Assert.AreEqual(19346, sensorAnalysisData[AA_SENSOR].EstimateFullWellCapacity(100).EstimatedValue);
            Assert.AreEqual(20952, sensorAnalysisData[ZWO_SENSOR].EstimateFullWellCapacity(0).EstimatedValue);
            Assert.AreEqual(3.25515866, sensorAnalysisData[AA_SENSOR].EstimateReadNoise(100).EstimatedValue, 0.001);
            Assert.AreEqual(3.510164, sensorAnalysisData[ZWO_SENSOR].EstimateReadNoise(0).EstimatedValue, 0.001);
        }

        [Test]
        public void MaxGain_Test() {
            var sensorAnalysisData = this.reader.Read(this.sensorAnalysisTestDataPath);
            Assert.AreEqual(1152, sensorAnalysisData[AA_SENSOR].EstimateFullWellCapacity(2000).EstimatedValue);
            Assert.AreEqual(64, sensorAnalysisData[ZWO_SENSOR].EstimateFullWellCapacity(500).EstimatedValue);
            Assert.AreEqual(1.44073522, sensorAnalysisData[AA_SENSOR].EstimateReadNoise(2000).EstimatedValue, 0.001);
            Assert.AreEqual(0.901244342, sensorAnalysisData[ZWO_SENSOR].EstimateReadNoise(500).EstimatedValue, 0.001);
        }

        [Test]
        public void InBetweenGain_Test() {
            var sensorAnalysisData = this.reader.Read(this.sensorAnalysisTestDataPath);
            Assert.AreEqual(11518.5, sensorAnalysisData[AA_SENSOR].EstimateFullWellCapacity(174).EstimatedValue, 0.001);
            Assert.AreEqual(10607.5, sensorAnalysisData[ZWO_SENSOR].EstimateFullWellCapacity(60).EstimatedValue, 0.001);
            Assert.AreEqual(2.563265, sensorAnalysisData[AA_SENSOR].EstimateReadNoise(174).EstimatedValue, 0.001);
            Assert.AreEqual(2.396112, sensorAnalysisData[ZWO_SENSOR].EstimateReadNoise(60).EstimatedValue, 0.001);
        }
    }
}
