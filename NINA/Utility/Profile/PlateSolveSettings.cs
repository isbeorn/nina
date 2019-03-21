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

using NINA.Model.MyFilterWheel;
using NINA.Utility.Enum;
using System;
using System.IO;
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
            downSampleFactor = 2;

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

        private string aSTAPLocation;

        [DataMember]
        public string ASTAPLocation {
            get {
                return Environment.ExpandEnvironmentVariables(aSTAPLocation);
            }
            set {
                aSTAPLocation = value;
                RaisePropertyChanged();
            }
        }

        private int downSampleFactor;

        [DataMember]
        public int DownSampleFactor {
            get {
                return downSampleFactor;
            }
            set {
                downSampleFactor = value;
                RaisePropertyChanged();
            }
        }
    }
}