using System;
using System.Runtime.Serialization;
using NINA.Model.MyCamera;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class FlatWizardSettings : Settings, IFlatWizardSettings {

        public FlatWizardSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDeserialization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            flatCount = 10;
            histogramTolerance = 0.1;
            histogramMeanTarget = 0.5;
            noFlatProcessing = false;
            stepSize = 0.5;
            binningMode = new BinningMode(1, 1);
        }

        private int flatCount;

        [DataMember]
        public int FlatCount {
            get {
                return flatCount;
            }
            set {
                flatCount = value;
                RaisePropertyChanged();
            }
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

        private bool noFlatProcessing;

        [DataMember]
        public bool NoFlatProcessing {
            get {
                return noFlatProcessing;
            }
            set {
                noFlatProcessing = value;
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

        private BinningMode binningMode;

        [DataMember]
        public BinningMode BinningMode {
            get {
                return binningMode;
            }
            set {
                binningMode = value;
                RaisePropertyChanged();
            }
        }
    }
}