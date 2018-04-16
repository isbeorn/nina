using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(Profile))]
    public class PlateSolveSettings {

        private PlateSolverEnum plateSolverType = PlateSolverEnum.PLATESOLVE2;
        [XmlElement(nameof(PlateSolverType))]
        public PlateSolverEnum PlateSolverType {
            get {
                return plateSolverType;
            }
            set {
                plateSolverType = value;
            }
        }

        private BlindSolverEnum blindSolverType = BlindSolverEnum.ASTROMETRY_NET;
        [XmlElement(nameof(BlindSolverType))]
        public BlindSolverEnum BlindSolverType {
            get {
                return blindSolverType;
            }
            set {
                blindSolverType = value;
            }
        }

        private string astrometryAPIKey = string.Empty;
        [XmlElement(nameof(AstrometryAPIKey))]
        public string AstrometryAPIKey {
            get {
                return astrometryAPIKey;
            }
            set {
                astrometryAPIKey = value;
            }
        }

        private string cygwinLocation = @"%localappdata%\NINA\cygwin";
        [XmlElement(nameof(CygwinLocation))]
        public string CygwinLocation {
            get {
                return Environment.ExpandEnvironmentVariables(cygwinLocation);
            }
            set {
                cygwinLocation = value;
            }
        }

        private double searchRadius = 30;
        [XmlElement(nameof(SearchRadius))]
        public double SearchRadius {
            get {
                return searchRadius;
            }
            set {
                searchRadius = value;
            }
        }

        private string pS2Location = string.Empty;
        [XmlElement(nameof(PS2Location))]
        public string PS2Location {
            get {
                return Environment.ExpandEnvironmentVariables(pS2Location);
            }
            set {
                pS2Location = value;
            }
        }

        private int regions = 5000;
        [XmlElement(nameof(Regions))]
        public int Regions {
            get {
                return regions;
            }
            set {
                regions = value;
            }
        }

    }
}
