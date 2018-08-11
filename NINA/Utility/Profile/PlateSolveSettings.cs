using NINA.Model.MyFilterWheel;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class PlateSolveSettings : Settings, IPlateSolveSettings {
        private PlateSolverEnum plateSolverType = PlateSolverEnum.PLATESOLVE2;

        [DataMember]
        public PlateSolverEnum PlateSolverType {
            get {
                return plateSolverType;
            }
            set {
                plateSolverType = value;
                RaisePropertyChanged();
            }
        }

        private BlindSolverEnum blindSolverType = BlindSolverEnum.ASTROMETRY_NET;

        [DataMember]
        public BlindSolverEnum BlindSolverType {
            get {
                return blindSolverType;
            }
            set {
                blindSolverType = value;
                RaisePropertyChanged();
            }
        }

        private string astrometryAPIKey = string.Empty;

        [DataMember]
        public string AstrometryAPIKey {
            get {
                return astrometryAPIKey;
            }
            set {
                astrometryAPIKey = value;
                RaisePropertyChanged();
            }
        }

        private string cygwinLocation = @"%localappdata%\NINA\cygwin";

        [DataMember]
        public string CygwinLocation {
            get {
                return Environment.ExpandEnvironmentVariables(cygwinLocation);
            }
            set {
                cygwinLocation = value;
                RaisePropertyChanged();
            }
        }

        private double searchRadius = 30;

        [DataMember]
        public double SearchRadius {
            get {
                return searchRadius;
            }
            set {
                searchRadius = value;
                RaisePropertyChanged();
            }
        }

        private string pS2Location = string.Empty;

        [DataMember]
        public string PS2Location {
            get {
                return Environment.ExpandEnvironmentVariables(pS2Location);
            }
            set {
                pS2Location = value;
                RaisePropertyChanged();
            }
        }

        private int regions = 5000;

        [DataMember]
        public int Regions {
            get {
                return regions;
            }
            set {
                regions = value;
                RaisePropertyChanged();
            }
        }

        private double exposureTime = 2.0d;

        [DataMember]
        public double ExposureTime {
            get {
                return exposureTime;
            }
            set {
                exposureTime = value;
                RaisePropertyChanged();
            }
        }

        private double threshold = 1.0d;

        [DataMember]
        public double Threshold {
            get {
                return threshold;
            }
            set {
                threshold = value;
                RaisePropertyChanged();
            }
        }

        private FilterInfo filter = null;

        [DataMember]
        public FilterInfo Filter {
            get {
                return filter;
            }
            set {
                filter = value;
                RaisePropertyChanged();
            }
        }
    }
}