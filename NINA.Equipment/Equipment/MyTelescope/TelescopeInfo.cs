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
using NINA.Astrometry;
using System.Collections.Generic;
using NINA.Equipment.Interfaces;
using System.Collections;

namespace NINA.Equipment.Equipment.MyTelescope {

    public class TelescopeInfo : DeviceInfo {
        private double siderealTime;

        public double SiderealTime {
            get => siderealTime;
            set {
                if (siderealTime != value) {
                    siderealTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double rightAscension;

        public double RightAscension {
            get => rightAscension;
            set {
                if (rightAscension != value) {
                    rightAscension = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double declination;

        public double Declination {
            get => declination;
            set {
                if (declination != value) {
                    declination = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double siteLatitude;

        public double SiteLatitude {
            get => siteLatitude;
            set {
                if (siteLatitude != value) {
                    siteLatitude = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double siteLongitude;

        public double SiteLongitude {
            get => siteLongitude;
            set {
                if (siteLongitude != value) {
                    siteLongitude = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double siteElevation;

        public double SiteElevation {
            get => siteElevation;
            set {
                if (siteElevation != value) {
                    siteElevation = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string rightAscensionString;

        public string RightAscensionString {
            get => rightAscensionString;
            set {
                if (rightAscensionString != value) {
                    rightAscensionString = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string declinationString;

        public string DeclinationString {
            get => declinationString;
            set {
                if (declinationString != value) {
                    declinationString = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                if (coordinates != value) {
                    coordinates = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double timeToMeridianFlip;

        public double TimeToMeridianFlip {
            get => timeToMeridianFlip;
            set {
                if (timeToMeridianFlip != value) {
                    timeToMeridianFlip = value;
                    RaisePropertyChanged();
                }
            }
        }

        private PierSide sideOfPier;

        public PierSide SideOfPier {
            get => sideOfPier;
            set {
                if (sideOfPier != value) {
                    sideOfPier = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double altitude = double.NaN;

        public double Altitude {
            get => altitude;
            set {
                if (altitude != value) {
                    altitude = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string altitudeString = string.Empty;

        public string AltitudeString {
            get => altitudeString;
            set {
                if (altitudeString != value) {
                    altitudeString = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double azimuth = double.NaN;

        public double Azimuth {
            get => azimuth;
            set {
                if (azimuth != value) {
                    azimuth = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string azimuthString = string.Empty;

        public string AzimuthString {
            get => azimuthString;
            set {
                if (azimuthString != value) {
                    azimuthString = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string siderealTimeString;

        public string SiderealTimeString {
            get => siderealTimeString;
            set {
                if (siderealTimeString != value) {
                    siderealTimeString = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string hoursToMeridianString;

        public string HoursToMeridianString {
            get => hoursToMeridianString;
            set {
                if (hoursToMeridianString != value) {
                    hoursToMeridianString = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool atPark;

        public bool AtPark {
            get => atPark;
            set {
                if (atPark != value) {
                    atPark = value;
                    RaisePropertyChanged();
                }
            }
        }

        private TrackingRate trackingRate;

        public TrackingRate TrackingRate {
            get => trackingRate;
            set {
                if (trackingRate != value) {
                    trackingRate = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TrackingMode));
                }
            }
        }

        private bool trackingEnabled;

        public bool TrackingEnabled {
            get => trackingEnabled;
            set {
                if (trackingEnabled != value) {
                    trackingEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IList<TrackingMode> trackingModes;

        public IList<TrackingMode> TrackingModes {
            get => trackingModes;
            set {
                if (trackingModes != value) {
                    trackingModes = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool atHome;

        public bool AtHome {
            get => atHome;
            set {
                if (atHome != value) {
                    atHome = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canFindHome;

        public bool CanFindHome {
            get => canFindHome;
            set {
                if (canFindHome != value) {
                    canFindHome = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canPark;

        public bool CanPark {
            get => canPark;
            set {
                if (canPark != value) {
                    canPark = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canSetPark;

        public bool CanSetPark {
            get => canSetPark;
            set {
                if (canSetPark != value) {
                    canSetPark = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canSetTracking;

        public bool CanSetTrackingEnabled {
            get => canSetTracking;
            set {
                if (canSetTracking != value) {
                    canSetTracking = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canSetDeclinationRate;

        public bool CanSetDeclinationRate {
            get => canSetDeclinationRate;
            set {
                if (canSetDeclinationRate != value) {
                    canSetDeclinationRate = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canSetRightAscensionRate;

        public bool CanSetRightAscensionRate {
            get => canSetRightAscensionRate;
            set {
                if (canSetRightAscensionRate != value) {
                    canSetRightAscensionRate = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Epoch equatorialSystem;

        public Epoch EquatorialSystem {
            get => equatorialSystem;
            set {
                if (equatorialSystem != value) {
                    equatorialSystem = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool hasUnknownEpoch;

        public bool HasUnknownEpoch {
            get => hasUnknownEpoch;
            set {
                if (hasUnknownEpoch != value) {
                    hasUnknownEpoch = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string timeToMeridianFlipString;

        public string TimeToMeridianFlipString {
            get => timeToMeridianFlipString;
            set {
                if (timeToMeridianFlipString != value) {
                    timeToMeridianFlipString = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Coordinates targetCoordinates;

        public Coordinates TargetCoordinates {
            get => targetCoordinates;
            set {
                if (targetCoordinates != value) {
                    targetCoordinates = value;
                    RaisePropertyChanged();
                }
            }
        }

        private PierSide? targetSideOfPier;

        public PierSide? TargetSideOfPier {
            get => targetSideOfPier;
            set {
                if (targetSideOfPier != value) {
                    targetSideOfPier = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool slewing;

        public bool Slewing {
            get => slewing;
            set {
                if (slewing != value) {
                    slewing = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double guideRateRightAscensionArcsecPerSec;

        public double GuideRateRightAscensionArcsecPerSec {
            get => guideRateRightAscensionArcsecPerSec;
            set {
                if (guideRateRightAscensionArcsecPerSec != value) {
                    guideRateRightAscensionArcsecPerSec = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double guideRateDeclinationArcsecPerSec;

        public double GuideRateDeclinationArcsecPerSec {
            get => guideRateDeclinationArcsecPerSec;
            set {
                if (guideRateDeclinationArcsecPerSec != value) {
                    guideRateDeclinationArcsecPerSec = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canMovePrimaryAxis;

        public bool CanMovePrimaryAxis {
            get => canMovePrimaryAxis;
            set {
                if (canMovePrimaryAxis != value) {
                    canMovePrimaryAxis = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canMoveSecondaryAxis;

        public bool CanMoveSecondaryAxis {
            get => canMoveSecondaryAxis;
            set {
                if (canMoveSecondaryAxis != value) {
                    canMoveSecondaryAxis = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IList<(double, double)> primaryAxisRates;

        public IList<(double, double)> PrimaryAxisRates {
            get => primaryAxisRates;
            set {
                if (primaryAxisRates != value) {
                    primaryAxisRates = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IList<(double, double)> secondaryAxisRates;

        public IList<(double, double)> SecondaryAxisRates {
            get => secondaryAxisRates;
            set {
                if (secondaryAxisRates != value) {
                    secondaryAxisRates = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IList<string> supportedActions;

        public IList<string> SupportedActions {
            get => supportedActions;
            set {
                if (supportedActions != value) {
                    supportedActions = value;
                    RaisePropertyChanged();
                }
            }
        }

        private AlignmentMode alignmentMode;

        public AlignmentMode AlignmentMode {
            get => alignmentMode;
            set {
                if (alignmentMode != value) {
                    alignmentMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canPulseGuide;

        public bool CanPulseGuide {
            get => canPulseGuide;
            set {
                if (canPulseGuide != value) {
                    canPulseGuide = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isPulseGuiding;

        public bool IsPulseGuiding {
            get => isPulseGuiding;
            set {
                if (isPulseGuiding != value) {
                    isPulseGuiding = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canSetPierSide;

        public bool CanSetPierSide {
            get => canSetPierSide;
            set {
                if (canSetPierSide != value) {
                    canSetPierSide = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canSlew;

        public bool CanSlew {
            get => canSlew;
            set {
                if (canSlew != value) {
                    canSlew = value;
                    RaisePropertyChanged();
                }
            }
        }

        private System.DateTime uTCDate;

        public System.DateTime UTCDate {
            get => uTCDate;
            set {
                if (uTCDate != value) {
                    uTCDate = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}