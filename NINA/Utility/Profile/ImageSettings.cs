using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class ImageSettings : IImageSettings {
        private double autoStretchFactor = 0.2;

        [DataMember]
        public double AutoStretchFactor {
            get {
                return autoStretchFactor;
            }
            set {
                autoStretchFactor = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}