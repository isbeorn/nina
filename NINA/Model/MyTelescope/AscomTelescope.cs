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

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyTelescope {

    internal class AscomTelescope : BaseINPC, ITelescope, IDisposable {

        public AscomTelescope(string telescopeId, string name, IProfileService profileService) {
            this.profileService = profileService;
            Id = telescopeId;
            Name = name;
        }

        private IProfileService profileService;

        public string Category { get; } = "ASCOM";

        private void init() {
            _canGetAlignmentMode = true;
            _canGetAltitude = true;
            _canGetApertureArea = true;
            _canGetApertureDiameter = true;
            _canGetAzimuth = true;
            _canDoRefraction = true;
            _canGetFocalLength = true;
            _canSetUTCDate = true;
            _canGetSlewing = true;
            _canGetSideOfPier = true;
            _canGetSiteElevation = true;
            _canSetSiteElevation = true;
            _canGetSiteLatLong = true;
            _canSetSiteLatLong = true;
            _canSetSlewSettleTime = true;
            _canGetTargetRaDec = true;
            _canSetTargetRaDec = true;
        }

        private string _id;

        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private Telescope _telescope;

        private bool _canGetAlignmentMode;

        public AlignmentModes AlignmentMode {
            get {
                AlignmentModes val = AlignmentModes.algGermanPolar;
                try {
                    if (Connected && _canGetAlignmentMode) {
                        val = _telescope.AlignmentMode;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetAlignmentMode = false;
                }
                return val;
            }
        }

        public bool CanSetSiteLatLong {
            get {
                return _canSetSiteLatLong;
            }
        }

        private bool _canGetAltitude;

        public double Altitude {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetAltitude) {
                        val = _telescope.Altitude;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetAltitude = false;
                }
                return val;
            }
        }

        public string AltitudeString {
            get {
                return Astrometry.DegreesToDMS(Altitude);
            }
        }

        private bool _canGetAzimuth;

        public double Azimuth {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetAzimuth) {
                        val = _telescope.Azimuth;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetAzimuth = false;
                }
                return val;
            }
        }

        public string AzimuthString {
            get {
                return Astrometry.DegreesToDMS(Azimuth);
            }
        }

        private bool _canGetApertureArea;

        public double ApertureArea {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetApertureArea) {
                        val = _telescope.Altitude;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetApertureArea = false;
                }
                return val;
            }
        }

        private bool _canGetApertureDiameter;

        public double ApertureDiameter {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetApertureDiameter) {
                        val = _telescope.ApertureDiameter;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetApertureDiameter = false;
                }
                return val;
            }
        }

        public bool AtHome {
            get {
                if (Connected) {
                    return _telescope.AtHome;
                } else {
                    return false;
                }
            }
        }

        public bool AtPark {
            get {
                if (Connected) {
                    return _telescope.AtPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanFindHome {
            get {
                if (Connected) {
                    return _telescope.CanFindHome;
                } else {
                    return false;
                }
            }
        }

        public bool CanPark {
            get {
                if (Connected) {
                    return _telescope.CanPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanPulseGuide {
            get {
                if (Connected) {
                    return _telescope.CanPulseGuide;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetDeclinationRate {
            get {
                if (Connected) {
                    return _telescope.CanSetDeclinationRate;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetGuideRates {
            get {
                if (Connected) {
                    return _telescope.CanSetGuideRates;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetPark {
            get {
                if (Connected) {
                    return _telescope.CanSetPark;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetPierSide {
            get {
                if (Connected) {
                    return _telescope.CanSetPierSide;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetRightAscensionRate {
            get {
                if (Connected) {
                    return _telescope.CanSetRightAscensionRate;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetTracking {
            get {
                if (Connected) {
                    return _telescope.CanSetTracking;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlew {
            get {
                if (Connected) {
                    return _telescope.CanSlew;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAltAz {
            get {
                if (Connected) {
                    return _telescope.CanSlewAltAz;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAltAzAsync {
            get {
                if (Connected) {
                    return _telescope.CanSlewAltAzAsync;
                } else {
                    return false;
                }
            }
        }

        public bool CanSlewAsync {
            get {
                if (Connected) {
                    return _telescope.CanSlewAsync;
                } else {
                    return false;
                }
            }
        }

        public bool CanSync {
            get {
                if (Connected) {
                    return _telescope.CanSync;
                } else {
                    return false;
                }
            }
        }

        public bool CanSyncAltAz {
            get {
                if (Connected) {
                    return _telescope.CanSyncAltAz;
                } else {
                    return false;
                }
            }
        }

        public bool CanUnpark {
            get {
                if (Connected) {
                    return _telescope.CanUnpark;
                } else {
                    return false;
                }
            }
        }

        public Coordinates Coordinates {
            get {
                return new Coordinates(RightAscension, Declination, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);
            }
        }

        public double Declination {
            get {
                if (Connected) {
                    return _telescope.Declination;
                } else {
                    return -1;
                }
            }
        }

        public string DeclinationString {
            get {
                return Astrometry.DegreesToDMS(Declination);
            }
        }

        public double DeclinationRate {
            get {
                if (Connected) {
                    return _telescope.DeclinationRate;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (Connected && CanSetDeclinationRate) {
                        _telescope.DeclinationRate = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public double RightAscensionRate {
            get {
                if (Connected) {
                    return _telescope.RightAscensionRate;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (Connected && CanSetRightAscensionRate) {
                        _telescope.RightAscensionRate = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canGetSideOfPier;

        public PierSide SideOfPier {
            get {
                PierSide val = PierSide.pierUnknown;
                try {
                    if (Connected && _canGetSideOfPier) {
                        val = (PierSide)_telescope.SideOfPier;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetSideOfPier = false;
                }
                return val;
            }
            set {
                try {
                    if (Connected && CanSetPierSide) {
                        _telescope.SideOfPier = (ASCOM.DeviceInterface.PierSide)value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public string Description {
            get {
                if (Connected) {
                    return _telescope.Description;
                } else {
                    return string.Empty;
                }
            }
        }

        private bool _canDoRefraction;

        public bool DoesRefraction {
            get {
                bool val = false;
                try {
                    if (Connected && _canDoRefraction) {
                        val = _telescope.DoesRefraction;
                    }
                } catch (PropertyNotImplementedException) {
                    _canDoRefraction = false;
                }
                return val;
            }
        }

        public string DriverInfo {
            get {
                if (Connected) {
                    return _telescope.DriverInfo;
                } else {
                    return string.Empty;
                }
            }
        }

        public string DriverVersion {
            get {
                if (Connected) {
                    return _telescope.DriverVersion;
                } else {
                    return string.Empty;
                }
            }
        }

        public EquatorialCoordinateType EquatorialSystem {
            get {
                if (Connected) {
                    return _telescope.EquatorialSystem;
                } else {
                    return EquatorialCoordinateType.equOther;
                }
            }
        }

        private bool _canGetFocalLength;

        public double FocalLength {
            get {
                double val = -1;
                try {
                    if (Connected && _canGetFocalLength) {
                        val = _telescope.FocalLength;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetFocalLength = false;
                }
                return val;
            }
        }

        public short InterfaceVersion {
            get {
                if (Connected) {
                    return _telescope.InterfaceVersion;
                } else {
                    return -1;
                }
            }
        }

        private string _name;

        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public double RightAscension {
            get {
                if (Connected) {
                    return _telescope.RightAscension;
                } else {
                    return -1;
                }
            }
        }

        public string RightAscensionString {
            get {
                return Astrometry.HoursToHMS(RightAscension);
            }
        }

        public double SiderealTime {
            get {
                if (Connected) {
                    return _telescope.SiderealTime;
                } else {
                    return -1;
                }
            }
        }

        public string SiderealTimeString {
            get {
                return Astrometry.HoursToHMS(SiderealTime);
            }
        }

        private bool _canGetSlewing;

        public bool Slewing {
            get {
                bool val = false;
                try {
                    if (Connected && _canGetSlewing) {
                        val = _telescope.Slewing;
                    }
                } catch (PropertyNotImplementedException) {
                    _canGetSlewing = false;
                }
                return val;
            }
        }

        public ArrayList SupportedActions {
            get {
                if (Connected) {
                    return _telescope.SupportedActions;
                } else {
                    return new ArrayList();
                }
            }
        }

        public ITrackingRates TrackingRates {
            get {
                if (Connected) {
                    return _telescope.TrackingRates;
                } else {
                    return null;
                }
            }
        }

        public bool IsPulseGuiding {
            get {
                if (Connected && CanPulseGuide) {
                    return _telescope.IsPulseGuiding;
                } else {
                    return false;
                }
            }
        }

        public bool Tracking {
            get {
                if (Connected) {
                    return _telescope.Tracking;
                } else {
                    return false;
                }
            }
            set {
                if (Connected && CanSetTracking) {
                    _telescope.Tracking = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double GuideRateDeclination {
            get {
                if (Connected) {
                    return _telescope.GuideRateDeclination;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (Connected && CanSetGuideRates) {
                        _telescope.GuideRateDeclination = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public double GuideRateRightAscension {
            get {
                if (Connected) {
                    return _telescope.GuideRateRightAscension;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    if (Connected && CanSetGuideRates) {
                        _telescope.GuideRateRightAscension = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public DriveRates TrackingRate {
            get {
                if (Connected) {
                    return _telescope.TrackingRate;
                } else {
                    return DriveRates.driveSidereal;
                }
            }
            set {
                try {
                    if (Connected) {
                        _telescope.TrackingRate = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canSetUTCDate;

        public DateTime UTCDate {
            get {
                if (Connected) {
                    return _telescope.UTCDate;
                } else {
                    return DateTime.MinValue;
                }
            }
            set {
                try {
                    if (Connected && _canSetUTCDate) {
                        _telescope.UTCDate = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetUTCDate = false;
                }
            }
        }

        private bool _canGetSiteElevation;
        private bool _canSetSiteElevation;

        public double SiteElevation {
            get {
                double val = -1;
                if (Connected && _canGetSiteElevation) {
                    try {
                        val = _telescope.SiteElevation;
                    } catch (PropertyNotImplementedException) {
                        _canGetSiteElevation = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteElevation) {
                        _telescope.SiteElevation = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetSiteElevation = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canGetSiteLatLong;
        private bool _canSetSiteLatLong;

        public double SiteLatitude {
            get {
                double val = -1;
                if (Connected && _canGetSiteLatLong) {
                    try {
                        val = _telescope.SiteLatitude;
                    } catch (PropertyNotImplementedException) {
                        _canGetSiteLatLong = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteLatLong) {
                        _telescope.SiteLatitude = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetSiteLatLong = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public double SiteLongitude {
            get {
                double val = -1;
                if (Connected && _canGetSiteLatLong) {
                    try {
                        val = _telescope.SiteLongitude;
                    } catch (PropertyNotImplementedException) {
                        _canGetSiteLatLong = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSiteLatLong) {
                        _telescope.SiteLongitude = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetSiteLatLong = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canSetSlewSettleTime;

        public short SlewSettleTime {
            get {
                short val = -1;
                if (Connected && _canSetSlewSettleTime) {
                    try {
                        val = _telescope.SlewSettleTime;
                    } catch (PropertyNotImplementedException) {
                        _canSetSlewSettleTime = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetSlewSettleTime) {
                        _telescope.SlewSettleTime = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetSlewSettleTime = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _canGetTargetRaDec;
        private bool _canSetTargetRaDec;

        public double TargetDeclination {
            get {
                double val = -1;
                if (Connected && _canGetTargetRaDec) {
                    try {
                        val = _telescope.TargetDeclination;
                    } catch (PropertyNotImplementedException) {
                        _canGetTargetRaDec = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetTargetRaDec) {
                        _telescope.TargetDeclination = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetTargetRaDec = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        public double TargetRightAscension {
            get {
                double val = -1;
                if (Connected && _canGetTargetRaDec) {
                    try {
                        val = _telescope.TargetRightAscension;
                    } catch (PropertyNotImplementedException) {
                        _canGetTargetRaDec = false;
                    }
                }
                return val;
            }
            set {
                try {
                    if (Connected && _canSetTargetRaDec) {
                        _telescope.TargetRightAscension = value;
                        RaisePropertyChanged();
                    }
                } catch (PropertyNotImplementedException ex) {
                    Logger.Warning(ex.Message);
                    _canSetTargetRaDec = false;
                } catch (InvalidValueException ex) {
                    Logger.Warning(ex.Message);
                } catch (ASCOM.InvalidOperationException ex) {
                    Logger.Warning(ex.Message);
                }
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _telescope.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception) {
                        Disconnect();
                    }
                    return val;
                } else {
                    return false;
                }
            }
            private set {
                try {
                    _connected = value;
                    _telescope.Connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblReconnectTelescope"] + Environment.NewLine + ex.Message);
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }

        public void Disconnect() {
            Connected = false;
            _telescope?.Dispose();
            _telescope = null;
        }

        public bool MeridianFlip(Coordinates targetCoordinates) {
            var success = false;
            try {
                if (!Tracking) {
                    Tracking = true;
                }

                if (CanSetPierSide) {
                    var pierside = SideOfPier;
                    var flippedside = pierside == PierSide.pierEast ? PierSide.pierWest : PierSide.pierEast;
                    SideOfPier = flippedside;
                }

                SlewToCoordinates(targetCoordinates.RA, targetCoordinates.Dec);
                success = true;
            } catch (Exception) {
                Notification.ShowError(Locale.Loc.Instance["LblMeridianFlipFailed"]);
            }
            return success;
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            if (Connected) {
                if (CanSlew) {
                    if (!AtPark) {
                        try {
                            ASCOM.DeviceInterface.TelescopeAxes translatedAxis;
                            switch (axis) {
                                case TelescopeAxes.Primary:
                                    translatedAxis = ASCOM.DeviceInterface.TelescopeAxes.axisPrimary;
                                    break;

                                case TelescopeAxes.Secondary:
                                    translatedAxis = ASCOM.DeviceInterface.TelescopeAxes.axisSecondary;
                                    break;

                                case TelescopeAxes.Tertiary:
                                    translatedAxis = ASCOM.DeviceInterface.TelescopeAxes.axisTertiary;
                                    break;

                                default:
                                    translatedAxis = ASCOM.DeviceInterface.TelescopeAxes.axisPrimary;
                                    break;
                            }
                            _telescope.MoveAxis(translatedAxis, rate);
                        } catch (Exception e) {
                            Notification.ShowError(e.Message);
                        }
                    } else {
                        Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeParkedWarn"]);
                    }
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeCannotSlew"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeNotConnected"]);
            }
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            if (Connected) {
                if (CanPulseGuide) {
                    if (!AtPark) {
                        try {
                            _telescope.PulseGuide((ASCOM.DeviceInterface.GuideDirections)direction, duration);
                        } catch (Exception e) {
                            Notification.ShowError(e.Message);
                        }
                    } else {
                        Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeParkedWarn"]);
                    }
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeCannotPulseGuide"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblTelescopeNotConnected"]);
            }
        }

        public void Park() {
            if (Connected && CanPark) {
                try {
                    _telescope.Park();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                } finally {
                }
            }
        }

        public void Setpark() {
            if (Connected && CanSetPark) {
                try {
                    _telescope.SetPark();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void SlewToCoordinatesAsync(double ra, double dec) {
            if (Connected && CanSlew && !AtPark) {
                try {
                    if (!Tracking) {
                        Tracking = true;
                    }
                    _telescope.SlewToCoordinatesAsync(ra, dec);
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void SlewToCoordinates(double ra, double dec) {
            if (Connected && CanSlew && !AtPark) {
                try {
                    if (!Tracking) {
                        Tracking = true;
                    }
                    _telescope.SlewToCoordinates(ra, dec);
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void SlewToAltAz(double az, double alt) {
            if (Connected && CanSlew && !AtPark) {
                try {
                    _telescope.SlewToAltAz(az, alt);
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void SlewToAltAzAsync(double az, double alt) {
            if (Connected && CanSlew && !AtPark) {
                try {
                    _telescope.SlewToAltAzAsync(az, alt);
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void StopSlew() {
            if (Connected && CanSlew) {
                _telescope.AbortSlew();
            }
        }

        private static readonly Lazy<ASCOM.Utilities.Util> lazyAscomUtil =
            new Lazy<ASCOM.Utilities.Util>(() => new ASCOM.Utilities.Util());

        private static ASCOM.Utilities.Util AscomUtil { get { return lazyAscomUtil.Value; } }

        public bool Sync(string ra, string dec) {
            return Sync(AscomUtil.HMSToHours(ra), AscomUtil.DMSToDegrees(dec));
        }

        public bool Sync(double ra, double dec) {
            bool success = false;
            if (Connected && CanSync) {
                if (Tracking) {
                    try {
                        _telescope.SyncToCoordinates(ra, dec);
                        success = true;
                    } catch (Exception ex) {
                        Notification.ShowError(ex.Message);
                    }
                } else {
                    Notification.ShowError(Locale.Loc.Instance["LblTelescopeNotTrackingForSync"]);
                }
            }
            return success;
        }

        public void Unpark() {
            if (Connected && CanUnpark) {
                try {
                    _telescope.Unpark();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void Dispose() {
            _telescope.Dispose();
        }

        public double HoursToMeridian {
            get {
                var hourstomed = RightAscension - SiderealTime;
                if (hourstomed < 0) {
                    hourstomed = hourstomed + 24;
                }
                return hourstomed;
            }
        }

        public string HoursToMeridianString {
            get {
                return Astrometry.HoursToHMS(HoursToMeridian);
            }
        }

        public double TimeToMeridianFlip {
            get {
                var hourstomed = double.MaxValue;
                try {
                    hourstomed = RightAscension + (profileService.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian / 60) - SiderealTime;
                    if (hourstomed < 0) {
                        hourstomed += 24;
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
                return hourstomed;
            }
        }

        public string TimeToMeridianFlipString {
            get {
                return Astrometry.HoursToHMS(TimeToMeridianFlip);
            }
        }

        private double _movingRate;

        public double MovingRate {
            get {
                return _movingRate;
            }
            set {
                if (Connected) {
                    double result = value;
                    if (result < 0) result = 0;
                    bool incr = result > _movingRate;

                    double max = double.MinValue;
                    double min = double.MaxValue;
                    IAxisRates r = _telescope.AxisRates(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary);
                    IEnumerator e = r.GetEnumerator();
                    foreach (IRate item in r) {
                        if (min > item.Minimum) {
                            min = item.Minimum;
                        }
                        if (max < item.Maximum) {
                            max = item.Maximum;
                        }

                        if (item.Minimum <= value && value <= item.Maximum) {
                            result = value;
                            break;
                        } else if (incr && value < item.Minimum) {
                            result = item.Minimum;
                        } else if (!incr && value > item.Maximum) {
                            result = item.Maximum;
                        }
                    }
                    if (result > max) result = max;
                    if (result < min) result = min;

                    _movingRate = result;
                    RaisePropertyChanged();
                }
            }
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (_telescope == null) {
                        _telescope = new Telescope(Id);
                    }
                    _telescope.SetupDialog();
                    if (dispose) {
                        _telescope.Dispose();
                        _telescope = null;
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public void SendCommandString(string command) {
            if (Connected) {
                _telescope.CommandString(command, true);
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblTelescopeNotConnectedForCommand"] + ": " + command);
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                try {
                    _telescope = new Telescope(Id);
                    Connected = true;
                    if (Connected) {
                        init();
                        SiteLongitude = SiteLongitude;
                        SiteLatitude = SiteLatitude;
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("Unable to connect to telescope " + ex.Message);
                }
                return Connected;
            });
        }
    }
}