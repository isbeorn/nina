#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using System.Collections.Generic;

namespace NINA.Image.ImageData {

    public class StarDetectionAnalysis : BaseINPC, IStarDetectionAnalysis {
        private double _hfr = double.NaN;
        private double _fwhm = double.NaN;
        private double _eccentricity = double.NaN;
        private double _hfrStDev = double.NaN;
        private StarMeasurementUnit _hfrUnit = StarMeasurementUnit.Pixels;
        private StarMeasurementUnit _fwhmUnit = StarMeasurementUnit.Arcseconds;
        private StarMeasurementUnit _hfrStDevUnit = StarMeasurementUnit.Pixels;
        private int _detectedStars = -1;
        private List<DetectedStar> _starList = new List<DetectedStar>();

        public double HFR {
            get => this._hfr;
            set {
                this._hfr = value;
                this.RaisePropertyChanged();
            }
        }

        public double FWHM {
            get => this._fwhm;
            set {
                this._fwhm = value;
                this.RaisePropertyChanged();
            }
        }

        public double Eccentricity {
            get => this._eccentricity;
            set {
                this._eccentricity = value;
                this.RaisePropertyChanged();
            }
        }

        public double HFRStDev {
            get => this._hfrStDev;
            set {
                this._hfrStDev = value;
                this.RaisePropertyChanged();
            }
        }

        public StarMeasurementUnit HFRUnit {
            get => this._hfrUnit;
            set {
                this._hfrUnit = value;
                this.RaisePropertyChanged();
            }
        }

        public StarMeasurementUnit FWHMUnit {
            get => this._fwhmUnit;
            set {
                this._fwhmUnit = value;
                this.RaisePropertyChanged();
            }
        }

        public StarMeasurementUnit HFRStDevUnit {
            get => this._hfrStDevUnit;
            set {
                this._hfrStDevUnit = value;
                this.RaisePropertyChanged();
            }
        }

        public int DetectedStars {
            get => this._detectedStars;
            set {
                this._detectedStars = value;
                this.RaisePropertyChanged();
            }
        }

        public List<DetectedStar> StarList {
            get => this._starList;
            set {
                this._starList = value;
                this.RaisePropertyChanged();
            }
        }

        public StarDetectionAnalysis() {
        }
    }
}
