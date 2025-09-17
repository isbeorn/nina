using ASCOM.Alpaca.Discovery;
using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MySwitch.Ascom;
using NINA.Equipment.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyTelescope {
    internal class AlpacaDirectTelescope : ITelescope, INotifyPropertyChanged {

        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private AscomTelescope device;

        private void SetDevice(AscomTelescope newDevice) {
            if (ReferenceEquals(device, newDevice)) return;

            if (device != null)
                device.PropertyChanged -= OnDevicePropertyChanged;

            device = newDevice;

            if (device != null)
                device.PropertyChanged += OnDevicePropertyChanged;
        }

        private void OnDevicePropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(IDevice.Name) || e.PropertyName == "Name") {
                RaisePropertyChanged(nameof(Name));
            }
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(IDevice.DisplayName) || e.PropertyName == "DisplayName") {
                RaisePropertyChanged(nameof(DisplayName));
            }

            RaisePropertyChanged(e.PropertyName);
        }
        private IProfileService profileService;
        private AlpacaDirectSettings settings;

        public AlpacaDirectTelescope(IProfileService profileService) {
            this.profileService = profileService;
            settings = new AlpacaDirectSettings(new NINA.Profile.PluginOptionsAccessor(profileService, Guid.Parse(Id)));
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasSetupDialog => true;

        public string Id => "F98BB2D7-A53B-456C-A3B0-CC1CC0F3A40E";

        public string Name => device?.Name ?? "Alpaca Mount - Static IP";

        public string DisplayName => (device?.Name ?? "Alpaca Mount - Static IP") + $" @ {settings.IpAddress} #{settings.DeviceNumber}";

        public string Category => "ASCOM Alpaca";

        public async Task<bool> Connect(CancellationToken token) {
            var meta = new AscomDevice();
            meta.ServiceType = settings.ServiceType;
            meta.IpAddress = settings.IpAddress;
            meta.IpPort = settings.Port;
            meta.AlpacaDeviceNumber = settings.DeviceNumber;
            SetDevice(new AscomTelescope(meta, profileService));
            var connect = await ((IDevice)device).Connect(token);
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(DisplayName));
            return connect;
        }

        private IWindowService windowService = new WindowService();

        public void SetupDialog() {
            windowService.ShowDialog(settings, "ASCOM Alpaca IP Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }

        public string Action(string actionName, string actionParameters) {
            return ((IDevice)device).Action(actionName, actionParameters);
        }

        public bool Connected => ((IDevice)device).Connected;

        public string Description => ((IDevice)device).Description;

        public string DriverInfo => ((IDevice)device).DriverInfo;

        public string DriverVersion => ((IDevice)device).DriverVersion;

        public Coordinates Coordinates => ((ITelescope)device).Coordinates;

        public double RightAscension => ((ITelescope)device).RightAscension;

        public string RightAscensionString => ((ITelescope)device).RightAscensionString;

        public double Declination => ((ITelescope)device).Declination;

        public string DeclinationString => ((ITelescope)device).DeclinationString;

        public double SiderealTime => ((ITelescope)device).SiderealTime;

        public string SiderealTimeString => ((ITelescope)device).SiderealTimeString;

        public double Altitude => ((ITelescope)device).Altitude;

        public string AltitudeString => ((ITelescope)device).AltitudeString;

        public double Azimuth => ((ITelescope)device).Azimuth;

        public string AzimuthString => ((ITelescope)device).AzimuthString;

        public double HoursToMeridian => ((ITelescope)device).HoursToMeridian;

        public string HoursToMeridianString => ((ITelescope)device).HoursToMeridianString;

        public double TimeToMeridianFlip => ((ITelescope)device).TimeToMeridianFlip;

        public string TimeToMeridianFlipString => ((ITelescope)device).TimeToMeridianFlipString;

        public double PrimaryMovingRate { get => ((ITelescope)device).PrimaryMovingRate; set => ((ITelescope)device).PrimaryMovingRate = value; }
        public double SecondaryMovingRate { get => ((ITelescope)device).SecondaryMovingRate; set => ((ITelescope)device).SecondaryMovingRate = value; }

        public PierSide SideOfPier => ((ITelescope)device).SideOfPier;

        public bool CanSetTrackingEnabled => ((ITelescope)device).CanSetTrackingEnabled;

        public bool TrackingEnabled { get => ((ITelescope)device).TrackingEnabled; set => ((ITelescope)device).TrackingEnabled = value; }

        public IList<TrackingMode> TrackingModes => ((ITelescope)device).TrackingModes;

        public TrackingRate TrackingRate => ((ITelescope)device).TrackingRate;

        public TrackingMode TrackingMode { get => ((ITelescope)device).TrackingMode; set => ((ITelescope)device).TrackingMode = value; }
        public double SiteLatitude { get => ((ITelescope)device).SiteLatitude; set => ((ITelescope)device).SiteLatitude = value; }
        public double SiteLongitude { get => ((ITelescope)device).SiteLongitude; set => ((ITelescope)device).SiteLongitude = value; }
        public double SiteElevation { get => ((ITelescope)device).SiteElevation; set => ((ITelescope)device).SiteElevation = value; }

        public bool AtHome => ((ITelescope)device).AtHome;

        public bool CanFindHome => ((ITelescope)device).CanFindHome;

        public bool AtPark => ((ITelescope)device).AtPark;

        public bool CanPark => ((ITelescope)device).CanPark;

        public bool CanUnpark => ((ITelescope)device).CanUnpark;

        public bool CanSetPark => ((ITelescope)device).CanSetPark;

        public Epoch EquatorialSystem => ((ITelescope)device).EquatorialSystem;

        public bool HasUnknownEpoch => ((ITelescope)device).HasUnknownEpoch;

        public Coordinates TargetCoordinates => ((ITelescope)device).TargetCoordinates;

        public PierSide? TargetSideOfPier => ((ITelescope)device).TargetSideOfPier;

        public bool Slewing => ((ITelescope)device).Slewing;

        public double GuideRateRightAscensionArcsecPerSec => ((ITelescope)device).GuideRateRightAscensionArcsecPerSec;

        public double GuideRateDeclinationArcsecPerSec => ((ITelescope)device).GuideRateDeclinationArcsecPerSec;

        public bool CanMovePrimaryAxis => ((ITelescope)device).CanMovePrimaryAxis;

        public bool CanMoveSecondaryAxis => ((ITelescope)device).CanMoveSecondaryAxis;

        public bool CanSetDeclinationRate => ((ITelescope)device).CanSetDeclinationRate;

        public bool CanSetRightAscensionRate => ((ITelescope)device).CanSetRightAscensionRate;

        public AlignmentMode AlignmentMode => ((ITelescope)device).AlignmentMode;

        public bool CanPulseGuide => ((ITelescope)device).CanPulseGuide;

        public bool IsPulseGuiding => ((ITelescope)device).IsPulseGuiding;

        public bool CanSetPierSide => ((ITelescope)device).CanSetPierSide;

        public bool CanSlew => ((ITelescope)device).CanSlew;

        public bool CanSlewAltAz => ((ITelescope)device).CanSlewAltAz;

        public DateTime UTCDate => ((ITelescope)device).UTCDate;

        public IList<string> SupportedActions => ((IDevice)device).SupportedActions;

        public PierSide DestinationSideOfPier(Coordinates coordinates) {
            return ((ITelescope)device).DestinationSideOfPier(coordinates);
        }

        public void Disconnect() {
            ((IDevice)device).Disconnect();
        }

        public Task FindHome(CancellationToken token) {
            return ((ITelescope)device).FindHome(token);
        }

        public IList<(double, double)> GetAxisRates(TelescopeAxes axis) {
            return ((ITelescope)device).GetAxisRates(axis);
        }

        public Task<bool> MeridianFlip(Coordinates targetCoordinates, CancellationToken token) {
            return ((ITelescope)device).MeridianFlip(targetCoordinates, token);
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            ((ITelescope)device).MoveAxis(axis, rate);
        }

        public Task Park(CancellationToken token) {
            return ((ITelescope)device).Park(token);
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            ((ITelescope)device).PulseGuide(direction, duration);
        }

        public void SendCommandBlind(string command, bool raw = true) {
            ((IDevice)device).SendCommandBlind(command, raw);
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return ((IDevice)device).SendCommandBool(command, raw);
        }

        public string SendCommandString(string command, bool raw = true) {
            return ((IDevice)device).SendCommandString(command, raw);
        }

        public void SetCustomTrackingRate(double rightAscensionRate, double declinationRate) {
            ((ITelescope)device).SetCustomTrackingRate(rightAscensionRate, declinationRate);
        }

        public void Setpark() {
            ((ITelescope)device).Setpark();
        }

        public Task<bool> SlewToAltAz(TopocentricCoordinates coordinates, CancellationToken token) {
            return ((ITelescope)device).SlewToAltAz(coordinates, token);
        }

        public Task<bool> SlewToCoordinates(Coordinates coordinates, CancellationToken token) {
            return ((ITelescope)device).SlewToCoordinates(coordinates, token);
        }

        public void StopSlew() {
            ((ITelescope)device).StopSlew();
        }

        public bool Sync(Coordinates coordinates) {
            return ((ITelescope)device).Sync(coordinates);
        }

        public Task Unpark(CancellationToken token) {
            return ((ITelescope)device).Unpark(token);
        }
    }
}
