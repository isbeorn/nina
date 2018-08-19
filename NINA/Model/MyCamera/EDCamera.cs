using ASCOM.DeviceInterface;
using EDSDKLib;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using NINA.Utility.RawConverter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.MyCamera {

    internal class EDCamera : BaseINPC, ICamera {

        public EDCamera(IntPtr cam, EDSDK.EdsDeviceInfo info, IProfileService profileService) {
            this.profileService = profileService;
            _cam = cam;
            Id = info.szDeviceDescription;
            Name = info.szDeviceDescription;
        }

        private IProfileService profileService;

        private IntPtr _cam;

        public bool HasShutter {
            get {
                return true;
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                return _connected;
            }
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public double Temperature {
            get {
                return double.NaN;
            }
        }

        public double TemperatureSetPoint {
            get {
                return double.NaN;
            }
            set {
            }
        }

        public short BinX {
            get {
                return 1;
            }
            set {
            }
        }

        public bool CanSubSample {
            get {
                return false;
            }
        }

        public short BinY {
            get {
                return 1;
            }
            set {
            }
        }

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

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

        public string Description {
            get {
                return "Canon Camera";
            }
        }

        public string DriverInfo {
            get {
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                string property = string.Empty;
                if (Connected) {
                    if (HasError(EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_FirmwareVersion, 0, out property))) {
                        return string.Empty;
                    }
                }
                return property;
            }
        }

        public bool CanShowLiveView {
            get {
                return true;
            }
        }

        public string SensorName {
            get {
                return string.Empty;
            }
        }

        public SensorType SensorType {
            get {
                return SensorType.RGGB;
            }
        }

        public int CameraXSize {
            get {
                return -1;
            }
        }

        public int CameraYSize {
            get {
                return -1;
            }
        }

        public double ExposureMin {
            get {
                return this.ShutterSpeeds.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }
        }

        public double ExposureMax {
            get {
                return double.PositiveInfinity;
            }
        }

        public short MaxBinX {
            get {
                return 1;
            }
        }

        public short MaxBinY {
            get {
                return 1;
            }
        }

        public double PixelSizeX {
            get {
                return -1;
            }
        }

        public double PixelSizeY {
            get {
                return -1;
            }
        }

        public bool CanSetTemperature {
            get { return false; }
        }

        public bool CoolerOn {
            get { return false; }
            set { }
        }

        public double CoolerPower {
            get {
                return double.NaN;
            }
        }

        private string _cameraState;

        public string CameraState {
            get {
                return _cameraState;
            }
            set {
                _cameraState = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetOffset {
            get {
                return false;
            }
        }

        public bool CanSetUSBLimit {
            get {
                return false;
            }
        }

        public bool CanGetGain {
            get {
                return true;
            }
        }

        public bool CanSetGain {
            get {
                return true;
            }
        }

        public short GainMax {
            get {
                return ISOSpeeds.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }
        }

        public short GainMin {
            get {
                return ISOSpeeds.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            }
        }

        public short Gain {
            get {
                int iso;
                EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_ISOSpeed, 0, out iso);

                var translatediso = ISOSpeeds.Where(x => x.Value == iso).FirstOrDefault().Key;

                return translatediso;
            }
            set {
                ValidateMode();
                var iso = ISOSpeeds.Where((x) => x.Key == value).FirstOrDefault().Value;
                if (HasError(SetProperty(EDSDK.PropID_ISOSpeed, iso))) {
                    Notification.ShowError(Locale.Loc.Instance["LblUnableToSetISO"]);
                }
                RaisePropertyChanged();
            }
        }

        private ArrayList _gains;

        public ArrayList Gains {
            get {
                if (_gains == null) {
                    _gains = new ArrayList();
                }
                return _gains;
            }
        }

        private AsyncObservableCollection<BinningMode> _binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (_binningModes == null) {
                    _binningModes = new AsyncObservableCollection<BinningMode>();
                    _binningModes.Add(new BinningMode(1, 1));
                }
                return _binningModes;
            }
            private set {
            }
        }

        public bool HasSetupDialog {
            get { return false; }
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

        public IntPtr DirectoryItem { get; private set; }
        public bool DownloadReady { get; private set; }

        public void AbortExposure() {
            var err = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF);
        }

        [System.Obsolete("Use async Connect")]
        public bool Connect() {
            uint err = EDSDK.EdsOpenSession(_cam);
            if (err != (uint)EDSDK.EDS_ERR.OK) {
                return false;
            } else {
                Connected = true;
                if (!Initialize()) { Disconnect(); }
                RaiseAllPropertiesChanged();
                return true;
            }
        }

        private bool Initialize() {
            ValidateMode();
            GetISOSpeeds();
            SetRawFormat();
            SetSaveLocation();
            SubscribeEvents();

            return true;
        }

        protected event EDSDK.EdsObjectEventHandler SDKObjectEvent;

        private void SubscribeEvents() {
            SDKObjectEvent += new EDSDK.EdsObjectEventHandler(Camera_SDKObjectEvent);

            EDSDK.EdsSetObjectEventHandler(_cam, EDSDK.ObjectEvent_All, SDKObjectEvent, _cam);
        }

        private uint Camera_SDKObjectEvent(uint inEvent, IntPtr inRef, IntPtr inContext) {
            if (inEvent == EDSDK.ObjectEvent_DirItemRequestTransfer) {
                this.DirectoryItem = inRef;
                this.DownloadReady = true;
            }
            return (uint)EDSDK.EDS_ERR.OK;
        }

        private void SetSaveLocation() {
            /* 1: memory card; 2: pc; 3: both */
            if (HasError(SetProperty(EDSDK.PropID_SaveTo, 2))) {
                throw new Exception("Unable to set save location to PC");
            }

            EDSDK.EdsCapacity capacity = new EDSDK.EdsCapacity();
            capacity.NumberOfFreeClusters = 0x7FFFFFFF;
            capacity.BytesPerSector = 0x1000;
            capacity.Reset = 1;
            EDSDK.EdsSetCapacity(_cam, capacity);
        }

        private void SetRawFormat() {
            if (HasError(SetProperty(EDSDK.PropID_ImageQuality, (uint)EDSDK.ImageQuality.EdsImageQuality_LR))) {
                throw new Exception("Error setting Canon image quality to RAW");
            }
        }

        private Dictionary<double, int> ShutterSpeeds = new Dictionary<double, int>();

        private void GetShutterSpeeds() {
            ShutterSpeeds.Clear();
            EDSDK.EdsPropertyDesc bla;
            EDSDK.EdsGetPropertyDesc(_cam, EDSDK.PropID_Tv, out bla);
            for (int i = 0; i < bla.NumElements; i++) {
                var elem = bla.PropDesc[i];
                var item = EDSDK.ShutterSpeeds.FirstOrDefault((x) => x.Value == elem);
                if (item.Value != 0) {
                    ShutterSpeeds.Add(item.Key, item.Value);
                }
            }
        }

        private bool IsManualMode() {
            UInt32 mode;
            EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_AEModeSelect, 0, out mode);
            bool isManual = (mode == 3);
            return isManual;
        }

        private bool IsBulbMode() {
            UInt32 mode;
            EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_AEModeSelect, 0, out mode);
            bool isBulb = (mode == 4);
            return isBulb;
        }

        private Dictionary<short, int> ISOSpeeds = new Dictionary<short, int>();

        private void GetISOSpeeds() {
            ISOSpeeds.Clear();
            Gains.Clear();
            EDSDK.EdsPropertyDesc prop;
            EDSDK.EdsGetPropertyDesc(_cam, EDSDK.PropID_ISOSpeed, out prop);

            var length = (int)(prop.PropDesc.Length / prop.NumElements);

            for (int i = 0; i < prop.NumElements; i++) {
                var elem = prop.PropDesc[i];
                var item = EDSDK.ISOSpeeds.FirstOrDefault((x) => x.Value == elem);
                if (item.Value != 0) {
                    ISOSpeeds.Add(item.Key, item.Value);
                    Gains.Add(item.Key);
                }
            }
        }

        public void Disconnect() {
            uint err = EDSDK.EdsCloseSession(_cam);

            Connected = false;
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token) {
            return await Task<ImageArray>.Run(async () => {
                var stream = IntPtr.Zero;
                var pointer = IntPtr.Zero;
                try {
                    while (!DownloadReady) {
                        await Task.Delay(100);
                        token.ThrowIfCancellationRequested();
                    }

                    var sw = Stopwatch.StartNew();
                    
                    if (HasError(EDSDK.EdsGetDirectoryItemInfo(this.DirectoryItem, out var directoryItemInfo))) {                        
                        return null;
                    }

                    //create a file stream to accept the image

                    if (HasError(EDSDK.EdsCreateMemoryStream(directoryItemInfo.Size, out stream))) {
                        return null;
                    }

                    //download image

                    if (HasError(EDSDK.EdsDownload(this.DirectoryItem, directoryItemInfo.Size, stream))) {
                        return null;
                    }

                    //complete download

                    if (HasError(EDSDK.EdsDownloadComplete(this.DirectoryItem))) {
                        return null;
                    }
                    token.ThrowIfCancellationRequested();

                    Debug.Print("Download from Camera: " + sw.Elapsed);
                    sw.Restart();

                    //convert to memory stream

                    EDSDK.EdsGetPointer(stream, out pointer);
                    EDSDK.EdsGetLength(stream, out var length);

                    byte[] bytes = new byte[length];

                    //Move from unmanaged to managed code.
                    Marshal.Copy(pointer, bytes, 0, bytes.Length);

                    Debug.Print("Getting pixels to managed code : " + sw.Elapsed);
                    sw.Restart();

                    using (var memoryStream = new System.IO.MemoryStream(bytes)) {
                        var converter = RawConverter.CreateInstance(profileService.Profiles.ActiveProfile.CameraSettings.RawConverter);
                        var iarr = await converter.ConvertToImageArray(memoryStream, token, profileService.ActiveProfile.ImageSettings.HistogramResolution);
                        iarr.RAWType = "cr2";
                        return iarr;
                    }
                } finally {
                    if (pointer != IntPtr.Zero) {
                        EDSDK.EdsRelease(pointer);
                        pointer = IntPtr.Zero;
                    }

                    if (this.DirectoryItem != IntPtr.Zero) {
                        EDSDK.EdsRelease(this.DirectoryItem);
                        this.DirectoryItem = IntPtr.Zero;
                    }

                    if (stream != IntPtr.Zero) {
                        EDSDK.EdsRelease(stream);
                        stream = IntPtr.Zero;
                    }
                }
            });
        }

        public void SetBinning(short x, short y) {
        }

        public void SetupDialog() {
        }

        private void ValidateMode() {
            if (!IsManualMode() && !IsBulbMode()) {
                var result = MyMessageBox.MyMessageBox.Show(
                    Locale.Loc.Instance["LblEDCameraNotInManualMode"],
                    Locale.Loc.Instance["LblInvalidMode"],
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxResult.OK);
                if (result == System.Windows.MessageBoxResult.OK) {
                    ValidateMode();
                } else {
                    throw new Exception("Invalid camera mode");
                }
            }
        }

        private void ValidateModeForExposure(double exposureTime) {
            if (!IsManualMode() && !IsBulbMode()) {
                var result = MyMessageBox.MyMessageBox.Show(
                    Locale.Loc.Instance["LblEDCameraNotInManualMode"],
                    Locale.Loc.Instance["LblInvalidMode"],
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxResult.OK);
                if (result == System.Windows.MessageBoxResult.OK) {
                    ValidateModeForExposure(exposureTime);
                } else {
                    throw new Exception("Invalid camera mode for taking exposures");
                }
            }

            if (IsManualMode()) {
                GetShutterSpeeds();
                if (exposureTime <= 30.0) {
                    SetExposureTime(exposureTime);
                } else {
                    var success = SetExposureTime(double.MaxValue);
                    if (!success) {
                        var result = MyMessageBox.MyMessageBox.Show(
                            Locale.Loc.Instance["LblChangeToBulbMode"],
                            Locale.Loc.Instance["LblInvalidModeManual"],
                            System.Windows.MessageBoxButton.OKCancel,
                            System.Windows.MessageBoxResult.OK);
                        if (result == System.Windows.MessageBoxResult.OK) {
                            ValidateModeForExposure(exposureTime);
                        } else {
                            throw new Exception("Invalid camera mode [Manual] for taking bulb exposures");
                        }
                    }
                }
            }

            if (IsBulbMode() && exposureTime < 1.0) {
                var result = MyMessageBox.MyMessageBox.Show(
                    Locale.Loc.Instance["LblChangeToManualMode"],
                    Locale.Loc.Instance["LblInvalidModeBulb"],
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxResult.OK);
                if (result == System.Windows.MessageBoxResult.OK) {
                    ValidateModeForExposure(exposureTime);
                } else {
                    throw new Exception("Invalid camera mode [Bulb] for taking exposures < 1s");
                }
            };
        }

        public void StartExposure(double exposureTime, bool isLightFrame) {
            DownloadReady = false;

            ValidateModeForExposure(exposureTime);

            /* Start exposure */
            if (HasError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_Completely_NonAF))) {
                Notification.ShowError(Locale.Loc.Instance["LblUnableToStartExposure"]);
            }
            DateTime d = DateTime.Now;
            /*Stop Exposure after exposure time */
            Task.Run(async () => {
                await Utility.Utility.Wait(TimeSpan.FromSeconds(exposureTime));

                if (HasError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF))) {
                    Notification.ShowError("Could not stop camera exposure");
                }
            });
        }

        private bool SetExposureTime(double exposureTime) {
            double key;
            if (exposureTime != double.MaxValue) {
                var l = new List<double>(ShutterSpeeds.Keys);
                key = l.Aggregate((x, y) => Math.Abs(x - exposureTime) < Math.Abs(y - exposureTime) ? x : y);
            } else {
                key = double.MaxValue;
                if (!ShutterSpeeds.ContainsKey(key)) {
                    // No Bulb available - bulb mode has to be set manually
                    return false;
                }
            }

            /* Shutter speed to Bulb */
            if (HasError(SetProperty(EDSDK.PropID_Tv, ShutterSpeeds[key]))) {
                Notification.ShowError(Locale.Loc.Instance["LblUnableToSetExposureTime"]);
                return false;
            }
            return true;
        }

        public int Offset {
            get {
                return -1;
            }
            set {
            }
        }

        public int USBLimit {
            get {
                return -1;
            }
            set {
            }
        }

        public void StopExposure() {
            var err = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF);
        }

        private uint SetProperty(uint property, object value) {
            int propsize;
            EDSDK.EdsDataType proptype;
            var err = EDSDK.EdsGetPropertySize(_cam, property, 0, out proptype, out propsize);
            if (err != (uint)EDSDK.EDS_ERR.OK) {
                return err;
            }
            err = EDSDK.EdsSetPropertyData(_cam, property, 0, propsize, value);
            return err;
        }

        private bool HasError(uint err) {
            if (err == (uint)EDSDK.EDS_ERR.OK) {
                return false;
            } else {
                Logger.Error(new Exception(string.Format("Canon SDK Error with Code {0} occured", err)));
                return true;
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                uint err = EDSDK.EdsOpenSession(_cam);
                if (err != (uint)EDSDK.EDS_ERR.OK) {
                    return false;
                } else {
                    Connected = true;
                    if (!Initialize()) {
                        Disconnect();
                        return false;
                    }
                    RaiseAllPropertiesChanged();
                    return true;
                }
            });
        }

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        public void StartLiveView() {
            SetProperty(EDSDK.PropID_Evf_OutputDevice, (int)EDSDK.EvfOutputDevice_PC);
            LiveViewEnabled = true;
        }

        public void StopLiveView() {
            SetProperty(EDSDK.PropID_Evf_OutputDevice, (int)EDSDK.EvfOutputDevice_OFF);
            LiveViewEnabled = false;
        }

        public async Task<ImageArray> DownloadLiveView(CancellationToken token) {
            if (HasError(EDSDK.EdsCreateMemoryStream(0, out var stream))) {
                return null;
            }

            if (HasError(EDSDK.EdsCreateEvfImageRef(stream, out var imageRef))) {
                return null;
            }

            if (HasError(EDSDK.EdsDownloadEvfImage(_cam, imageRef))) {
                return null;
            }

            EDSDK.EdsGetPointer(stream, out var pointer);
            EDSDK.EdsGetLength(stream, out var length);

            byte[] bytes = new byte[length];

            //Move from unmanaged to managed code.
            Marshal.Copy(pointer, bytes, 0, bytes.Length);

            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(bytes);

            JpegBitmapDecoder decoder = new JpegBitmapDecoder(memoryStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);

            FormatConvertedBitmap bitmap = new FormatConvertedBitmap();
            bitmap.BeginInit();
            bitmap.Source = decoder.Frames[0];
            bitmap.DestinationFormat = System.Windows.Media.PixelFormats.Gray16;
            bitmap.EndInit();

            ushort[] outArray = new ushort[bitmap.PixelWidth * bitmap.PixelHeight];
            bitmap.CopyPixels(outArray, 2 * bitmap.PixelWidth, 0);

            var iarr = await ImageArray.CreateInstance(outArray, bitmap.PixelWidth, bitmap.PixelHeight, false, false, profileService.ActiveProfile.ImageSettings.HistogramResolution);

            memoryStream.Close();
            memoryStream.Dispose();

            EDSDK.EdsRelease(stream);
            EDSDK.EdsRelease(imageRef);

            return iarr;
        }
    }
}