using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(Profile))]
    public class ImageSettings {

        private double autoStretchFactor = 0.2;
        [XmlElement(nameof(AutoStretchFactor))]
        public double AutoStretchFactor {
            get {
                return autoStretchFactor;
            }
            set {
                autoStretchFactor = value;
            }
        }

        private int histogramResolution = 300;
        [XmlElement(nameof(HistogramResolution))]
        public int HistogramResolution {
            get {
                return histogramResolution;
            }
            set {
                histogramResolution = value;
            }
        }

        private bool annotateImage = false;
        [XmlElement(nameof(AnnotateImage))]
        public bool AnnotateImage {
            get {
                return annotateImage;
            }
            set {
                annotateImage = value;
            }
        }
    }
}
