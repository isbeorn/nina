using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {
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
