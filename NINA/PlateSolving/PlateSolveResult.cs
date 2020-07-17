#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using System;

namespace NINA.PlateSolving {

    public class PlateSolveResult {

        public PlateSolveResult() {
            Success = true;
            SolveTime = DateTime.Now;
        }

        public DateTime SolveTime { get; private set; }

        private double _orientation;

        public double Orientation {
            get => _orientation;
            set {
                _orientation = Astrometry.EuclidianModulus(value, 360);
            }
        }

        public double Pixscale { get; set; }

        public double Radius { get; set; }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                coordinates = value?.Transform(Epoch.J2000);
            }
        }

        public bool Success { get; set; }

        public Separation Separation { get; set; }

        public string RaErrorString {
            get {
                return Astrometry.DegreesToHMS(Separation?.RA.Degree ?? 0);
            }
        }

        public double RaPixError {
            get {
                return Separation?.RA.ArcSeconds / Pixscale ?? 0;
            }
        }

        public double DecPixError {
            get {
                return Separation?.Dec.ArcSeconds / Pixscale ?? 0;
            }
        }

        public string DecErrorString {
            get {
                return Astrometry.DegreesToDMS(Separation?.Dec.Degree ?? 0);
            }
        }

        public Separation DetermineSeparation(Coordinates targetCoordinates) {
            if (targetCoordinates != null) {
                return targetCoordinates - this.Coordinates;
            }
            return null;
        }
    }
}