using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.PlateSolving {
    interface IPlateSolver {
        Task<PlateSolveResult> SolveAsync(MemoryStream image, IProgress<string> progress, CancellationToken canceltoken);
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
                return Utility.Utility.AscomUtil.DegreesToDMS(RaError);
            }
        }
    }
}
