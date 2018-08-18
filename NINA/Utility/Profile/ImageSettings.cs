using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class ImageSettings : Settings, IImageSettings {
        private double autoStretchFactor = 0.2;

        [DataMember]
        public double AutoStretchFactor {
            get {
                return autoStretchFactor;
            }
            set {
                autoStretchFactor = value;
                RaisePropertyChanged();
            }
        }

        private int histogramResolution = 300;

        [DataMember]
        public int HistogramResolution {
            get {
                return histogramResolution;
            }
            set {
                histogramResolution = value;
                RaisePropertyChanged();
            }
        }

        private bool annotateImage = false;

        [DataMember]
        public bool AnnotateImage {
            get {
                return annotateImage;
            }
            set {
                annotateImage = value;
                RaisePropertyChanged();
            }
        }

        private bool debayerImage = false;

        [DataMember]
        public bool DebayerImage {
            get {
                return debayerImage;
            }
            set {
                debayerImage = value;
                RaisePropertyChanged();
            }
        }
    }
}