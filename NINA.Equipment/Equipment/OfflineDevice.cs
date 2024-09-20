using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment {
    public class OfflineDevice : IDevice, ICamera, IDome, IFilterWheel, IFlatDevice, IFocuser, IRotator, ISafetyMonitor, ISwitch, ITelescope, IWeatherData {
        public OfflineDevice(string id) {
            Name = id + " (OFFLINE)";
            Id = id;
        }

        public bool HasSetupDialog => false;

        public string Category { get; } = "OFFLINE";

        public string Id { get; private set; }

        public string Name { get; private set; }
        public string DisplayName => Name;

        public bool Connected => false;

        public string Description => string.Empty;

        public string DriverInfo => string.Empty;

        public string DriverVersion => string.Empty;

        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        public async Task<bool> Connect(CancellationToken token) {
            throw new Exception($"Unable to connect Device {Id} as it is not found!");
        }

        public void Disconnect() {
        }

        public void SetupDialog() {
        }

        public IList<string> SupportedActions => new List<string>();

        public bool HasShutter => throw new NotImplementedException();

        public double Temperature => throw new NotImplementedException();

        public double TemperatureSetPoint { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public short BinX { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public short BinY { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string SensorName => throw new NotImplementedException();

        public SensorType SensorType => throw new NotImplementedException();

        public short BayerOffsetX => throw new NotImplementedException();

        public short BayerOffsetY => throw new NotImplementedException();

        public int CameraXSize => throw new NotImplementedException();

        public int CameraYSize => throw new NotImplementedException();

        public double ExposureMin => throw new NotImplementedException();

        public double ExposureMax => throw new NotImplementedException();

        public short MaxBinX => throw new NotImplementedException();

        public short MaxBinY => throw new NotImplementedException();

        public double PixelSizeX => throw new NotImplementedException();

        public double PixelSizeY => throw new NotImplementedException();

        public bool CanSetTemperature => throw new NotImplementedException();

        public bool CoolerOn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public double CoolerPower => throw new NotImplementedException();

        public bool HasDewHeater => throw new NotImplementedException();

        public bool DewHeaterOn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public CameraStates CameraState => throw new NotImplementedException();

        public bool CanSubSample => throw new NotImplementedException();

        public bool EnableSubSample { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int SubSampleX { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int SubSampleY { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int SubSampleWidth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int SubSampleHeight { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool CanShowLiveView => throw new NotImplementedException();

        public bool LiveViewEnabled => throw new NotImplementedException();

        public bool HasBattery => throw new NotImplementedException();

        public int BatteryLevel => throw new NotImplementedException();

        public int BitDepth => throw new NotImplementedException();

        public bool CanSetOffset => throw new NotImplementedException();

        public int Offset { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int OffsetMin => throw new NotImplementedException();

        public int OffsetMax => throw new NotImplementedException();

        public bool CanSetUSBLimit => throw new NotImplementedException();

        public int USBLimit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int USBLimitMin => throw new NotImplementedException();

        public int USBLimitMax => throw new NotImplementedException();

        public int USBLimitStep => throw new NotImplementedException();

        public bool CanGetGain => throw new NotImplementedException();

        public bool CanSetGain => throw new NotImplementedException();

        public int GainMax => throw new NotImplementedException();

        public int GainMin => throw new NotImplementedException();

        public int Gain { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public double ElectronsPerADU => throw new NotImplementedException();

        public IList<string> ReadoutModes => throw new NotImplementedException();

        public short ReadoutMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public short ReadoutModeForSnapImages { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public short ReadoutModeForNormalImages { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<int> Gains => throw new NotImplementedException();

        public AsyncObservableCollection<BinningMode> BinningModes => throw new NotImplementedException();

        public ShutterState ShutterStatus => throw new NotImplementedException();

        public bool DriverCanFollow => throw new NotImplementedException();

        public bool CanSetShutter => throw new NotImplementedException();

        public bool CanSetPark => throw new NotImplementedException();

        public bool CanSetAzimuth => throw new NotImplementedException();

        public bool CanSyncAzimuth => throw new NotImplementedException();

        public bool CanPark => throw new NotImplementedException();

        public bool CanFindHome => throw new NotImplementedException();

        public double Azimuth => throw new NotImplementedException();

        public bool AtPark => throw new NotImplementedException();

        public bool AtHome => throw new NotImplementedException();

        public bool DriverFollowing { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Slewing => throw new NotImplementedException();

        public int[] FocusOffsets => throw new NotImplementedException();

        public string[] Names => throw new NotImplementedException();

        public short Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public AsyncObservableCollection<FilterInfo> Filters => throw new NotImplementedException();

        public CoverState CoverState => throw new NotImplementedException();

        public int MaxBrightness => throw new NotImplementedException();

        public int MinBrightness => throw new NotImplementedException();

        public bool LightOn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Brightness { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string PortName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool SupportsOpenClose => throw new NotImplementedException();

        public bool SupportsOnOff => throw new NotImplementedException();

        public bool IsMoving => throw new NotImplementedException();

        public int MaxIncrement => throw new NotImplementedException();

        public int MaxStep => throw new NotImplementedException();

        int IFocuser.Position => throw new NotImplementedException();

        public double StepSize => throw new NotImplementedException();

        public bool TempCompAvailable => throw new NotImplementedException();

        public bool TempComp { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool CanReverse => throw new NotImplementedException();

        public bool Reverse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Synced => throw new NotImplementedException();

        float IRotator.Position => throw new NotImplementedException();

        public float MechanicalPosition => throw new NotImplementedException();

        float IRotator.StepSize => throw new NotImplementedException();

        public bool IsSafe => throw new NotImplementedException();

        short ISwitch.Id => throw new NotImplementedException();

        public double Value => throw new NotImplementedException();

        public Coordinates Coordinates => throw new NotImplementedException();

        public double RightAscension => throw new NotImplementedException();

        public string RightAscensionString => throw new NotImplementedException();

        public double Declination => throw new NotImplementedException();

        public string DeclinationString => throw new NotImplementedException();

        public double SiderealTime => throw new NotImplementedException();

        public string SiderealTimeString => throw new NotImplementedException();

        public double Altitude => throw new NotImplementedException();

        public string AltitudeString => throw new NotImplementedException();

        public string AzimuthString => throw new NotImplementedException();

        public double HoursToMeridian => throw new NotImplementedException();

        public string HoursToMeridianString => throw new NotImplementedException();

        public double TimeToMeridianFlip => throw new NotImplementedException();

        public string TimeToMeridianFlipString => throw new NotImplementedException();

        public double PrimaryMovingRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double SecondaryMovingRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public PierSide SideOfPier => throw new NotImplementedException();

        public bool CanSetTrackingEnabled => throw new NotImplementedException();

        public bool TrackingEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<TrackingMode> TrackingModes => throw new NotImplementedException();

        public TrackingRate TrackingRate => throw new NotImplementedException();

        public TrackingMode TrackingMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double SiteLatitude { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double SiteLongitude { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double SiteElevation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool CanUnpark => throw new NotImplementedException();

        public Epoch EquatorialSystem => throw new NotImplementedException();

        public bool HasUnknownEpoch => throw new NotImplementedException();

        public Coordinates TargetCoordinates => throw new NotImplementedException();

        public PierSide? TargetSideOfPier => throw new NotImplementedException();

        public double GuideRateRightAscensionArcsecPerSec => throw new NotImplementedException();

        public double GuideRateDeclinationArcsecPerSec => throw new NotImplementedException();

        public bool CanMovePrimaryAxis => throw new NotImplementedException();

        public bool CanMoveSecondaryAxis => throw new NotImplementedException();

        public bool CanSetDeclinationRate => throw new NotImplementedException();

        public bool CanSetRightAscensionRate => throw new NotImplementedException();

        public AlignmentMode AlignmentMode => throw new NotImplementedException();

        public bool CanPulseGuide => throw new NotImplementedException();

        public bool IsPulseGuiding => throw new NotImplementedException();

        public bool CanSetPierSide => throw new NotImplementedException();

        public bool CanSlew => throw new NotImplementedException();

        public DateTime UTCDate => throw new NotImplementedException();

        public double AveragePeriod => throw new NotImplementedException();

        public double CloudCover => throw new NotImplementedException();

        public double DewPoint => throw new NotImplementedException();

        public double Humidity => throw new NotImplementedException();

        public double Pressure => throw new NotImplementedException();

        public double RainRate => throw new NotImplementedException();

        public double SkyBrightness => throw new NotImplementedException();

        public double SkyQuality => throw new NotImplementedException();

        public double SkyTemperature => throw new NotImplementedException();

        public double StarFWHM => throw new NotImplementedException();

        public double WindDirection => throw new NotImplementedException();

        public double WindGust => throw new NotImplementedException();

        public double WindSpeed => throw new NotImplementedException();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SetBinning(short x, short y) {
            throw new NotImplementedException();
        }

        public void StartExposure(CaptureSequence sequence) {
            throw new NotImplementedException();
        }

        public Task WaitUntilExposureIsReady(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StopExposure() {
            throw new NotImplementedException();
        }

        public void AbortExposure() {
            throw new NotImplementedException();
        }

        public Task<IExposureData> DownloadExposure(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StartLiveView(CaptureSequence sequence) {
            throw new NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }

        public Task SlewToAzimuth(double azimuth, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task StopSlewing() {
            throw new NotImplementedException();
        }

        public Task StopShutter() {
            throw new NotImplementedException();
        }

        public Task StopAll() {
            throw new NotImplementedException();
        }

        public Task OpenShutter(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task CloseShutter(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task FindHome(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task Park(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public void SetPark() {
            throw new NotImplementedException();
        }

        public void SyncToAzimuth(double azimuth) {
            throw new NotImplementedException();
        }

        public Task<bool> Open(CancellationToken ct, int delay = 300) {
            throw new NotImplementedException();
        }

        public Task<bool> Close(CancellationToken ct, int delay = 300) {
            throw new NotImplementedException();
        }

        public Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            throw new NotImplementedException();
        }

        public void Halt() {
            throw new NotImplementedException();
        }

        public void Sync(float skyAngle) {
            throw new NotImplementedException();
        }

        public Task<bool> Move(float position, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<bool> MoveAbsolute(float position, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<bool> MoveAbsoluteMechanical(float position, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<bool> Poll() {
            throw new NotImplementedException();
        }

        public IList<(double, double)> GetAxisRates(TelescopeAxes axis) {
            throw new NotImplementedException();
        }

        public Task<bool> MeridianFlip(Coordinates targetCoordinates, CancellationToken token) {
            throw new NotImplementedException();
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            throw new NotImplementedException();
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            throw new NotImplementedException();
        }

        public void Setpark() {
            throw new NotImplementedException();
        }

        public Task<bool> SlewToCoordinates(Coordinates coordinates, CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StopSlew() {
            throw new NotImplementedException();
        }

        public bool Sync(Coordinates coordinates) {
            throw new NotImplementedException();
        }

        public Task Unpark(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void SetCustomTrackingRate(double rightAscensionRate, double declinationRate) {
            throw new NotImplementedException();
        }

        public PierSide DestinationSideOfPier(Coordinates coordinates) {
            throw new NotImplementedException();
        }
    }
}
