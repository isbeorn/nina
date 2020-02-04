#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model.MyFilterWheel;
using NINA.Utility.Enum;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class PlateSolveSettings : Settings, IPlateSolveSettings {
        private PlateSolverEnum plateSolverType = PlateSolverEnum.PLATESOLVE2;

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            blindSolverType = BlindSolverEnum.ASTROMETRY_NET;
            astrometryURL = "http://nova.astrometry.net";
            astrometryAPIKey = string.Empty;
            cygwinLocation = string.Empty;
            searchRadius = 30;
            pS2Location = string.Empty;
            regions = 5000;
            exposureTime = 2.0d;
            threshold = 1.0d;
            rotationTolerance = 1.0d;
            reattemptDelay = 10;
            numberOfAttempts = 1;
            filter = null;
            downSampleFactor = 2;
            maxObjects = 500;
            gain = -1;
            binning = 1;
            sync = false;

            var defaultASPSLocation = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\PlateSolver\PlateSolver.exe");
            aspsLocation =
                File.Exists(defaultASPSLocation)
                ? defaultASPSLocation
                : string.Empty;

            var defaultASTAPLocation = Environment.ExpandEnvironmentVariables(@"%programfiles%\astap\astap.exe"); aspsLocation =
            aSTAPLocation = File.Exists(defaultASTAPLocation)
                 ? defaultASTAPLocation
                 : string.Empty;
        }

        [DataMember]
        public PlateSolverEnum PlateSolverType {
            get {
                return plateSolverType;
            }
            set {
                if (plateSolverType != value) {
                    plateSolverType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private BlindSolverEnum blindSolverType;

        [DataMember]
        public BlindSolverEnum BlindSolverType {
            get {
                return blindSolverType;
            }
            set {
                if (blindSolverType != value) {
                    blindSolverType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string astrometryURL;

        [DataMember]
        public string AstrometryURL {
            get {
                return astrometryURL;
            }
            set {
                // Clear out any whitespace characters in the URL
                string url = Regex.Replace(value, @"\s", string.Empty);

                if (astrometryURL != url) {
                    astrometryURL = url;
                    RaisePropertyChanged();
                }
            }
        }

        private string astrometryAPIKey;

        [DataMember]
        public string AstrometryAPIKey {
            get {
                return astrometryAPIKey;
            }
            set {
                // Whitespace characters are not valid characaters in an Astrometry.net API key.
                // Help the user by removing any that might be present. Copy and pasting from the astrometry.net API page
                // can sometimes insert a space at the end of the API key string, and it's not very obvious.
                string key = Regex.Replace(value, @"\s", string.Empty);

                if (astrometryAPIKey != key) {
                    astrometryAPIKey = key;
                    RaisePropertyChanged();
                }
            }
        }

        private string cygwinLocation;

        [DataMember]
        public string CygwinLocation {
            get {
                return Environment.ExpandEnvironmentVariables(cygwinLocation);
            }
            set {
                if (cygwinLocation != value) {
                    cygwinLocation = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double searchRadius;

        [DataMember]
        public double SearchRadius {
            get {
                return searchRadius;
            }
            set {
                if (searchRadius != value) {
                    searchRadius = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string pS2Location;

        [DataMember]
        public string PS2Location {
            get {
                return Environment.ExpandEnvironmentVariables(pS2Location);
            }
            set {
                if (pS2Location != value) {
                    pS2Location = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int regions;

        [DataMember]
        public int Regions {
            get {
                return regions;
            }
            set {
                if (regions != value) {
                    regions = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double exposureTime;

        [DataMember]
        public double ExposureTime {
            get {
                return exposureTime;
            }
            set {
                if (exposureTime != value) {
                    exposureTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double threshold;

        [DataMember]
        public double Threshold {
            get {
                return threshold;
            }
            set {
                if (threshold != value) {
                    threshold = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double rotationTolerance;

        [DataMember]
        public double RotationTolerance {
            get {
                return rotationTolerance;
            }
            set {
                if (rotationTolerance != value) {
                    rotationTolerance = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int numberOfAttempts;

        [DataMember]
        public int NumberOfAttempts {
            get {
                return numberOfAttempts;
            }
            set {
                if (numberOfAttempts != value) {
                    numberOfAttempts = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double reattemptDelay;

        [DataMember]
        public double ReattemptDelay {
            get {
                return reattemptDelay;
            }
            set {
                if (reattemptDelay != value) {
                    reattemptDelay = value;
                    RaisePropertyChanged();
                }
            }
        }

        private FilterInfo filter;

        [DataMember]
        public FilterInfo Filter {
            get {
                return filter;
            }
            set {
                if (filter != value) {
                    filter = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string aspsLocation;

        [DataMember]
        public string AspsLocation {
            get {
                return Environment.ExpandEnvironmentVariables(aspsLocation);
            }
            set {
                if (aspsLocation != value) {
                    aspsLocation = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string aSTAPLocation;

        [DataMember]
        public string ASTAPLocation {
            get {
                return Environment.ExpandEnvironmentVariables(aSTAPLocation);
            }
            set {
                if (aSTAPLocation != value) {
                    aSTAPLocation = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int downSampleFactor;

        [DataMember]
        public int DownSampleFactor {
            get {
                return downSampleFactor;
            }
            set {
                if (downSampleFactor != value) {
                    downSampleFactor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int maxObjects;

        [DataMember]
        public int MaxObjects {
            get {
                return maxObjects;
            }
            set {
                if (maxObjects != value) {
                    maxObjects = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool sync;

        [DataMember]
        public bool Sync {
            get {
                return sync;
            }
            set {
                if (sync != value) {
                    sync = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool slewToTarget;

        [DataMember]
        public bool SlewToTarget {
            get {
                return slewToTarget;
            }
            set {
                if (slewToTarget != value) {
                    slewToTarget = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short binning;

        [DataMember]
        public short Binning {
            get {
                return binning;
            }
            set {
                if (binning != value) {
                    binning = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int gain;

        [DataMember]
        public int Gain {
            get {
                return gain;
            }
            set {
                if (gain != value) {
                    gain = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}