using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {
    interface ICamera : IDevice {
        
        bool HasShutter { get; }
        bool Connected { get; }
        double CCDTemperature { get; }
        double SetCCDTemperature { get; set; }
        short BinX { get; set; }
        short BinY { get; set; }
        string Name { get;  }
        string Description { get; }
        string DriverInfo { get; }
        string DriverVersion { get; }
        string SensorName { get; }
        ASCOM.DeviceInterface.SensorType SensorType { get; }
        int CameraXSize { get; }
        int CameraYSize { get; }
        double ExposureMin { get; }
        double ExposureMax { get; }
        short MaxBinX { get; }
        short MaxBinY { get; }
        double PixelSizeX { get; }
        double PixelSizeY { get; }
        bool CanSetCCDTemperature { get; }
        bool CoolerOn { get; set; }
        double CoolerPower { get; }
        string CameraState { get; }

        int Offset { get; set; }
        int USBLimit { get; set; }
        bool CanSetOffset { get; }
        bool CanSetUSBLimit { get; }
        bool CanGetGain { get; }
        bool CanSetGain { get; }
        short GainMax { get; }
        short GainMin { get; }
        short Gain { get; set; }

        ArrayList Gains { get; }

        AsyncObservableCollection<BinningMode> BinningModes { get; }

        bool Connect();
        void Disconnect();
        void UpdateValues();
        void SetBinning(short x, short y);
        void StartExposure(double exposureTime, bool isLightFrame);
        void StopExposure();
        void AbortExposure();
        
        Task<ImageArray> DownloadExposure(CancellationTokenSource tokenSource); 
    }

    public class ImageArray {
        public ushort[] FlatArray;
        public ImageStatistics Statistics { get; set; }        

        private ImageArray() {
            Statistics = new ImageStatistics { };
        }


        public static async Task<ImageArray> CreateInstance(Array input) {
            ImageArray imgArray = new ImageArray();

            await Task.Run(() => imgArray.FlipAndConvert(input));
            await Task.Run(() => imgArray.CalculateStatistics());

            return imgArray;
        }

        public static async Task<ImageArray> CreateInstance(ushort[] input, int width, int height) {
            ImageArray imgArray = new ImageArray();

            imgArray.FlatArray = input;
            imgArray.Statistics.Width = width;
            imgArray.Statistics.Height = height;
            await Task.Run(() => imgArray.CalculateStatistics());

            return imgArray;
        }

        private void CalculateStatistics() {

            /*Calculate StDev and Min/Max Values for Stretch */
            double average = this.FlatArray.Average(x => x);
            double sumOfSquaresOfDifferences = this.FlatArray.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / this.FlatArray.Length);

            this.Statistics.StDev = sd;
            this.Statistics.Mean = average;
            
            this.Statistics.Histogram = this.FlatArray.GroupBy(x => Math.Floor(x * ((double)ImageStatistics.HistogramResolution / ushort.MaxValue)))
                .Select(g => new {Key = g.Key, Value = g.Count()})
                .OrderBy(item => item.Key);
        }

        private void FlipAndConvert(Array input) {
            if(input.GetType() == typeof(Int32[,,])) {
                this.FlatArray = FlipAndConvert3d(input);
            } else {
                this.FlatArray = FlipAndConvert2d(input);
            }
        }

        private ushort[] FlipAndConvert2d(Array input) {
            Int32[,] arr = (Int32[,])input;
            int width = arr.GetLength(0);
            int height = arr.GetLength(1);

            this.Statistics.Width = width;
            this.Statistics.Height = height;
            ushort[] flatArray = new ushort[arr.Length];
            ushort value;

            unsafe
            {
                fixed (Int32* ptr = arr) {
                    int idx = 0, row = 0;
                    for (int i = 0; i < arr.Length; i++) {
                        value = (ushort)ptr[i];

                        idx = ((i % height) * width) + row;
                        if ((i % (height)) == (height - 1)) row++;

                        ushort b = value;
                        flatArray[idx] = b;
                    }
                }
            }
            return flatArray;
        }

        private ushort[] FlipAndConvert3d(Array input) {
            Notification.ShowError("Color sensor is not yet supported");
            throw new NotSupportedException();
        }
    }

   

    public class ImageStatistics : BaseINPC {
        public int Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double StDev { get; set; }
        public double Mean { get; set; }
        private double _hFR;

        public IEnumerable Histogram { get; set; }
        public static double HistogramMajorStep = 642.5;
        public static double HistogramMinorStep = 321.25;
        public static double HistogramResolution = 1285;

        public int DetectedStars {
            get {
                return _detectedStars;
            }

            set {
                _detectedStars = value;
                RaisePropertyChanged();
            }
        }

        public double HFR {
            get {
                return _hFR;
            }

            set {
                _hFR = value;
                RaisePropertyChanged();
            }
        }

        private int _detectedStars;

    }
}
