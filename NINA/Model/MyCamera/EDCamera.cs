#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using EDSDKLib;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using NINA.Utility.RawConverter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
                    if (CheckError(EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_FirmwareVersion, 0, out property))) {
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
                return this.ShutterSpeeds.Aggregate((l, r) => l.Value > r.Value ? l : r).Value;
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

        public bool HasDewHeater {
            get {
                return false;
            }
        }

        public bool DewHeaterOn {
            get {
                return false;
            }
            set {
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
                if (CheckError(SetProperty(EDSDK.PropID_ISOSpeed, iso))) {
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

        public IEnumerable<string> ReadoutModes => new List<string> { "Default" };

        private short _readoutModeForSnapImages;

        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set {
                _readoutModeForSnapImages = value;
                RaisePropertyChanged();
            }
        }

        private short _readoutModeForNormalImages;

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                _readoutModeForNormalImages = value;
                RaisePropertyChanged();
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
        private TaskCompletionSource<object> downloadExposure;

        public void AbortExposure() {
            CheckError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF));
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
                downloadExposure?.TrySetResult(true);
            }
            return (uint)EDSDK.EDS_ERR.OK;
        }

        private void SetSaveLocation() {
            /* 1: memory card; 2: pc; 3: both */
            if (CheckError(SetProperty(EDSDK.PropID_SaveTo, 2))) {
                throw new Exception("Unable to set save location to PC");
            }

            EDSDK.EdsCapacity capacity = new EDSDK.EdsCapacity();
            capacity.NumberOfFreeClusters = 0x7FFFFFFF;
            capacity.BytesPerSector = 0x1000;
            capacity.Reset = 1;
            EDSDK.EdsSetCapacity(_cam, capacity);
        }

        private void SetRawFormat() {
            CheckAndThrowError(SetProperty(EDSDK.PropID_ImageQuality, (uint)EDSDK.ImageQuality.EdsImageQuality_LR));
        }

        /// <summary>
        /// Internal ShutterSpeed Code -> ShutterSpeed Value
        /// e.g.: 0x10 -> 30
        /// </summary>
        private Dictionary<int, double> ShutterSpeeds = new Dictionary<int, double>();

        private void GetShutterSpeeds() {
            ShutterSpeeds.Clear();
            EDSDK.EdsGetPropertyDesc(_cam, EDSDK.PropID_Tv, out var shutterSpeedsDesc);
            for (int i = 0; i < shutterSpeedsDesc.NumElements; i++) {
                var elem = shutterSpeedsDesc.PropDesc[i];
                var item = EDSDK.ShutterSpeeds.FirstOrDefault((x) => x.Key == elem);
                if (item.Value != 0) {
                    ShutterSpeeds.Add(item.Key, item.Value);
                } else {
                    Logger.Warning("Canon - Unknown Shutterspeed with code: " + elem);
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
            CheckError(EDSDK.EdsCloseSession(_cam));

            Connected = false;
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token, bool calculateStatistics) {
            return await Task<ImageArray>.Run(async () => {
                var memoryStreamHandle = IntPtr.Zero;
                var imageDataPointer = IntPtr.Zero;
                try {
                    using (token.Register(() => downloadExposure.TrySetCanceled())) {
                        await downloadExposure.Task;
                    }

                    using (MyStopWatch.Measure("Canon - Image Download")) {
                        CheckAndThrowError(EDSDK.EdsGetDirectoryItemInfo(this.DirectoryItem, out var directoryItemInfo));

                        //create a file stream to accept the image
                        CheckAndThrowError(EDSDK.EdsCreateMemoryStream(directoryItemInfo.Size, out memoryStreamHandle));

                        //download image
                        CheckAndThrowError(EDSDK.EdsDownload(this.DirectoryItem, directoryItemInfo.Size, memoryStreamHandle));

                        //complete download
                        CheckAndThrowError(EDSDK.EdsDownloadComplete(this.DirectoryItem));

                        token.ThrowIfCancellationRequested();
                    }

                    using (MyStopWatch.Measure("Canon - Creating Image Array")) {
                        //convert to memory stream
                        EDSDK.EdsGetPointer(memoryStreamHandle, out imageDataPointer);
                        EDSDK.EdsGetLength(memoryStreamHandle, out var length);

                        byte[] rawImageData = new byte[length];

                        //Move from unmanaged to managed code.
                        Marshal.Copy(imageDataPointer, rawImageData, 0, rawImageData.Length);

                        token.ThrowIfCancellationRequested();

                        using (var memoryStream = new System.IO.MemoryStream(rawImageData)) {
                            var converter = RawConverter.CreateInstance(profileService.Profiles.ActiveProfile.CameraSettings.RawConverter);
                            var iarr = await converter.ConvertToImageArray(memoryStream, BitDepth, profileService.ActiveProfile.ImageSettings.HistogramResolution, calculateStatistics, token);
                            iarr.RAWType = "cr2";
                            return iarr;
                        }
                    }
                } finally {
                    /* Memory cleanup */
                    if (imageDataPointer != IntPtr.Zero) {
                        EDSDK.EdsRelease(imageDataPointer);
                        imageDataPointer = IntPtr.Zero;
                    }

                    if (this.DirectoryItem != IntPtr.Zero) {
                        EDSDK.EdsRelease(this.DirectoryItem);
                        this.DirectoryItem = IntPtr.Zero;
                    }

                    if (memoryStreamHandle != IntPtr.Zero) {
                        EDSDK.EdsRelease(memoryStreamHandle);
                        memoryStreamHandle = IntPtr.Zero;
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

        public void StartExposure(CaptureSequence sequence, bool isLightFrame) {
            downloadExposure = new TaskCompletionSource<object>();
            var exposureTime = sequence.ExposureTime;
            ValidateModeForExposure(exposureTime);

            /* Start exposure */
            CheckAndThrowError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_Completely_NonAF));

            if ((IsManualMode() && exposureTime > 30.0) || (IsBulbMode() && exposureTime >= 1.0)) {
                /*Stop Exposure after exposure time */
                Task.Run(async () => {
                    await Utility.Utility.Wait(TimeSpan.FromSeconds(exposureTime));

                    StopExposure();
                });
            } else {
                /*Immediately release shutter button when having a set exposure*/
                StopExposure();
            }
        }

        private bool SetExposureTime(double exposureTime) {
            double nearestExposureTime = double.MaxValue;
            if (exposureTime != double.MaxValue) {
                var l = new List<double>(ShutterSpeeds.Values);
                nearestExposureTime = l.Aggregate((x, y) => Math.Abs(x - exposureTime) < Math.Abs(y - exposureTime) ? x : y);
            }

            var key = ShutterSpeeds.FirstOrDefault(x => x.Value == nearestExposureTime).Key;
            if (key == 0) {
                // No Bulb available - bulb mode has to be set manually
                return false;
            }

            CheckAndThrowError(SetProperty(EDSDK.PropID_Tv, key));
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

        public int BatteryLevel {
            get {
                try {
                    if (!CheckError(EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_BatteryLevel, 0, out UInt32 batteryLevel))) {
                        return (int)batteryLevel;
                    } else {
                        return -1;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    return -1;
                }
            }
        }

        public void StopExposure() {
            CheckAndThrowError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF));
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

        private bool CheckError(uint code, [CallerMemberName] string memberName = "") {
            var err = GetError(code);
            return CheckError(err, memberName);
        }

        private bool CheckError(EDSDK.EDS_ERR err, [CallerMemberName] string memberName = "") {
            if (err == EDSDK.EDS_ERR.OK) {
                return false;
            } else {
                Logger.Error(new Exception(string.Format(Locale.Loc.Instance["LblCanonErrorOccurred"], err)), memberName);
                return true;
            }
        }

        private void CheckAndThrowError(EDSDK.EDS_ERR err, [CallerMemberName] string memberName = "") {
            if (err != EDSDK.EDS_ERR.OK) {
                var ex = new Exception(string.Format(Locale.Loc.Instance["LblCanonErrorOccurred"], err));
                Logger.Error(ex, memberName);
                throw ex;
            }
        }

        private void CheckAndThrowError(uint code, [CallerMemberName] string memberName = "") {
            var err = GetError(code);
            CheckAndThrowError(err, memberName);
        }

        private EDSDK.EDS_ERR GetError(uint code) {
            return (EDSDK.EDS_ERR)code;
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

        private int bitDepth;

        public int BitDepth {
            get {
                return (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        public bool HasBattery => true;

        public void StartLiveView() {
            SetProperty(EDSDK.PropID_Evf_OutputDevice, (int)EDSDK.EvfOutputDevice_PC);
            LiveViewEnabled = true;
        }

        public void StopLiveView() {
            SetProperty(EDSDK.PropID_Evf_OutputDevice, (int)EDSDK.EvfOutputDevice_OFF);
            LiveViewEnabled = false;
        }

        public async Task<ImageArray> DownloadLiveView(CancellationToken token) {
            IntPtr stream = IntPtr.Zero;
            IntPtr imageRef = IntPtr.Zero;
            IntPtr pointer = IntPtr.Zero;
            try {
                CheckAndThrowError(EDSDK.EdsCreateMemoryStream(0, out stream));

                CheckAndThrowError(EDSDK.EdsCreateEvfImageRef(stream, out imageRef));

                EDSDK.EDS_ERR err;
                do {
                    err = GetError(EDSDK.EdsDownloadEvfImage(_cam, imageRef));
                    if (err == EDSDK.EDS_ERR.OBJECT_NOTREADY) {
                        await Utility.Utility.Wait(TimeSpan.FromMilliseconds(100), token);
                    }
                } while (err == EDSDK.EDS_ERR.OBJECT_NOTREADY);

                CheckAndThrowError(err);

                EDSDK.EdsGetPointer(stream, out pointer);
                EDSDK.EdsGetLength(stream, out var length);

                byte[] bytes = new byte[length];

                //Move from unmanaged to managed code.
                Marshal.Copy(pointer, bytes, 0, bytes.Length);

                using (var memoryStream = new System.IO.MemoryStream(bytes)) {
                    JpegBitmapDecoder decoder = new JpegBitmapDecoder(memoryStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);

                    FormatConvertedBitmap bitmap = new FormatConvertedBitmap();
                    bitmap.BeginInit();
                    bitmap.Source = decoder.Frames[0];
                    bitmap.DestinationFormat = System.Windows.Media.PixelFormats.Gray16;
                    bitmap.EndInit();

                    ushort[] outArray = new ushort[bitmap.PixelWidth * bitmap.PixelHeight];
                    bitmap.CopyPixels(outArray, 2 * bitmap.PixelWidth, 0);

                    return await ImageArray.CreateInstance(outArray, bitmap.PixelWidth, bitmap.PixelHeight, BitDepth, false, false, profileService.ActiveProfile.ImageSettings.HistogramResolution);
                }
            } finally {
                /* Memory cleanup */
                if (stream != IntPtr.Zero) {
                    EDSDK.EdsRelease(stream);
                    stream = IntPtr.Zero;
                }
                if (pointer != IntPtr.Zero) {
                    EDSDK.EdsRelease(pointer);
                    pointer = IntPtr.Zero;
                }
                if (imageRef != IntPtr.Zero) {
                    EDSDK.EdsRelease(imageRef);
                    imageRef = IntPtr.Zero;
                }
            }
        }
    }
}