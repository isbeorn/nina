#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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