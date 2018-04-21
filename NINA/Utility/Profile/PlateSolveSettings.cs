using NINA.Utility.Enum;
using NINA.Utility.Mediator;
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

    }
}
