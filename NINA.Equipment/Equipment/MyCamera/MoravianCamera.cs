using MoravianCameraSDK;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace NINA.Equipment.Equipment.MyCamera {
    public class MoravianCamera : BaseINPC, ICamera {
        private readonly uint cameraId;
        private readonly IMoravianCameraSDK sdk;
        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;
        private UIntPtr handle = UIntPtr.Zero;

        public MoravianCamera(
            uint cameraId,
            string serialNumber,
            string name,
            string category,
            string driverVersion,
            string firmwareVersion,
            string flashVersion,
            IMoravianCameraSDK sdk,
            IProfileService profileService,
            IExposureDataFactory exposureDataFactory) {
            this.cameraId = cameraId;
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            this.sdk = sdk;

            Name = name;
            Category = category;
            DriverVersion = driverVersion;
            FirmwareVersion = firmwareVersion;
            FlashVersion = flashVersion;
            Description = serialNumber;
            DriverInfo = $"Native driver implementation for {category} Cameras";
            SerialNumber = serialNumber;
            Id = $"MoravianInstruments_{serialNumber}";
            DisplayName = $"{category} - {name} ({(serialNumber.Length > 8 ? serialNumber[^8..] : serialNumber)})";
        }

        public bool HasSetupDialog => sdk is IMoravianConfigurable;
        public string SerialNumber { get; }
        public string Id { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Description { get; }
        public string DriverInfo { get; }
        public string DriverVersion { get; }
        public string FirmwareVersion { get; }
        public string FlashVersion { get; }

        public bool Connected => handle != UIntPtr.Zero && GetBoolSafe(MoravianBooleanParameter.gbpConnected);
        public IList<string> SupportedActions => new List<string>();

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() =>
            {
                try {
                    token.ThrowIfCancellationRequested();

                    if (Connected)
                        return true;

                    handle = sdk.Initialize(cameraId);
                    if (handle == UIntPtr.Zero) { 
                        return false;
                    }

                    if (!Connected) {
                        throw new InvalidOperationException("Failed to connect to Moravian camera");
                    }

                    Initialize();

                    RaisePropertyChanged(nameof(Connected));
                    RaiseAllPropertiesChanged();
                    return true;
                } catch (Exception ex) {
                    if (Connected) {
                        Disconnect();
                    }
                    Logger.Error(ex);
                    return false;
                }
            }, token);
        }

        public void Disconnect() {
            try {
                if (!Connected)
                    return;

                try { AbortExposure(); } catch { }

                sdk.Release(handle);
                handle = UIntPtr.Zero;

                RaisePropertyChanged(nameof(Connected));
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public void SetupDialog() {
            if (sdk is IMoravianConfigurable configurable) {
                var hwnd = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
                configurable.Configure(handle, hwnd);
            }
        }

        public string Action(string actionName, string actionParameters) => throw new NotImplementedException();
        public string SendCommandString(string command, bool raw = true) => throw new NotImplementedException();
        public bool SendCommandBool(string command, bool raw = true) => throw new NotImplementedException();
        public void SendCommandBlind(string command, bool raw = true) => throw new NotImplementedException();

        protected void Initialize() {
            BinningModes = new AsyncObservableCollection<BinningMode>();

            MaxBinX = (short)GetInt(MoravianIntegerParameter.gipMaxBinningX);
            MaxBinY = (short)GetInt(MoravianIntegerParameter.gipMaxBinningY);

            int max = Math.Max(1, (int)Math.Min(MaxBinX, MaxBinY));
            for (int b = 1; b <= max; b++)
                BinningModes.Add(new BinningMode((short)b, (short)b));

            CameraXSize = GetInt(MoravianIntegerParameter.gipChipW);
            CameraYSize = GetInt(MoravianIntegerParameter.gipChipD);

            int pxW = GetIntSafe(MoravianIntegerParameter.gipPixelW, -1);
            int pxH = GetIntSafe(MoravianIntegerParameter.gipPixelD, -1);
            PixelSizeX = (pxW <= 0) ? double.NaN : (pxW / 1000.0);
            PixelSizeY = (pxH <= 0) ? double.NaN : (pxH / 1000.0);

            bool isColor = GetBoolSafe(MoravianBooleanParameter.gbpRGB) || GetBoolSafe(MoravianBooleanParameter.gbpCMY) || GetBoolSafe(MoravianBooleanParameter.gbpCMYG);
            SensorType = isColor ? SensorType.Color : SensorType.Monochrome;

            CanGetTemperature = true;
            CanSetTemperature = GetBoolSafe(MoravianBooleanParameter.gbpCooler);
            HasDewHeater = GetBoolSafe(MoravianBooleanParameter.gbpWindowHeating);

            HasShutter = GetBoolSafe(MoravianBooleanParameter.gbpShutter);

            CanSubSample = GetBoolSafe(MoravianBooleanParameter.gbpSubFrame);

            ReadoutModes = EnumerateReadoutModesUntilFailure();

            ReadoutModeForNormalImages = 0;
            ReadoutModeForSnapImages = 0;

            BinX = 1;
            BinY = 1;

            EnableSubSample = false;
            SubSampleX = 0;
            SubSampleY = 0;
            SubSampleWidth = CameraXSize;
            SubSampleHeight = CameraYSize;

            _gain = 0;

            HasFan = GetBoolSafe(MoravianBooleanParameter.gbpFan);            
            MaxFanSpeed = HasFan ? (byte)0 : (byte)GetIntSafe(MoravianIntegerParameter.gipMaxFan, 0);
            FanSpeed = MaxFanSpeed;
        }

        public bool HasFan { get; private set; }
        public byte MaxFanSpeed { get; private set; }
        public byte FanSpeed {
            get {
                return field;
            }
            set {
                if (!HasFan) return;
                if (value < 0) value = 0;
                if (value > MaxFanSpeed) value = (byte)MaxFanSpeed;
                if (sdk.SetFan(handle, value)) {
                    field = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IList<string> EnumerateReadoutModesUntilFailure() {
            var list = new List<string>();

            // If driver reports no readout modes, keep one default entry.
            if (!GetBoolSafe(MoravianBooleanParameter.gbpReadModes)) {
                list.Add("Default");
                return list;
            }

            for (int i = 0; ; i++) {
                var sb = new StringBuilder(256);
                if (!sdk.EnumerateReadModes(handle, i, sb.Capacity, sb)) {
                    break;
                }

                var s = sb.ToString();
                list.Add(string.IsNullOrWhiteSpace(s) ? $"Mode {i}" : s);
            }

            if (list.Count == 0)
                list.Add("Default");

            return list;
        }

        public int BitDepth {
            get {
                bool bitScaling = profileService.ActiveProfile.CameraSettings.BitScaling;
                if (bitScaling) return 16;

                int v = GetIntSafe(MoravianIntegerParameter.gipMaxPossiblePixelValue, 0);
                if (v <= 0) return 16;
                int bits = 0;
                while (v > 0) { bits++; v >>= 1; }
                return bits;
            }
        }

        public SensorType SensorType { get; private set; }
        public int CameraXSize { get; private set; }
        public int CameraYSize { get; private set; }
        public double PixelSizeX { get; private set; }
        public double PixelSizeY { get; private set; }        
        public double ExposureMin => GetIntSafe(MoravianIntegerParameter.gipMinimalExposure, 0) / 1_000_000.0;
        public double ExposureMax => Math.Ceiling(GetIntSafe(MoravianIntegerParameter.gipMaximalExposure, 0) / 1000.0);

        private short binX = 1;
        public short BinX {
            get => binX;
            set {
                if (value <= 0) value = 1;
                if (value > MaxBinX) value = MaxBinX;
                binX = value;
                RaisePropertyChanged();
            }
        }

        private short binY = 1;
        public short BinY {
            get => binY;
            set {
                if (value <= 0) value = 1;
                if (value > MaxBinY) value = MaxBinY;
                binY = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; private set; } = new();
        public short MaxBinX { get; private set; }
        public short MaxBinY { get; private set; }
        public bool CanGetGain => GetBoolSafe(MoravianBooleanParameter.gbpGain);
        public bool CanSetGain => GetBoolSafe(MoravianBooleanParameter.gbpGain);
        public int GainMax => GetIntSafe(MoravianIntegerParameter.gipMaxGain, 0);
        public int GainMin => 0;

        private int _gain;

        public int Gain {
            get => _gain;
            set {
                if (!CanSetGain) return;

                if (value < GainMin) value = GainMin;
                if (value > GainMax) value = GainMax;

                sdk.SetGain(handle, (uint)value);
                _gain = value;

                RaisePropertyChanged();
            }
        }

        private IList<string> readoutModes = new List<string> { "Default" };
        public IList<string> ReadoutModes {
            get => readoutModes;
            private set { readoutModes = value; RaisePropertyChanged(); }
        }

        private short currentReadoutMode = 0;

        public short ReadoutMode {
            get => currentReadoutMode;
            set {
                currentReadoutMode = ClampReadoutMode(value);
                sdk.SetReadMode(handle, currentReadoutMode);
                RaisePropertyChanged();
            }
        }

        private short _readoutModeForNormalImages = 0;
        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set { _readoutModeForNormalImages = ClampReadoutMode(value); RaisePropertyChanged(); }
        }

        private short _readoutModeForSnapImages = 0;
        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set { _readoutModeForSnapImages = ClampReadoutMode(value); RaisePropertyChanged(); }
        }

        private short ClampReadoutMode(short value) {
            if (ReadoutModes == null || ReadoutModes.Count <= 0) return 0;
            if (value < 0) return 0;
            if (value >= ReadoutModes.Count) return 0;
            return value;
        }

        public void SetBinning(short x, short y) {
            BinX = x;
            BinY = y;
        }

        private Task<DateTime>
            exposureTask;
        private CancellationTokenSource exposureTaskCts;
        private int exposureTaskX;
        private int exposureTaskY;
        private int exposureTaskWidth;
        private int exposureTaskHeight;
        private double exposureTaskTime;
        private DateTime lastExposureStartTime;
        private DateTime lastExposureEndTime;

        public void StartExposure(CaptureSequence sequence) {
            var isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;
            ReadoutMode = isSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

            int x, y, w, h;
            if (EnableSubSample && CanSubSample) {
                UpdateSubSampleArea();
                x = SubSampleX / BinX;
                y = SubSampleY / BinY;
                w = SubSampleWidth / BinX;
                h = SubSampleHeight / BinY;
            } else {
                x = 0; y = 0; w = CameraXSize / BinX; h = CameraYSize / BinY;
            }

            sdk.SetBinning(handle, (uint)BinX, (uint)BinY);

            exposureTaskX = x;
            exposureTaskY = y;
            exposureTaskWidth = w;
            exposureTaskHeight = h;
            exposureTaskTime = sequence.ExposureTime;
            lastExposureStartTime = DateTime.UtcNow;

            var useShutter = (sequence.IsLightSequence() && HasShutter);

            if (sdk is IMoravianComputerTimingExposure computerTimingExposure) {
                exposureTaskCts = new CancellationTokenSource();
                exposureTask = TakeExposureTask(computerTimingExposure, handle, useShutter, TimeSpan.FromSeconds(exposureTaskTime), exposureTaskCts.Token);
            } else if (sdk is IMoravianCameraTimingExposure asyncExposure) {                
                if (!asyncExposure.StartExposure(handle, sequence.ExposureTime, useShutter, x, y, w, h)) {
                    throw new Exception($"{Category} - Failed to trigger camera exposure");
                }
            } else {
                throw new NotImplementedException();
            }
        }

        public static async Task<DateTime> TakeExposureTask(
                IMoravianComputerTimingExposure computerTimingExposure,
                nuint cameraHandle, 
                bool useShutter,
                TimeSpan duration,
                CancellationToken cancellationToken = default) {
            try {
                if (duration <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(duration));

                computerTimingExposure.BeginExposure(cameraHandle, useShutter);

                // Start monotonic clock
                long start = Stopwatch.GetTimestamp();
                long target = start + ToStopwatchTicks(duration);
                var tail = TimeSpan.FromMilliseconds(5);

                // Coarse wait loop until we're within 'tail' of the deadline.
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();

                    long now = Stopwatch.GetTimestamp();
                    long remainingTicks = target - now;
                    if (remainingTicks <= 0) { 
                        break;
                    }

                    var remaining = FromStopwatchTicks(remainingTicks);
                    if (remaining <= tail) { 
                        break;
                    }

                    // Delay the "safe" portion, but cap to remain responsive to cancellation.
                    var delay = remaining - tail;
                    if (delay > TimeSpan.FromMilliseconds(50)) { 
                        delay = TimeSpan.FromMilliseconds(50);
                    }

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                // Fine-grained wait to hit the deadline.
                var sw = new SpinWait();
                while (Stopwatch.GetTimestamp() < target) {
                    cancellationToken.ThrowIfCancellationRequested();
                    sw.SpinOnce();
                }
            } catch(OperationCanceledException) {
                // Abort
                computerTimingExposure.EndExposure(cameraHandle, useShutter, true);
                return DateTime.UtcNow;
            }

            // Stop exposure as close as possible to the target time
            var stoptime = DateTime.UtcNow;
            computerTimingExposure.EndExposure(cameraHandle, useShutter, false);
            return stoptime;
        }
        private static long ToStopwatchTicks(TimeSpan t)
            => (long)(t.TotalSeconds * Stopwatch.Frequency);

        private static TimeSpan FromStopwatchTicks(long ticks)
            => TimeSpan.FromSeconds((double)ticks / Stopwatch.Frequency);

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            if (sdk is IMoravianCameraTimingExposure asyncExposure) { 
                using (token.Register(() => AbortExposure())) {
                    while (true) {
                        token.ThrowIfCancellationRequested();

                        bool ready;
                        var rc = asyncExposure.ImageReady(handle, out ready);
                        if (rc && ready)
                            break;

                        await CoreUtil.Wait(TimeSpan.FromMilliseconds(10), token);
                    }
                    lastExposureEndTime = DateTime.UtcNow;
                }
            } else {
                await exposureTask;
            }
        }

        public void StopExposure() {
            if (sdk is IMoravianCameraTimingExposure asyncExposure) {
                try { asyncExposure.AbortExposure(handle, downloadFlag: true); } catch { }
            } else if (sdk is IMoravianComputerTimingExposure computerTimingExposure) {
                try { exposureTaskCts?.Cancel(); } catch { }
            }
        }

        public void AbortExposure() {
            if (sdk is IMoravianCameraTimingExposure asyncExposure) {
                try { asyncExposure.AbortExposure(handle, downloadFlag: false); } catch { }
            } else if (sdk is IMoravianComputerTimingExposure computerTimingExposure) {
                try { exposureTaskCts?.Cancel(); } catch { }
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            ushort[] data = null;
            int pixelCount = exposureTaskWidth * exposureTaskHeight;
            uint bufferLength = (uint)(pixelCount * sizeof(ushort));
            data = new ushort[pixelCount];

            if (sdk is IMoravianCameraTimingExposure asyncExposure) {
                using (var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                    try {
                        downloadCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(60, profileService.ActiveProfile.CameraSettings.Timeout)));

                        asyncExposure.ImageReady(handle, out var ready);
                        if (!ready)
                            await WaitUntilExposureIsReady(downloadCts.Token);

                        if (LiveViewEnabled) {
                            var rc = asyncExposure.ReadImageExposure(handle, bufferLength, data);
                            if (!rc) data = null;
                        } else {
                            var rc = asyncExposure.ReadImage(handle, bufferLength, data);
                            if (!rc) data = null;
                        }
                    } catch {
                        data = null;
                    }
                }
            } else if (sdk is IMoravianComputerTimingExposure computerTimingExposure) {
                lastExposureEndTime = await exposureTask;
                var rc = computerTimingExposure.GetImage(handle, exposureTaskX, exposureTaskY, exposureTaskWidth, exposureTaskHeight, bufferLength, data);
                if (!rc) data = null;
            } else {
                throw new NotImplementedException();
            }
                        
            if (data == null)
                return null;

            int maxPixelValue = GetIntSafe(MoravianIntegerParameter.gipMaxPossiblePixelValue, ushort.MaxValue);
            int nativeBitDepth = 32 - BitOperations.LeadingZeroCount((uint)maxPixelValue);

            var bitScaling = this.profileService.ActiveProfile.CameraSettings.BitScaling;
            if (bitScaling) {
                int shift = 16 - nativeBitDepth;
                if (bitScaling && shift != 0) {
                    ImageUtility.BitShiftLeftInPlace(data, shift);
                }
            }

            var metaData = new ImageMetaData();
            metaData.FromCamera(this);
            metaData.Image.SetExposureTimes(lastExposureStartTime, lastExposureEndTime);
            
            // Extract GPS timestamps if availabe
            if (sdk is IMoravianGPS gps && sdk.GetBooleanParameter(handle, MoravianBooleanParameter.gbpGPS, out var value) && value) {
                var getGpsDataSuccess = gps.GetGPSData(handle, out var lat, out var lon, out var msl, out _, out _, out _, out _, out _, out _, out var satellites, out var fix);
                var getImageTimeStampSuccess = gps.GetImageTimeStamp(handle, out var year, out var month, out var day, out var hour, out var minute, out double second);

                if (getGpsDataSuccess && getImageTimeStampSuccess) {
                    static DateTime BuildUtcTimestamp(
                        int y, int mo, int d, int h, int mi, double s) {
                        // Defensive handling
                        if (double.IsNaN(s) || double.IsInfinity(s) || s < 0) s = 0;

                        int wholeSeconds = (int)Math.Floor(s);
                        double frac = s - wholeSeconds;

                        // Round fractional seconds to ticks (100 ns)
                        long fracTicks = (long)Math.Round(
                            frac * TimeSpan.TicksPerSecond,
                            MidpointRounding.AwayFromZero);

                        // Normalize if rounding overflowed
                        if (fracTicks >= TimeSpan.TicksPerSecond) {
                            wholeSeconds += 1;
                            fracTicks -= TimeSpan.TicksPerSecond;
                        }

                        // Let DateTime.AddSeconds handle overflow across minutes/hours/days.
                        var dt = new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc)
                            .AddSeconds(wholeSeconds)
                            .AddTicks(fracTicks);

                        return dt;
                    }

                    var exposureTime = TimeSpan.FromSeconds(Math.Max(ExposureMin, exposureTaskTime));
                    var exposureStartUtc = BuildUtcTimestamp(year, month, day, hour, minute, second);
                    var exposureEndUtc = exposureStartUtc + exposureTime;

                    metaData.Image.SetExposureTimes(exposureStartUtc, exposureEndUtc);

                    metaData.GenericHeaders.Add(new StringMetaDataHeader("GPS_STAT", $"{satellites} sats", "GPS status"));
                    metaData.GenericHeaders.Add(new DoubleMetaDataHeader("GPS_ALT", msl, "[m] Altitude"));
                    metaData.GenericHeaders.Add(new DateTimeMetaDataHeader("GPS_EUTC", exposureEndUtc, "End shutter time"));
                    metaData.GenericHeaders.Add(new DateTimeMetaDataHeader("GPS_ET", exposureEndUtc, "End shutter time"));
                    metaData.GenericHeaders.Add(new DoubleMetaDataHeader("GPS_LAT", lat, "[deg] Latitude"));
                    metaData.GenericHeaders.Add(new DoubleMetaDataHeader("GPS_LON", lon, "[deg] Longitude"));
                    metaData.GenericHeaders.Add(new DateTimeMetaDataHeader("GPS_SUTC", exposureStartUtc, "Start shutter time"));
                    metaData.GenericHeaders.Add(new DateTimeMetaDataHeader("GPS_ST", exposureStartUtc, "Start shutter time"));
                    metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_W", exposureTaskWidth, "Width"));
                    metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_H", exposureTaskHeight, "Height"));
                    metaData.GenericHeaders.Add(new DoubleMetaDataHeader("GPS_EXPU", exposureTime.TotalMicroseconds, "[us] Exposure"));
                    int lineTimePicoSeconds = GetIntSafe(MoravianIntegerParameter.gipLineTime, int.MinValue);
                    if (lineTimePicoSeconds >= 0) {
                        metaData.GenericHeaders.Add(new DoubleMetaDataHeader("GPS_LP", lineTimePicoSeconds / 1000.0, "[ns] linePeriod"));
                    }
                }
            }

            return exposureDataFactory.CreateImageArrayExposureData(
                input: data,
                width: exposureTaskWidth,
                height: exposureTaskHeight,
                bitDepth: this.BitDepth,
                isBayered: SensorType != SensorType.Monochrome,
                metaData: metaData);
        }

        public bool HasDewHeater { get; private set; }

        private bool dewHeaterOn;
        public bool DewHeaterOn {
            get => dewHeaterOn;
            set {
                if (!HasDewHeater) return;
                dewHeaterOn = value;

                var rc = sdk.SetWindowHeating(handle, value);
                if (rc) RaisePropertyChanged();
            }
        }

        public bool CanGetTemperature { get; private set; }
        public bool CanSetTemperature { get; private set; }

        private bool coolerOn;
        public bool CoolerOn {
            get => coolerOn;
            set {
                // cxusb wrapper lacks explicit on/off. Keep as local semantic flag.
                if (!CanSetTemperature) return;
                coolerOn = value;
                RaisePropertyChanged();
            }
        }

        public double CoolerPower {
            get {
                if (!CanSetTemperature) return double.NaN;

                float p;
                if (!sdk.GetValue(handle, MoravianValueParameter.gvPowerUtilization, out p))
                    return double.NaN;

                return p * 100.0;
            }
        }

        public double Temperature {
            get {
                if (!CanGetTemperature) return double.NaN;
                float t;
                return sdk.GetValue(handle, MoravianValueParameter.gvChipTemperature, out t) ? t : double.NaN;
            }
        }

        private double temperatureSetPoint = double.NaN;

        public double TemperatureSetPoint {
            get => temperatureSetPoint;
            set {
                if (!CanSetTemperature) return;

                temperatureSetPoint = value;
                sdk.SetTemperature(handle, (float)value);
                RaisePropertyChanged();
            }
        }

        public bool CanSubSample { get; private set; }
        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        public void UpdateSubSampleArea() {
            SubSampleX = Math.Clamp(SubSampleX, 0, Math.Max(0, CameraXSize - 1));
            SubSampleY = Math.Clamp(SubSampleY, 0, Math.Max(0, CameraYSize - 1));
            SubSampleWidth = Math.Clamp(SubSampleWidth, 1, Math.Max(1, CameraXSize - SubSampleX));
            SubSampleHeight = Math.Clamp(SubSampleHeight, 1, Math.Max(1, CameraYSize - SubSampleY));
        }
        public bool CanShowLiveView => false;
        public bool LiveViewEnabled { get; set; } = false;
        public void StartLiveView(CaptureSequence sequence) {
            var isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;
            ReadoutMode = isSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

            int x, y, w, h;
            if (EnableSubSample && CanSubSample) {
                UpdateSubSampleArea();
                x = SubSampleX / BinX;
                y = SubSampleY / BinY;
                w = SubSampleWidth / BinX;
                h = SubSampleHeight / BinY;
            } else {
                x = 0; y = 0; w = CameraXSize / BinX; h = CameraYSize / BinY;
            }

            sdk.SetBinning(handle, (uint)BinX, (uint)BinY);

            exposureTaskX = x;
            exposureTaskY = y;
            exposureTaskWidth = w;
            exposureTaskHeight = h;
            exposureTaskTime = sequence.ExposureTime;
            lastExposureStartTime = DateTime.UtcNow;

            var useShutter = (sequence.IsLightSequence() && HasShutter);

            if (sdk is IMoravianComputerTimingExposure computerTimingExposure) {
                throw new InvalidOperationException($"{Category} - Live view not supported for {DisplayName}");
            } else if (sdk is IMoravianCameraTimingExposure asyncExposure) {
                if (!asyncExposure.StartExposure(handle, sequence.ExposureTime, useShutter, x, y, w, h)) {
                    throw new Exception($"{Category} - Failed to trigger camera exposure");
                }
            } else {
                throw new NotImplementedException();
            }
            LiveViewEnabled = true;
        }
        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            lastExposureStartTime = DateTime.UtcNow;
            return DownloadExposure(token);
        }
        public void StopLiveView() {
            AbortExposure();
            LiveViewEnabled = false;
        }

        public bool HasFilterWheel() {
            return GetBoolSafe(MoravianBooleanParameter.gbpFilters);
        }

        public void SetFilter(uint index) {
            sdk.SetFilter(handle, index);
        }

        public void ReinitFilterWheel() {
            sdk.ReinitFilterWheel(handle);
        }

        public ObserveAllCollection<FilterInfo> GetFilters() {
            var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            var positions = GetIntSafe(MoravianIntegerParameter.gipFilters, 0);
            var filters = new FilterManager().SyncFiltersWithPositions(filtersList, positions);
            return filters;
        }

        #region Unsupported
        public CameraStates CameraState => CameraStates.NoState;
        public string SensorName => string.Empty;

        public bool HasShutter { get; private set; }
        public bool HasBattery => false;
        public int BatteryLevel => -1;
        public double ElectronsPerADU => double.NaN;
        public short BayerOffsetX { get; } = 0;
        public short BayerOffsetY { get; } = 0;
        public IList<int> Gains => new List<int>();
        public bool CanSetOffset => false;
        public int Offset { get => -1; set => throw new NotImplementedException(); }
        public int OffsetMin => -1;
        public int OffsetMax => -1;
        public bool CanSetUSBLimit => false;
        public int USBLimit { get => -1; set => throw new NotImplementedException(); }
        public int USBLimitMin => -1;
        public int USBLimitMax => -1;
        public int USBLimitStep => -1;

        #endregion

        private int GetInt(MoravianIntegerParameter index) {
            int v;
            var rc = sdk.GetIntegerParameter(handle, index, out v);
            if (!rc) throw new InvalidOperationException($"GetIntegerParameter({index}) failed rc={rc}");
            return v;
        }

        private int GetIntSafe(MoravianIntegerParameter index, int fallback) {
            try { return GetInt(index); } catch { return fallback; }
        }

        private bool GetBoolSafe(MoravianBooleanParameter index) {
            try {
                var rc = sdk.GetBooleanParameter(handle, index, out var v);
                return rc && v;
            } catch { return false; }
        }
    }
}
