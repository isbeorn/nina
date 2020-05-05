using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Accord.Statistics.Models.Regression.Linear;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NINA.Utility.ImageAnalysis {

    internal class OffsetInfo {

        [JsonProperty(PropertyName = "OffsetValue")]
        public double OffsetValue;

        [JsonProperty(PropertyName = "MeanADU")]
        public double MeanADU;

        [JsonProperty(PropertyName = "MedianADU")]
        public double MedianADU;

        [JsonProperty(PropertyName = "SigmaADU")]
        public double SigmaADU;
    }

    internal class GainInfo {
        [JsonProperty(PropertyName = "OffsetInfo")]
        public OffsetInfo[] OffsetInfos;

        [JsonProperty(PropertyName = "BlackLevelSigma")]
        public double BlackLevelSigma;

        [JsonProperty(PropertyName = "GainValue")]
        public double GainValue;

        [JsonProperty(PropertyName = "RelativeGain")]
        public double RelativeGain;

        [JsonProperty(PropertyName = "ReadNoise")]
        public double ReadNoise;

        [JsonProperty(PropertyName = "EPerADU")]
        public double EPerADU;
    }

    internal class SensorInfo {
        [JsonProperty(PropertyName = "GainList")]
        public GainInfo[] GainInfos;

        [JsonProperty(PropertyName = "BitDepth")]
        public int BitDepth;

        [JsonProperty(PropertyName = "IsAdditiveBinning")]
        public bool IsAdditiveBinning;

        [JsonProperty(PropertyName = "LinearityLimit")]
        public double LinearityLimit;

        [JsonProperty(PropertyName = "CameraType")]
        public string CameraType;

        [JsonProperty(PropertyName = "IsMono")]
        public bool IsMono;

        [JsonProperty(PropertyName = "IsRaw")]
        public bool IsRaw;

        [JsonProperty(PropertyName = "SensorName")]
        public string SensorName;

        [JsonProperty(PropertyName = "ColourSpace")]
        public string ColourSpace;
    }

    public class SharpCapSensorAnalysisGainData {
        public SharpCapSensorAnalysisGainData(double gain, double readNoise, int fullWellCapacity) {
            this.Gain = gain;
            this.ReadNoise = readNoise;
            this.FullWellCapacity = fullWellCapacity;
        }

        public double Gain { get; private set; }

        public double ReadNoise { get; private set; }

        public int FullWellCapacity { get; private set; }

        internal static SharpCapSensorAnalysisGainData FromGainInfo(SensorInfo sensorInfo, GainInfo gainInfo) {
            var maxAdu = 1 << sensorInfo.BitDepth;
            var fullWellCapacity = (int)Math.Round(gainInfo.EPerADU * maxAdu);
            return new SharpCapSensorAnalysisGainData(gain: gainInfo.GainValue, readNoise: gainInfo.ReadNoise, fullWellCapacity: fullWellCapacity);
        }
    }

    public class Estimate {
        public double Gain { get; private set; }
        public double EstimatedValue { get; private set; }
        public double RSquared { get; private set; }
        public Estimate(double gain, double estimatedValue, double rSquared) {
            this.Gain = gain;
            this.EstimatedValue = estimatedValue;
            this.RSquared = rSquared;
        }
    }

    public class SharpCapSensorAnalysisData {
        public SharpCapSensorAnalysisData(string sensorName, string colourSpace, int bitDepth, IList<SharpCapSensorAnalysisGainData> gainData) {
            this.SensorName = sensorName;
            this.ColourSpace = colourSpace;
            this.BitDepth = bitDepth;
            this.GainData = gainData.ToImmutableList();
        }

        public ImmutableList<SharpCapSensorAnalysisGainData> GainData { get; private set; }

        public string SensorName { get; private set; }

        public string ColourSpace { get; private set; }

        public int BitDepth { get; private set; }

        public static SharpCapSensorAnalysisData ParseFromPath(string path) {
            using (StreamReader file = File.OpenText(path)) {
                using (JsonTextReader reader = new JsonTextReader(file)) {
                    JObject jobj = (JObject)JToken.ReadFrom(reader);
                    var sensorInfo = jobj.ToObject<SensorInfo>();
                    var gainDataListBuilder = ImmutableList.CreateBuilder<SharpCapSensorAnalysisGainData>();
                    foreach (var gainInfo in sensorInfo.GainInfos) {
                        gainDataListBuilder.Add(SharpCapSensorAnalysisGainData.FromGainInfo(sensorInfo, gainInfo));
                    }
                    return new SharpCapSensorAnalysisData(
                        sensorName: sensorInfo.SensorName, 
                        colourSpace: sensorInfo.ColourSpace, 
                        bitDepth: sensorInfo.BitDepth,
                        gainData: gainDataListBuilder.ToImmutable());
                }
            }
        }

        private Estimate CalculateEstimate(double gain, Func<SharpCapSensorAnalysisGainData, double> valueFunc) {
            // Gain is hyperbolically correlated with the response variables, so use 1 / gain as the independent variable for linear regression
            double[] xData = this.GainData.Select(x => 1 / x.Gain).ToArray();
            double[] yData = this.GainData.Select(valueFunc).ToArray();

            var linearRegression = SimpleLinearRegression.FromData(xData, yData);
            double b = linearRegression.Intercept;
            double m = linearRegression.Slope;

            // We include the goodness of fit measure so callers can choose whether to discard the estimate if the data aren't well correlated
            double rSquared = linearRegression.CoefficientOfDetermination(xData, yData);
            double estimatedValue = m * (1 / gain) + b;
            return new Estimate(gain: gain, estimatedValue: estimatedValue, rSquared: rSquared);
        }

        public Estimate EstimateReadNoise(double gain) {
            return CalculateEstimate(gain, x => x.ReadNoise);
        }

        public Estimate EstimateFullWellCapacity(double gain) {
            return CalculateEstimate(gain, x => x.FullWellCapacity);
        }
    }

    public sealed class SharpCapSensorAnalysisConstants {
        public static readonly string DEFAULT_SHARPCAP_SENSOR_ANALYSIS_PATH = Environment.ExpandEnvironmentVariables(@"%APPDATA%\SharpCap\SensorCharacteristics");
    }

    public interface ISharpCapSensorAnalysisReader {        

        ImmutableDictionary<string, SharpCapSensorAnalysisData> Read(string sensorAnalysisPath);
    }

    public sealed class DefaultSharpCapSensorAnalysisReader : ISharpCapSensorAnalysisReader {

        public ImmutableDictionary<string, SharpCapSensorAnalysisData> Read(string sensorAnalysisPath) {
            var sensorAnalysisDataByNameBuilder = ImmutableDictionary.CreateBuilder<String, SharpCapSensorAnalysisData>();
            foreach (var filePath in Directory.GetFiles(sensorAnalysisPath, "*.json")) {
                try {
                    var analysisData = SharpCapSensorAnalysisData.ParseFromPath(filePath);
                    var sensorName = String.Format("{0} - {1}", analysisData.SensorName, analysisData.ColourSpace);
                    if (sensorAnalysisDataByNameBuilder.ContainsKey(sensorName)) {
                        Logger.Warning($"Sensor name {sensorName} encountered multiple times in {sensorAnalysisPath}");
                        continue;
                    }

                    sensorAnalysisDataByNameBuilder.Add(sensorName, analysisData);
                } catch (Exception ex) {
                    Logger.Error(ex, $"Failed parsing sensor analysis data from {filePath}. Ignoring and continuing...");
                }
            }
            return sensorAnalysisDataByNameBuilder.ToImmutable();
        }
    }
}
