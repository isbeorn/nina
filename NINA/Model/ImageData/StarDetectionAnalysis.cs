#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;

namespace NINA.Model.ImageData {

    public class StarDetectionAnalysis : BaseINPC, IStarDetectionAnalysis {
        private double _hfr = double.NaN;
        private int _detectedStars = -1;

        public double HFR {
            get {
                return this._hfr;
            }
            set {
                this._hfr = value;
                this.RaisePropertyChanged();
            }
        }

        public int DetectedStars {
            get {
                return this._detectedStars;
            }
            set {
                this._detectedStars = value;
                this.RaisePropertyChanged();
            }
        }

        public StarDetectionAnalysis() {
        }
    }
}