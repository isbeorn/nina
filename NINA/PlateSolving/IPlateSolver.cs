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
                return Utility.Utility.AscomUtil.DegreesToHMS(RaError);
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