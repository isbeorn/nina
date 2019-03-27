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

using NINA.Utility.Astrometry;
using System;

namespace NINA.PlateSolving {

    public class PlateSolveResult {

        public PlateSolveResult() {
            Success = true;
            SolveTime = DateTime.Now;
        }

        public DateTime SolveTime { get; private set; }

        public double Orientation { get; set; }

        public double Pixscale { get; set; }

        public double Radius { get; set; }

        public Coordinates Coordinates { get; set; }

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
    }
}