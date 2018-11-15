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

        public PlateSolveSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDesiralization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            blindSolverType = BlindSolverEnum.ASTROMETRY_NET;
            astrometryAPIKey = string.Empty;
            cygwinLocation = string.Empty;
            searchRadius = 30;
            pS2Location = string.Empty;
            regions = 5000;
            exposureTime = 2.0d;
            threshold = 1.0d;
            rotationTolerance = 1.0d;
            filter = null;
            aspsLocation = string.Empty;
        }

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

        private BlindSolverEnum blindSolverType;

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

        private string astrometryAPIKey;

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

        private string cygwinLocation;

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

        private double searchRadius;

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

        private string pS2Location;

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

        private int regions;

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

        private double exposureTime;

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

        private double threshold;

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

        private double rotationTolerance;

        [DataMember]
        public double RotationTolerance {
            get {
                return rotationTolerance;
            }
            set {
                rotationTolerance = value;
                RaisePropertyChanged();
            }
        }

        private FilterInfo filter;

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

        private string aspsLocation;

        [DataMember]
        public string AspsLocation {
            get {
                return Environment.ExpandEnvironmentVariables(aspsLocation);
            }
            set {
                aspsLocation = value;
                RaisePropertyChanged();
            }
        }
    }
}