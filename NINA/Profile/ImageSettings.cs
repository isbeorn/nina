#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class ImageSettings : Settings, IImageSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            autoStretchFactor = 0.2;
            blackClipping = -2.8;
            histogramResolution = 300;
            annotateImage = false;
            debayerImage = true;
            unlinkedStretch = true;
        }

        private double autoStretchFactor;

        [DataMember]
        public double AutoStretchFactor {
            get {
                return autoStretchFactor;
            }
            set {
                if (autoStretchFactor != value) {
                    autoStretchFactor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double blackClipping;

        [DataMember]
        public double BlackClipping {
            get {
                return blackClipping;
            }
            set {
                if (blackClipping != value) {
                    blackClipping = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int histogramResolution;

        [DataMember]
        public int HistogramResolution {
            get {
                return histogramResolution;
            }
            set {
                if (histogramResolution != value) {
                    histogramResolution = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool annotateImage;

        [DataMember]
        public bool AnnotateImage {
            get {
                return annotateImage;
            }
            set {
                if (annotateImage != value) {
                    annotateImage = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool debayerImage;

        [DataMember]
        public bool DebayerImage {
            get {
                return debayerImage;
            }
            set {
                if (debayerImage != value) {
                    debayerImage = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool unlinkedStretch;

        [DataMember]
        public bool UnlinkedStretch {
            get {
                return unlinkedStretch;
            }
            set {
                if (unlinkedStretch != value) {
                    unlinkedStretch = value;
                    if (unlinkedStretch) {
                        DebayerImage = unlinkedStretch;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public double HistogramMajorStep {
            get {
                return HistogramResolution / 2;
            }
        }

        public double HistogramMinorStep {
            get {
                return HistogramResolution / 4;
            }
        }
    }
}