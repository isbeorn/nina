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

using NINA.Model;
using NINA.Utility.Astrometry;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal interface IPlateSolver {

        Task<PlateSolveResult> SolveAsync(MemoryStream image, IProgress<ApplicationStatus> progress, CancellationToken canceltoken);
    }

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

        public double RaError { get; set; }

        public double DecError { get; set; }

        public string RaErrorString {
            get {
                return Astrometry.DegreesToHMS(RaError);
            }
        }

        public double RaPixError {
            get {
                return Astrometry.DegreeToArcsec(RaError) / Pixscale;
            }
        }

        public double DecPixError {
            get {
                return Astrometry.DegreeToArcsec(DecError) / Pixscale;
            }
        }

        public string DecErrorString {
            get {
                return Astrometry.DegreesToDMS(RaError);
            }
        }
    }
}