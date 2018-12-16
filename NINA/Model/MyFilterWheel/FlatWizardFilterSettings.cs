using NINA.Utility;
using System;
using System.Runtime.Serialization;

namespace NINA.Model.MyFilterWheel {

    [Serializable]
    [DataContract]
    public class FlatWizardFilterSettings : BaseINPC {
        private double histogramMeanTarget;

        private double histogramTolerance;

        private double maxFlatExposureTime;

        private double minFlatExposureTime;

        private double stepSize;

        public FlatWizardFilterSettings() {
            HistogramMeanTarget = 0.5;
            HistogramTolerance = 0.1;
            StepSize = 0.1;
            MinFlatExposureTime = 0.01;
            MaxFlatExposureTime = 30;
        }

        [DataMember]
        public double HistogramMeanTarget {
            get {
                return histogramMeanTarget;
            }
            set {
                histogramMeanTarget = value;
                DelayedPropertyChanged();
            }
        }

        [DataMember]
        public double HistogramTolerance {
            get {
                return histogramTolerance;
            }
            set {
                histogramTolerance = value;
                DelayedPropertyChanged();
            }
        }

        [DataMember]
        public double MaxFlatExposureTime {
            get {
                return maxFlatExposureTime;
            }
            set {
                maxFlatExposureTime = value;
                DelayedPropertyChanged();
            }
        }

        [DataMember]
        public double MinFlatExposureTime {
            get {
                return minFlatExposureTime;
            }
            set {
                minFlatExposureTime = value;
                DelayedPropertyChanged();
            }
        }

        [DataMember]
        public double StepSize {
            get {
                return stepSize;
            }
            set {
                stepSize = value;
                DelayedPropertyChanged();
            }
        }
    }
}