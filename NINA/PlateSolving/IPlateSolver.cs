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
        Task<PlateSolveResult> BlindSolve(MemoryStream image, IProgress<string> progress, CancellationTokenSource canceltoken);
    }

    

    public class PlateSolveResult {
        public PlateSolveResult() {
            Success = true;
            Epoch = Epoch.J2000;
        }

        BitmapSource _solvedImage;
        double _orientation;
        double _pixscale;
        double _radius;
        double _ra;
        double _dec;
        bool _success;
        Epoch _epoch;

        public double Orientation {
            get {
                return _orientation;
            }

            set {
                _orientation = value;
            }
        }

        public double Pixscale {
            get {
                return _pixscale;
            }

            set {
                _pixscale = value;
            }
        }

        public double Radius {
            get {
                return _radius;
            }

            set {
                _radius = value;
            }
        }

        public double Ra {
            get {
                return _ra;
            }

            set {
                _ra = value;
            }
        }

        public double Dec {
            get {
                return _dec;
            }

            set {
                _dec = value;
            }
        }

        public string RaString {
            get {
                return Utility.Utility.AscomUtil.DegreesToHMS(Ra);
            }
        }

        public string DecString {
            get {
                return Utility.Utility.AscomUtil.DegreesToDMS(Dec);
            }
        }

        public BitmapSource SolvedImage {
            get {
                return _solvedImage;
            }

            set {
                _solvedImage = value;
            }
        }

        public bool Success {
            get {
                return _success;
            }

            set {
                _success = value;
            }
        }

        public Epoch Epoch {
            get {
                return _epoch;
            }
            set {
                _epoch = value;
            }
        }

        public double RaError { get; set; }
        public string RaErrorString {
            get {
                return Utility.Utility.AscomUtil.DegreesToHMS(RaError);
            }
        }
        public double RaPixError {
            get {
                return (RaError * 60 * 60) / Pixscale;
            }
        }

        public double DecError { get; set; }
        public double DecPixError {
            get {
                return (DecError * 60 * 60) / Pixscale;
            }
        }
        public string DecErrorString {
            get {
                return Utility.Utility.AscomUtil.DegreesToDMS(RaError);
            }
        }
    }
}
