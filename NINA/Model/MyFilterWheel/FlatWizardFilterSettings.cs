using NINA.Utility;
using System;
using System.Runtime.Serialization;

namespace NINA.Model.MyFilterWheel {

    [Serializable]
    [DataContract]
    public class FlatWizardFilterSettings : BaseINPC {

        public FlatWizardFilterSettings() {
            HistogramMeanTarget = 0.5;
            HistogramTolerance = 0.1;
            StepSize = 0.1;
            MinFlatExposureTime = 0.01;
            MaxFlatExposureTime = 30;
            BinningMode = new MyCamera.BinningMode(1, 1);
        }

        private double histogramMeanTarget;

        [DataMember]
        public double HistogramMeanTarget {
            get {
                return histogramMeanTarget;
            }
            set {
                histogramMeanTarget = value;
                RaisePropertyChanged();
            }
        }

        private double histogramTolerance;

        [DataMember]
        public double HistogramTolerance {
            get {
                return histogramTolerance;
            }
            set {
                histogramTolerance = value;
                RaisePropertyChanged();
            }
        }

        private double stepSize;

        [DataMember]
        public double StepSize {
            get {
                return stepSize;
            }
            set {
                stepSize = value;
                RaisePropertyChanged();
            }
        }

        private MyCamera.BinningMode binningMode;

        [DataMember]
        public MyCamera.BinningMode BinningMode {
            get {
                return binningMode;
            }
            set {
                binningMode = value;
                RaisePropertyChanged();
            }
        }

        private double minFlatExposureTime;

        [DataMember]
        public double MinFlatExposureTime {
            get {
                return minFlatExposureTime;
            }
            set {
                minFlatExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private double maxFlatExposureTime;

        [DataMember]
        public double MaxFlatExposureTime {
            get {
                return maxFlatExposureTime;
            }
            set {
                maxFlatExposureTime = value;
                RaisePropertyChanged();
            }
        }
    }
}