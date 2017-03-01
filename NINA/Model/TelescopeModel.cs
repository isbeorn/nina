using ASCOM.DriverAccess;
using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;

namespace NINA.Model {
    class TelescopeModel :BaseINPC {
        public TelescopeModel() {
           
            Init();
        }

        private void Init() {
            
        }

        private Telescope _telescope;
        public Telescope Telescope {
            get {
                return _telescope;
            }

            set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private string _progId;
        public string ProgId {
            get {
                return _progId;
            }
            set {
                _progId = value;
                RaisePropertyChanged();
            }
        }

        bool _connected;
        public bool Connected {
            get {
                return _connected;
            }
            set {
                _connected = value;
                if (Telescope != null) {
                    Telescope.Connected = value;
                }
                RaisePropertyChanged();
            }
        }

        internal void UpdateValues() {
            try {
                Altitude = Telescope.Altitude;
                Azimuth = Telescope.Azimuth;
                Declination = Telescope.Declination;
                RightAscension = Telescope.RightAscension;
                SiderealTime = Telescope.SiderealTime;
                AtPark = Telescope.AtPark;
                Tracking = Telescope.Tracking;
            } catch (Exception e) {
                Notification.ShowError(e.Message);
            }
            
        }

        public AlignmentModes AlignmentMode {
            get {
                return _alignmentMode;
            }

            set {
                _alignmentMode = value;
                RaisePropertyChanged();
            }
        }

        public double Altitude {
            get {
                return _altitude;
            }

            set {
                _altitude = value;
                RaisePropertyChanged();
                RaisePropertyChanged("AltitudeString");
            }
        }

        public string AltitudeString {
            get {
                return Utility.Utility.AscomUtil.DegreesToDMS(Altitude);
            }
        }

        public double ApertureArea {
            get {
                return _apertureArea;
            }

            set {
                _apertureArea = value;
                RaisePropertyChanged();
            }
        }

        public double ApertureDiameter {
            get {
                return _apertureDiameter;
            }

            set {
                _apertureDiameter = value;
                RaisePropertyChanged();
            }
        }

        public bool AtHome {
            get {
                return _atHome;
            }

            set {
                _atHome = value;
                RaisePropertyChanged();
            }
        }

        public bool AtPark {
            get {
                return _atPark;
            }

            set {
                _atPark = value;
                RaisePropertyChanged();
            }
        }

        public double Azimuth {
            get {
                return _azimuth;
            }

            set {
                _azimuth = value;
                RaisePropertyChanged();
                RaisePropertyChanged("AzimuthString");
            }
        }

        internal void StopSlew() {
            if(Connected && CanSlew) {
                Telescope.AbortSlew();
            }
        }

        public string AzimuthString {
            get {
                return Utility.Utility.AscomUtil.DegreesToDMS(Azimuth);
            }
        }

        public bool CanFindHome {
            get {
                return _canFindHome;
            }

            set {
                _canFindHome = value;
                RaisePropertyChanged();
            }
        }

        public bool CanPark {
            get {
                return _canPark;
            }

            set {
                _canPark = value;
                RaisePropertyChanged();
            }
        }

        public bool CanPulseGuide {
            get {
                return _canPulseGuide;
            }

            set {
                _canPulseGuide = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetDeclinationRate {
            get {
                return _canSetDeclinationRate;
            }

            set {
                _canSetDeclinationRate = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetGuideRates {
            get {
                return _canSetGuideRates;
            }

            set {
                _canSetGuideRates = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetPark {
            get {
                return _canSetPark;
            }

            set {
                _canSetPark = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetPierSide {
            get {
                return _canSetPierSide;
            }

            set {
                _canSetPierSide = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetRightAscensionRate {
            get {
                return _canSetRightAscensionRate;
            }

            set {
                _canSetRightAscensionRate = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetTracking {
            get {
                return _canSetTracking;
            }

            set {
                _canSetTracking = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSlew {
            get {
                return _canSlew;
            }

            set {
                _canSlew = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSlewAltAz {
            get {
                return _canSlewAltAz;
            }

            set {
                _canSlewAltAz = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSlewAltAzAsync {
            get {
                return _canSlewAltAzAsync;
            }

            set {
                _canSlewAltAzAsync = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSlewAsync {
            get {
                return _canSlewAsync;
            }

            set {
                _canSlewAsync = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSync {
            get {
                return _canSync;
            }

            set {
                _canSync = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSyncAltAz {
            get {
                return _canSyncAltAz;
            }

            set {
                _canSyncAltAz = value;
                RaisePropertyChanged();
            }
        }

        public bool CanUnpark {
            get {
                return _canUnpark;
            }

            set {
                _canUnpark = value;
                RaisePropertyChanged();
            }
        }

        public double Declination {
            get {
                return _declination;
            }

            set {
                _declination = value;
                RaisePropertyChanged();
                RaisePropertyChanged("DeclinationString");
            }
        }
        public string DeclinationString {
            get {
                return Utility.Utility.AscomUtil.DegreesToDMS(Declination);
            }
        }

        public string Description {
            get {
                return _description;
            }

            set {
                _description = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo {
            get {
                return _driverInfo;
            }

            set {
                _driverInfo = value;
                RaisePropertyChanged();
            }
        }

        public string DriverVersion {
            get {
                return _driverVersion;
            }

            set {
                _driverVersion = value;
                RaisePropertyChanged();
            }
        }

        public EquatorialCoordinateType EquatorialSystem {
            get {
                return _equatorialSystem;
            }

            set {
                _equatorialSystem = value;
                RaisePropertyChanged();
            }
        }

        public double FocalLength {
            get {
                return _focalLength;
            }

            set {
                _focalLength = value;
                RaisePropertyChanged();
            }
        }

        public short InterfaceVersion {
            get {
                return _interfaceVersion;
            }

            set {
                _interfaceVersion = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPulseGuiding {
            get {
                return _isPulseGuiding;
            }

            set {
                _isPulseGuiding = value;
                RaisePropertyChanged();
            }
        }

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
                return _rightAscension;
            }

            set {
                _rightAscension = value;
                RaisePropertyChanged();
                RaisePropertyChanged("RightAscensionString");
            }
        }
        public string RightAscensionString {
            get {
                return Utility.Utility.AscomUtil.HoursToHMS(RightAscension);
            }
        }


        public double SiderealTime {
            get {
                return _siderealTime;
            }

            set {
                _siderealTime = value;
                RaisePropertyChanged();
                RaisePropertyChanged("SiderealTimeString");
            }
        }
        public string SiderealTimeString {
            get {
                return Utility.Utility.AscomUtil.DegreesToDMS(SiderealTime);
            }
        }

        public bool Slewing {
            get {
                return _slewing;
            }

            set {
                _slewing = value;
                RaisePropertyChanged();
            }
        }

        public ArrayList SupportedActions {
            get {
                return _supportedActions;
            }

            set {
                _supportedActions = value;
                RaisePropertyChanged();
            }
        }

        public ITrackingRates TrackingRates {
            get {
                return _trackingRates;
            }

            set {
                _trackingRates = value;
                RaisePropertyChanged();
            }
        }

        public double DeclinationRate {
            get {
                return _declinationRate;
            }

            set {
                _declinationRate = value;
                RaisePropertyChanged();
            }
        }

        public bool DoesRefraction {
            get {
                return _doesRefraction;
            }

            set {
                _doesRefraction = value;
                RaisePropertyChanged();
            }
        }

        public double GuideRateDeclination {
            get {
                return _guideRateDeclination;
            }

            set {
                _guideRateDeclination = value;
                RaisePropertyChanged();
            }
        }

        public double GuideRateRightAscension {
            get {
                return _guideRateRightAscension;
            }

            set {
                _guideRateRightAscension = value;
                RaisePropertyChanged();
            }
        }

        public double RightAscensionRate {
            get {
                return _rightAscensionRate;
            }

            set {
                _rightAscensionRate = value;
                RaisePropertyChanged();
            }
        }

        public PierSide SideOfPier {
            get {
                return _sideOfPier;
            }

            set {
                _sideOfPier = value;
                RaisePropertyChanged();
            }
        }

        public double SiteElevation {
            get {
                return _siteElevation;
            }

            set {
                _siteElevation = value;
                RaisePropertyChanged();
            }
        }

        public double SiteLatitude {
            get {
                return _siteLatitude;
            }

            set {
                _siteLatitude = value;
                RaisePropertyChanged();
            }
        }

        public double SiteLongitude {
            get {
                return _siteLongitude;
            }

            set {
                _siteLongitude = value;
                RaisePropertyChanged();
            }
        }

        public short SlewSettleTime {
            get {
                return _slewSettleTime;
            }

            set {
                _slewSettleTime = value;
                RaisePropertyChanged();
            }
        }

        public double TargetDeclination {
            get {
                return _targetDeclination;
            }

            set {
                _targetDeclination = value;
                RaisePropertyChanged();
            }
        }

        public double TargetRightAscension {
            get {
                return _targetRightAscension;
            }

            set {
                _targetRightAscension = value;
                RaisePropertyChanged();
            }
        }

        public bool Tracking {
            get {
                return _tracking;
            }

            set {
                _tracking = value;
                RaisePropertyChanged();
            }
        }

        public DriveRates TrackingRate {
            get {
                return _trackingRate;
            }

            set {
                _trackingRate = value;
                RaisePropertyChanged();
            }
        }

        public DateTime UTCDate {
            get {
                return _uTCDate;
            }

            set {
                _uTCDate = value;
                RaisePropertyChanged();
            }
        }

        public bool Connect() {
            bool con = false;
            string oldProgId = this.ProgId;
            string telescopeId = Settings.TelescopeId;
            ProgId = ASCOM.DriverAccess.Telescope.Choose(telescopeId);
            if ((!Connected || oldProgId != ProgId) && ProgId != "") {

                Init();
                try {                    
                    Telescope = new Telescope(ProgId);                    
                    Connected = true;
                    Settings.TelescopeId = ProgId;                                        
                    GetTelescopeInfo();
                    Notification.ShowSuccess("Telescope connected.");
                    con = true;
                }
                catch (ASCOM.DriverAccessCOMException ex) {
                    Logger.Error("Unable to connect to telescope");
                    Logger.Trace(ex.Message);
                    Notification.ShowError("Unable to connect to telescope");
                    Connected = false;
                }
                catch (Exception ex) {
                    Notification.ShowError("Unable to connect to telescope");
                    Logger.Error("Unable to connect to telescope");
                    Logger.Trace(ex.Message);
                    Connected = false;
                }

            }
            return con;
        }

        private double _movingRate;
        public double MovingRate {
            get {
                return _movingRate;
            }
            set {
                if (Telescope != null && Telescope.Connected) {
                    double result = value;                
                    if (result < 0) result = 0;
                    bool incr = result > _movingRate;
               
                        double max = double.MinValue;
                        double min = double.MaxValue;
                        IAxisRates r = Telescope.AxisRates(TelescopeAxes.axisSecondary);
                        IEnumerator e = r.GetEnumerator();
                        foreach (IRate item in r) {
                            if(min > item.Minimum) {
                                min = item.Minimum;
                            }
                            if (max < item.Maximum) {
                                max = item.Maximum;
                            }
                        
                            if (item.Minimum <= value && value <= item.Maximum) {
                                result = value;
                                break;
                            }
                            else if (incr && value < item.Minimum) {
                                result = item.Minimum;
                            }
                            else if (!incr && value > item.Maximum) {
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
             
        

        private ASCOM.DeviceInterface.AlignmentModes _alignmentMode;
        private double _altitude;
        private double _apertureArea;
        private double _apertureDiameter;
        private bool _atHome;
        private bool _atPark;
        private double _azimuth;
        private bool _canFindHome;
        private bool _canPark;
        private bool _canPulseGuide;
        private bool _canSetDeclinationRate;
        private bool _canSetGuideRates;
        private bool _canSetPark;
        private bool _canSetPierSide;
        private bool _canSetRightAscensionRate;
        private bool _canSetTracking;
        private bool _canSlew;
        private bool _canSlewAltAz;
        private bool _canSlewAltAzAsync;
        private bool _canSlewAsync;
        private bool _canSync;
        private bool _canSyncAltAz;
        private bool _canUnpark;
        private double _declination;
        private string _description;
        private string _driverInfo;
        private string _driverVersion;
        private ASCOM.DeviceInterface.EquatorialCoordinateType _equatorialSystem;
        private double _focalLength;
        private short _interfaceVersion;
        private bool _isPulseGuiding;
        private string _name;
        private double _rightAscension;
        private double _siderealTime;
        private bool _slewing;
        private ArrayList _supportedActions;
        private ASCOM.DeviceInterface.ITrackingRates _trackingRates;
      

        /*Settable to Telescope */
        private double _declinationRate;
        private bool _doesRefraction;
        private double _guideRateDeclination;
        private double _guideRateRightAscension;
        private double _rightAscensionRate;
        private ASCOM.DeviceInterface.PierSide _sideOfPier;
        private double _siteElevation;
        private double _siteLatitude;
        private double _siteLongitude;
        private short _slewSettleTime;
        private double _targetDeclination;
        private double _targetRightAscension;
        private bool _tracking;
        private ASCOM.DeviceInterface.DriveRates _trackingRate;
        private DateTime _uTCDate;

        private void GetTelescopeInfo() {

            /*getters exclusive */
            

            try {                
                Altitude = Telescope.Altitude;
                RightAscension = Telescope.RightAscension;
                Declination = Telescope.Declination;
                Azimuth = Telescope.Azimuth;
                SiderealTime = Telescope.SiderealTime;
            }
            catch(Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement Altitude, RightAscencion, Declination, Azimuth or SiderealTime");
                Logger.Trace(ex.Message);
                Altitude = double.MinValue;
                RightAscension = double.MinValue;
                Declination = double.MinValue;
                Azimuth = double.MinValue;
                SiderealTime = double.MinValue;
            }

            try {
                ApertureArea = Telescope.ApertureArea;
                ApertureDiameter = Telescope.ApertureDiameter;
                FocalLength = Telescope.FocalLength;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement ApertureArea, ApertureDiameter or FocalLength");
                Logger.Trace(ex.Message);
                ApertureArea = double.MinValue;
                ApertureDiameter = double.MinValue;
                FocalLength = double.MinValue;
            }

            try {
                CanFindHome = Telescope.CanFindHome;
                CanPark = Telescope.CanPark;
                AtHome = Telescope.AtHome;
                AtPark = Telescope.AtPark;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement AtHome or AtPark");
                Logger.Trace(ex.Message);
                CanFindHome = false;
                CanPark = false;
                AtHome = false;
                AtPark = false;
            }

            try {
                Name = Telescope.Name;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement Name");
                Logger.Trace(ex.Message);
                Name = "n.A.";
            }

            try {
                Description = Telescope.Description;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement Description");
                Logger.Trace(ex.Message);
                Description = "n.A.";
            }

            try {
                DriverInfo = Telescope.DriverInfo;
                DriverVersion = Telescope.DriverVersion;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement DriverInfo or DriverVersion");
                Logger.Trace(ex.Message);
                DriverInfo = "n.A.";
                DriverVersion = "n.A.";
            }
            try {
                InterfaceVersion = Telescope.InterfaceVersion;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement InterfaceVersion");
                Logger.Trace(ex.Message);
                InterfaceVersion = 0;
            }
            try {
                EquatorialSystem = Telescope.EquatorialSystem;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement EquatorialSystem");
                Logger.Trace(ex.Message);
                EquatorialSystem = EquatorialCoordinateType.equOther;
            }
            try {
                CanPulseGuide = Telescope.CanPulseGuide;
                IsPulseGuiding = Telescope.IsPulseGuiding;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement IsPulseGuiding");
                Logger.Trace(ex.Message);
                CanPulseGuide = false;
                IsPulseGuiding = false;
            }
            try {
                CanSlew = Telescope.CanSlew;
                CanSlewAltAz = Telescope.CanSlewAltAz;
                CanSlewAltAzAsync = Telescope.CanSlewAltAzAsync;
                CanSlewAsync = Telescope.CanSlewAsync;
                Slewing = Telescope.Slewing;
                MovingRate = 1;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement Slewing");
                Logger.Trace(ex.Message);
                CanSlew = false;
                CanSlewAltAz = false;
                CanSlewAltAzAsync = false;
                CanSlewAsync = false;
                Slewing = false;
            }
            try {
                TrackingRates = Telescope.TrackingRates;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement TrackingRates");
                Logger.Trace(ex.Message);
                TrackingRates = null;
            }
            try {
                SupportedActions = Telescope.SupportedActions;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement SupportedActions");
                Logger.Trace(ex.Message);
                SupportedActions = new ArrayList();
            }

            try {
                CanSync = Telescope.CanSync;
                CanSyncAltAz = Telescope.CanSyncAltAz;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement CanSync");
                Logger.Trace(ex.Message);
                CanSync = false;
                CanSyncAltAz = false;
            }

            try {
                CanSetPark = Telescope.CanSetPark;
                CanUnpark = Telescope.CanUnpark;
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement CanSetPark/CanUnpark");
                Logger.Trace(ex.Message);
                CanSetPark = false;
                CanUnpark = false;
            }

            try {
                CanSetTracking = Telescope.CanSetTracking;
                Tracking = Telescope.Tracking; /*Settable*/
                TrackingRate = Telescope.TrackingRate; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement Tracking or TrackingRate");
                Logger.Trace(ex.Message);
                CanSetTracking = false;
                Tracking = false;
                TrackingRate = DriveRates.driveSidereal;
            }

            try {
                CanSetRightAscensionRate = Telescope.CanSetRightAscensionRate;
                RightAscensionRate = Telescope.RightAscensionRate; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement RightAscensionRate");
                Logger.Trace(ex.Message);
                CanSetRightAscensionRate = false;
                RightAscensionRate = double.MinValue;
            }

            try {
                CanSetPierSide = Telescope.CanSetPierSide;
                SideOfPier = Telescope.SideOfPier; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement SideOfPier");
                Logger.Trace(ex.Message);
                CanSetPierSide = false;
                SideOfPier = PierSide.pierUnknown;
            }

            try {
                SiteElevation = Telescope.SiteElevation; /*Settable*/
                SiteLatitude = Telescope.SiteLatitude; /*Settable*/
                SiteLongitude = Telescope.SiteLongitude; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement SiteElevation/SiteLatitude/SiteLongitude");
                Logger.Trace(ex.Message);
                SiteElevation = double.MinValue;
                SiteLatitude = double.MinValue;
                SiteLongitude = double.MinValue;
            }

            try {
                CanSetDeclinationRate = Telescope.CanSetDeclinationRate;
                DeclinationRate = Telescope.DeclinationRate; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement DeclinationRate");
                Logger.Trace(ex.Message);
                CanSetDeclinationRate = false;
                DeclinationRate = double.MinValue;
            }

            try {
                CanSetGuideRates = Telescope.CanSetGuideRates;
                GuideRateDeclination = Telescope.GuideRateDeclination; /*Settable*/
                GuideRateRightAscension = Telescope.GuideRateRightAscension; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement GuideRateDeclination/GuideRateRightAscension");
                Logger.Trace(ex.Message);
                CanSetGuideRates = false;
                GuideRateDeclination = double.MinValue;
                GuideRateRightAscension = double.MinValue;
            }

            try {
                UTCDate = Telescope.UTCDate; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement UTCDate");
                Logger.Trace(ex.Message);
                UTCDate = DateTime.MinValue;
            }

            try {
                DoesRefraction = Telescope.DoesRefraction; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement DoesRefraction");
                Logger.Trace(ex.Message);
                DoesRefraction = false;
            }

            try {
                SlewSettleTime = Telescope.SlewSettleTime; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement SlewSettleTime");
                Logger.Trace(ex.Message);
                SlewSettleTime = short.MinValue;
            }

            try {
                TargetDeclination = Telescope.TargetDeclination; /*Settable*/
                TargetRightAscension = Telescope.TargetRightAscension; /*Settable*/
            }
            catch (Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement TargetDeclination/TargetRightAscension");
                Logger.Trace(ex.Message);
                TargetDeclination = double.MinValue;
                TargetRightAscension = double.MinValue;
            } 
            try {
                AlignmentMode = Telescope.AlignmentMode;
            }
            catch(Exception ex) {
                Logger.Warning("Used Camera AscomDriver does not implement AlignmentMode");
                Logger.Trace(ex.Message);
                AlignmentMode = AlignmentModes.algAltAz;
            }                        
        }

        public void Park() {
            if(Connected && CanPark) {
                try {
                    Telescope.Park();
                    AtPark = true;
                }
                catch (Exception e) {
                    Notification.ShowError(e.Message);
                } finally {
                    
                }
                
                
            }
        }

        public bool Sync(string ra, string dec) {
            return Sync(Utility.Utility.AscomUtil.HMSToHours(ra), Utility.Utility.AscomUtil.DMSToDegrees(dec));            
        }

        public bool Sync(double ra, double dec) {
            bool success = false;
            if (Connected && CanSync) {
                if (Tracking) {

                    try {
                        Telescope.SyncToCoordinates(ra, dec);
                        success = true;
                    }
                    catch (Exception ex) {
                        Notification.ShowError(ex.Message);
                    }
                }
                else {
                    Notification.ShowError("Telescope is not tracking. Sync is only available when tracking!");
                }

            }
            return success;
        }

        public void Unpark() {
            if(Connected && CanUnpark) {
                try {
                    Telescope.Unpark();
                    AtPark = false;
                }
                catch (Exception e) {
                    Notification.ShowError(e.Message);
                } finally {
                    
                }
                
                
            }
        }

        public void Setpark() {
            if(Connected && CanSetPark) {
                try {
                    Telescope.SetPark();
                }
                catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
                
            }
        }

        
        public void MoveAxis(TelescopeAxes axis, double rate) {
            if(Connected) {
                if(CanSlew) {
                    if(!AtPark) {
                        try {
                            Telescope.MoveAxis(axis, rate);
                        }
                        catch (Exception e) {
                            Notification.ShowError(e.Message);
                        }
                    } else {
                        Notification.ShowWarning("Telescope is parked. Cannot slew while parked");
                    }
                } else {
                    Notification.ShowWarning("Telescope cannot slew");
                }
            } else {
                Notification.ShowWarning("Telescope not connected");
            }
        }

        public void SlewToCoordinatesAsync(double ra, double dec) {
            if(Connected && CanSlew && !AtPark) {
                try { 
                    if(!Telescope.Tracking) {
                        Telescope.Tracking = true;
                    }
                    Telescope.SlewToCoordinatesAsync(ra, dec);
                } catch(Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
            
        }

        public void SlewToCoordinates(double ra, double dec) {
            if (Connected && CanSlew && !AtPark) {
                try {
                    if (!Telescope.Tracking) {
                        Telescope.Tracking = true;
                    }
                    Telescope.SlewToCoordinates(ra, dec);
                }
                catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }

        }

        public void SlewToAltAz(double az, double alt) {
            if (Connected && CanSlew && !AtPark) {
                try {
                    Telescope.SlewToAltAz(az, alt);
                }
                catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void SlewToAltAzAsync(double az, double alt) {
            if (Connected && CanSlew && !AtPark) {
                try {
                    Telescope.SlewToAltAzAsync(az, alt);
                }
                catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void Disconnect() {
            if(Telescope != null && Connected) { 
                Connected = false;
                Telescope.Dispose();
                Telescope = null;
                Init();
            }
            
        }
    }
}
