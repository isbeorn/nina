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
        public int Max { get; set; }
        public long MaxOccurrences { get; set; }
        public int Min { get; set; }
        public long MinOccurrences { get; set; }
        private double _hFR;

        public List<OxyPlot.DataPoint> Histogram { get; set; }

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
