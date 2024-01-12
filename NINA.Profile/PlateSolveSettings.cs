#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;
using System;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public partial class PlateSolveSettings : Settings, IPlateSolveSettings {
        private PlateSolverEnum plateSolverType = PlateSolverEnum.ASTAP;

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            plateSolverType = PlateSolverEnum.ASTAP;
            blindSolverType = BlindSolverEnum.ASTAP;
            blindFailoverEnabled = true;
            astrometryURL = "http://nova.astrometry.net";
            astrometryAPIKey = string.Empty;
            cygwinLocation = string.Empty;
            searchRadius = 30;
            pS2Location = string.Empty;
            pS3Location = string.Empty;
            regions = 5000;
            exposureTime = 2.0d;
            threshold = 1.0d;
            rotationTolerance = 1.0d;
            reattemptDelay = 2;
            numberOfAttempts = 10;
            filter = null;
            downSampleFactor = 0;
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

            _theSkyXHost = "localhost";
            _theSkyXPort = 3040;

            pinPointCatalogType = Dc3PoinPointCatalogEnum.ppGSCACT;
            pinPointCatalogRoot = Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\GSC11\");
            pinPointMaxMagnitude = 20;
            pinPointExpansion = 40;
            pinPointAllSkyApiKey = string.Empty;
            pinPointAllSkyApiHost = "nova.astrometry.net";
        }

        [DataMember]
        public PlateSolverEnum PlateSolverType {
            get => plateSolverType;
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
            get => blindSolverType;
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
            get => astrometryURL;
            set {
                // Clear out any whitespace characters in the URL
                string url = Whitespace().Replace(value, string.Empty);

                if (astrometryURL != url) {
                    astrometryURL = url;
                    RaisePropertyChanged();
                }
            }
        }

        private string astrometryAPIKey;

        [DataMember]
        public string AstrometryAPIKey {
            get => astrometryAPIKey;
            set {
                // Whitespace characters are not valid characaters in an Astrometry.net API key.
                // Help the user by removing any that might be present. Copy and pasting from the astrometry.net API page
                // can sometimes insert a space at the end of the API key string, and it's not very obvious.
                string key = Whitespace().Replace(value, string.Empty);

                if (astrometryAPIKey != key) {
                    astrometryAPIKey = key;
                    RaisePropertyChanged();
                }
            }
        }

        private string cygwinLocation;

        [DataMember]
        public string CygwinLocation {
            get => Environment.ExpandEnvironmentVariables(cygwinLocation);
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
            get => searchRadius;
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
            get => Environment.ExpandEnvironmentVariables(pS2Location);
            set {
                if (pS2Location != value) {
                    pS2Location = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string pS3Location;

        [DataMember]
        public string PS3Location {
            get => Environment.ExpandEnvironmentVariables(pS3Location);
            set {
                if (pS3Location != value) {
                    pS3Location = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int regions;

        [DataMember]
        public int Regions {
            get => regions;
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
            get => exposureTime;
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
            get => threshold;
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
            get => rotationTolerance;
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
            get => numberOfAttempts;
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
            get => reattemptDelay;
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
            get => filter;
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
            get => Environment.ExpandEnvironmentVariables(aspsLocation);
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
            get => Environment.ExpandEnvironmentVariables(aSTAPLocation);
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
            get => downSampleFactor;
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
            get => maxObjects;
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
            get => sync;
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
            get => slewToTarget;
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
            get => binning;
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
            get => gain;
            set {
                if (gain != value) {
                    gain = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool blindFailoverEnabled;

        [DataMember]
        public bool BlindFailoverEnabled {
            get => blindFailoverEnabled;
            set {
                if (blindFailoverEnabled != value) {
                    blindFailoverEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _theSkyXHost;

        [DataMember]
        public string TheSkyXHost {
            get => _theSkyXHost;
            set {
                if (_theSkyXHost != value) {
                    _theSkyXHost = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _theSkyXPort;

        [DataMember]
        public int TheSkyXPort {
            get => _theSkyXPort;
            set {
                if (_theSkyXPort != value) {
                    _theSkyXPort = value;
                    RaisePropertyChanged();
                }

            }
        }

        private Dc3PoinPointCatalogEnum pinPointCatalogType;

        [DataMember]
        public Dc3PoinPointCatalogEnum PinPointCatalogType {
            get => pinPointCatalogType;
            set {
                if (pinPointCatalogType != value) {
                    pinPointCatalogType = value;
                    RaisePropertyChanged();
                }

            }
        }

        private string pinPointCatalogRoot;

        [DataMember]
        public string PinPointCatalogRoot {
            get => pinPointCatalogRoot;
            set {
                if (pinPointCatalogRoot != value) {
                    pinPointCatalogRoot = value;
                    RaisePropertyChanged();
                }

            }
        }

        private double pinPointMaxMagnitude;

        [DataMember]
        public double PinPointMaxMagnitude {
            get => pinPointMaxMagnitude;
            set {
                if (pinPointMaxMagnitude != value) {
                    pinPointMaxMagnitude = value;
                    RaisePropertyChanged();
                }

            }
        }

        private double pinPointExpansion;

        [DataMember]
        public double PinPointExpansion {
            get => pinPointExpansion;
            set {
                if (pinPointExpansion != value) {
                    pinPointExpansion = value;
                    RaisePropertyChanged();
                }

            }
        }

        private string pinPointAllSkyApiKey;

        [DataMember]
        public string PinPointAllSkyApiKey {
            get => pinPointAllSkyApiKey;
            set {
                string key = Whitespace().Replace(value, string.Empty);

                if (pinPointAllSkyApiKey != key) {
                    pinPointAllSkyApiKey = key;
                    RaisePropertyChanged();
                }

            }
        }

        private string pinPointAllSkyApiHost;

        [DataMember]
        public string PinPointAllSkyApiHost {
            get => pinPointAllSkyApiHost;
            set {
                string host = value.Trim();

                if (pinPointAllSkyApiHost != host) {
                    pinPointAllSkyApiHost = host;
                    RaisePropertyChanged();
                }

            }
        }

        [GeneratedRegex(@"\s")]
        private static partial Regex Whitespace();
    }
}