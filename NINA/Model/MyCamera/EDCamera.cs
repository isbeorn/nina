#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Model.ImageData;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Notification;
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

        public string Category { get; } = "Canon";

        private IProfileService profileService;

        private IntPtr _cam;

        public bool HasShutter => true;

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public double Temperature => double.NaN;

        public double TemperatureSetPoint {
            get => double.NaN;
            set {
            }
        }

        public short BinX {
            get => 1;
            set {
            }
        }

        public bool CanSubSample => false;

        public short BinY {
            get => 1;
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
            get => _name;
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public string Description => "Canon Camera";

        public string DriverInfo => string.Empty;

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
                if (Connected) {
                    return EDSDK.EDS_ERR_OK == EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_Evf_OutputDevice, 0, out uint dummy);
                }
                return false;
            }
        }

        public string SensorName => string.Empty;

        public SensorType SensorType => SensorType.RGGB;

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        public int CameraXSize => -1;

        public int CameraYSize => -1;

        public double ExposureMin => this.ShutterSpeeds.Aggregate((l, r) => l.Value > r.Value ? l : r).Value;

        public double ExposureMax => double.PositiveInfinity;

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX => 1;

        public short MaxBinY => 1;

        public double PixelSizeX => -1;

        public double PixelSizeY => -1;

        public bool CanSetTemperature => false;

        public bool CoolerOn {
            get => false;
            set { }
        }

        public double CoolerPower => double.NaN;

        public bool HasDewHeater => false;

        public bool DewHeaterOn {
            get => false;
            set { }
        }

        private string _cameraState;

        public string CameraState {
            get => _cameraState;
            set {
                _cameraState = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetOffset => false;

        public int OffsetMin => 0;

        public int OffsetMax => 0;

        public bool CanSetUSBLimit => false;

        public bool CanGetGain => true;

        public bool CanSetGain => true;

        public int GainMax => ISOSpeeds.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

        public int GainMin => ISOSpeeds.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

        public int Gain {
            get {
                EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_ISOSpeed, 0, out uint iso);

                var translatediso = ISOSpeeds.Where(x => x.Value == iso).FirstOrDefault().Key;

                return translatediso;
            }
            set {
                ValidateMode();
                int iso = ISOSpeeds.Where((x) => x.Key == value).FirstOrDefault().Value;
                if (CheckError(SetProperty(EDSDK.PropID_ISOSpeed, iso))) {
                    Notification.ShowError(Locale.Loc.Instance["LblUnableToSetISO"]);
                }
                RaisePropertyChanged();
            }
        }

        private IList<int> _gains;

        public IList<int> Gains {
            get {
                if (_gains == null) {
                    _gains = new List<int>();
                }
                return _gains;
            }
        }

        public IEnumerable ReadoutModes => new List<string> { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

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
            private set { }
        }

        public bool HasSetupDialog => false;

        private string _id;

        public string Id {
            get => _id;
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        public IntPtr DirectoryItem { get; private set; }
        private TaskCompletionSource<object> downloadExposure;

        public void AbortExposure() {
            CheckError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF));
            CancelDownloadExposure();
        }

        private bool Initialize() {
            usesCameraCommandBulb = true;
            ValidateMode();
            GetISOSpeeds();
            GetShutterSpeeds();
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
            return EDSDK.EDS_ERR_OK;
        }

        private void SetSaveLocation() {
            if (CheckError(SetProperty(EDSDK.PropID_SaveTo, (uint)EDSDK.EdsSaveTo.Host))) {
                Logger.Error("CANON: Unable to set save location to Host");
                throw new Exception("Unable to set save location to Host");
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
                var item = EDSDKLocal.ShutterSpeeds.FirstOrDefault((x) => x.Key == elem);
                if (item.Value != 0) {
                    ShutterSpeeds.Add(item.Key, item.Value);
                } else {
                    Logger.Error("CANON: Unknown shutter speed code: " + elem);
                }
            }
        }

        private bool IsManualMode() {
            EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_AEMode, 0, out uint mode);

            if (mode == EDSDK.AEMode_Mamual) {
                Logger.Debug("CANON: Camera is in MANUAL mode");
                return true;
            } else {
                return false;
            }
        }

        private bool IsBulbMode() {
            bool isBulb = false;

            EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_AEMode, 0, out uint mode);

            if (mode == EDSDK.AEMode_Bulb) {
                Logger.Debug("CANON: Camera is in BULB mode");
                isBulb = true;
            } else {
                /*
                 * Older cameras with Manual on the dial but are set to "BULB" (0x0C) shutter speed
                 * can be considered to be in Bulb mode as well
                 */
                EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_Tv, 0, out uint speed);

                if (mode == EDSDK.AEMode_Mamual && speed == 0x0C) {
                    Logger.Debug("CANON: Camera is in manual mode but has a BULB speed (0x0C)");
                    isBulb = IsManualBulb = true;
                } else {
                    IsManualBulb = false;
                }
            }

            return isBulb;
        }

        private Dictionary<int, int> ISOSpeeds = new Dictionary<int, int>();

        private void GetISOSpeeds() {
            ISOSpeeds.Clear();
            Gains.Clear();

            EDSDK.EdsGetPropertyDesc(_cam, EDSDK.PropID_ISOSpeed, out EDSDK.EdsPropertyDesc prop);

            // Avoid a possible divide by zero situation
            if (prop.NumElements > 0) {
                var length = (prop.PropDesc.Length / prop.NumElements);
            } else {
                Logger.Warning("CANON: Unable to get ISO list from camera");
            }

            for (int i = 0; i < prop.NumElements; i++) {
                int elem = prop.PropDesc[i];
                var item = EDSDKLocal.ISOSpeeds.FirstOrDefault((x) => x.Value == elem);
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

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                await downloadExposure.Task;
            }
        }

        public Task<IExposureData> DownloadExposure(CancellationToken token) {
            return Task.Run<IExposureData>(async () => {
                if (downloadExposure.Task.IsCanceled) { return null; }
                var memoryStreamHandle = IntPtr.Zero;
                var imageDataPointer = IntPtr.Zero;
                try {
                    using (token.Register(() => CancelDownloadExposure())) {
                        await downloadExposure.Task;
                    }

                    using (MyStopWatch.Measure("Canon - Image Download")) {
                        CheckAndThrowError(EDSDK.EdsGetDirectoryItemInfo(DirectoryItem, out var directoryItemInfo));

                        //create a file stream to accept the image
                        CheckAndThrowError(EDSDK.EdsCreateMemoryStream(directoryItemInfo.Size, out memoryStreamHandle));

                        //download image
                        CheckAndThrowError(EDSDK.EdsDownload(DirectoryItem, directoryItemInfo.Size, memoryStreamHandle));

                        //complete download
                        CheckAndThrowError(EDSDK.EdsDownloadComplete(DirectoryItem));

                        token.ThrowIfCancellationRequested();
                    }

                    using (MyStopWatch.Measure("Canon - Creating Image Array")) {
                        //convert to memory stream
                        EDSDK.EdsGetPointer(memoryStreamHandle, out imageDataPointer);
                        EDSDK.EdsGetLength(memoryStreamHandle, out ulong length);

                        byte[] rawImageData = new byte[length];

                        //Move from unmanaged to managed code.
                        Marshal.Copy(imageDataPointer, rawImageData, 0, rawImageData.Length);

                        //release directory item
                        if (this.DirectoryItem != IntPtr.Zero) {
                            CheckAndThrowError(EDSDK.EdsRelease(DirectoryItem));
                        }

                        //release stream
                        if (memoryStreamHandle != IntPtr.Zero) {
                            CheckAndThrowError(EDSDK.EdsRelease(memoryStreamHandle));
                        }

                        token.ThrowIfCancellationRequested();

                        var rawConverter = RawConverter.CreateInstance(profileService.ActiveProfile.CameraSettings.RawConverter);
                        return new RAWExposureData(
                            rawConverter: rawConverter,
                            rawBytes: rawImageData,
                            rawType: "cr2",
                            bitDepth: BitDepth,
                            metaData: new ImageMetaData());
                    }
                } finally {
                    /* Memory cleanup */
                    if (imageDataPointer != IntPtr.Zero) {
                        EDSDK.EdsRelease(imageDataPointer);
                        imageDataPointer = IntPtr.Zero;
                    }

                    if (DirectoryItem != IntPtr.Zero) {
                        EDSDK.EdsRelease(DirectoryItem);
                        DirectoryItem = IntPtr.Zero;
                    }

                    if (memoryStreamHandle != IntPtr.Zero) {
                        EDSDK.EdsRelease(memoryStreamHandle);
                        memoryStreamHandle = IntPtr.Zero;
                    }
                }
            });
        }

        private void CancelDownloadExposure() {
            EDSDK.EdsDownloadCancel(this.DirectoryItem);
            downloadExposure.TrySetCanceled();
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

        public void StartExposure(CaptureSequence sequence) {
            downloadExposure = new TaskCompletionSource<object>();
            var exposureTime = sequence.ExposureTime;
            ValidateModeForExposure(exposureTime);

            /* Start exposure */
            bool useBulb = (IsManualMode() && exposureTime > 30.0) || (IsBulbMode() && exposureTime >= 1.0);
            SendStartExposureCmd(useBulb);

            if (useBulb) {
                /* Stop Exposure after exposure time */
                Task.Run(async () => {
                    await Utility.Utility.Wait(TimeSpan.FromSeconds(exposureTime));

                    SendStopExposureCmd(true);
                });
            } else {
                /* Immediately release shutter button when having a set exposure */
                SendStopExposureCmd(false);
            }
        }

        private void SendStartExposureCmd(bool useBulb) {
            uint error;
            const int PsbNonAF = (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_Completely_NonAF;

            if (useBulb) {
                /*
                 * Cameras that feature a Bulb ("B") mode on the mode selection dial use the PressShutterButton command to
                 * start and stop bulb exposures when in that mode. Cameras that lack a "B" mode on the mode selection dial
                 * and instead set bulb mode as a shutter speed while in Manual ("M") mode on the dial use BulbStart/BulbEnd.
                 */
                if (IsManualBulb && usesCameraCommandBulb) {
                    Logger.Debug("CANON: Initiating BULB mode exposure (via BulbStart)");

                    if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_BulbStart, 0)) != EDSDK.EDS_ERR_OK) {
                        Logger.Error($"CANON: Error initiating BULB mode exposure (via BulbStart): {ErrorCodeToString(error)}");

                        if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, PsbNonAF)) != EDSDK.EDS_ERR_OK) {
                            Logger.Error($"CANON: Error initiating BULB mode exposure (via PressShutterButton): {ErrorCodeToString(error)}");
                        } else {
                            usesCameraCommandBulb = false;
                        }
                    }
                } else {
                    Logger.Debug("CANON: Initiating BULB mode exposure (via PressShutterButton)");

                    if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, PsbNonAF)) != EDSDK.EDS_ERR_OK) {
                        Logger.Error($"CANON: Error initiating BULB mode exposure (via PressShutterButton): {ErrorCodeToString(error)}");
                    }
                }
            } else {
                /*
                 * Manual mode exposure
                 */
                Logger.Debug("CANON: Initiating MANUAL mode exposure (via PressShutterButton)");

                if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, PsbNonAF)) != EDSDK.EDS_ERR_OK) {
                    Logger.Error($"CANON: Error initiating MANUAL mode exposure (via PressShutterButton): {ErrorCodeToString(error)}");

                    /*
                     * Older Canon cameras, such as the Rebel XSi (450D), don't support the PressShutterButton command.
                     * Fall back to the TakePicture command if PressShutterButton failed. There doesn't appear to be a way
                     * to know of this situation ahead of time.
                     */
                    Logger.Debug("CANON: Initiating MANUAL mode exposure (via TakePicture)");

                    if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_TakePicture, 0)) != EDSDK.EDS_ERR_OK) {
                        Logger.Error($"CANON: Error initiating MANUAL mode exposure (via TakePicture): {ErrorCodeToString(error)}");
                    }
                }
            }

            CheckAndThrowError(error);
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
            get => -1;
            set { }
        }

        public int USBLimit {
            get => -1;
            set { }
        }

        public int BatteryLevel {
            get {
                try {
                    if (!CheckError(EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_BatteryLevel, 0, out uint batteryLevel))) {
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
            SendStopExposureCmd(false);
        }

        private void SendStopExposureCmd(bool useBulb) {
            uint error;

            if (useBulb) {
                if (IsManualBulb && usesCameraCommandBulb) {
                    Logger.Debug("CANON: Ending BULB mode exposure (via BulbEnd)");

                    if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_BulbEnd, 0)) != EDSDK.EDS_ERR_OK) {
                        Logger.Error($"CANON: Error stopping BULB mode exposure (via BulbEnd): {ErrorCodeToString(error)}");

                        if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF)) != EDSDK.EDS_ERR_OK) {
                            Logger.Error($"CANON: Error stopping BULB mode exposure (via PressShutterButton): {ErrorCodeToString(error)}");
                        }
                    }
                } else {
                    Logger.Debug("CANON: Ending BULB exposure (via PressShutterButton)");

                    if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF)) != EDSDK.EDS_ERR_OK) {
                        Logger.Error($"CANON: Error stopping BULB mode exposure (via PressShutterButton): {ErrorCodeToString(error)}");
                    }
                }
            } else {
                /*
                 * Manual mode exposure
                 */
                Logger.Debug("CANON: Ending MANUAL mode exposure (via PressShutterButton)");

                if ((error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF)) != EDSDK.EDS_ERR_OK) {
                    Logger.Error($"CANON: Error ending MANUAL mode exposure (via PressShutterButton): {ErrorCodeToString(error)}");
                }
            }

            CheckAndThrowError(error);
        }

        private uint SetProperty(uint property, object value) {
            var err = EDSDK.EdsGetPropertySize(_cam, property, 0, out EDSDK.EdsDataType proptype, out int propsize);
            if (err != EDSDK.EDS_ERR_OK) {
                return err;
            }
            err = EDSDK.EdsSetPropertyData(_cam, property, 0, propsize, value);
            return err;
        }

        private bool CheckError(uint err, [CallerMemberName] string memberName = "") {
            if (err == EDSDK.EDS_ERR_OK) {
                return false;
            } else {
                Logger.Error(new Exception(string.Format(Locale.Loc.Instance["LblCanonErrorOccurred"], ErrorCodeToString(err))), memberName);
                return true;
            }
        }

        private void CheckAndThrowError(uint err, [CallerMemberName] string memberName = "") {
            if (err != EDSDK.EDS_ERR_OK) {
                var ex = new Exception(string.Format(Locale.Loc.Instance["LblCanonErrorOccurred"], ErrorCodeToString(err)));
                Logger.Error(ex, memberName);
                throw ex;
            }
        }

        private string ErrorCodeToString(uint err) {
            string errStr;
            if (EDSDKLocal.ErrorCodes.ContainsKey(err)) {
                errStr = EDSDKLocal.ErrorCodes[err];
            } else {
                errStr = $"Unknown ({err})";
            }

            return errStr;
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                try {
                    CheckAndThrowError(EDSDK.EdsOpenSession(_cam));

                    if (!Initialize()) {
                        Disconnect();
                        return false;
                    }
                    Connected = true;
                    RaiseAllPropertiesChanged();

                    return true;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                    return false;
                }
            });
        }

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get => _liveViewEnabled;
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool usesCameraCommandBulb = true;
        private bool IsManualBulb { get; set; } = false;

        public int BitDepth => (int)profileService.ActiveProfile.CameraSettings.BitDepth;

        public bool HasBattery => true;

        public void StartLiveView() {
            uint err = EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_Evf_OutputDevice, 0, out uint device);

            if (err == EDSDK.EDS_ERR_OK) {
                device |= EDSDK.EvfOutputDevice_PC;
                SetProperty(EDSDK.PropID_Evf_OutputDevice, device);
                LiveViewEnabled = true;
            }
        }

        public void StopLiveView() {
            uint err = EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_Evf_OutputDevice, 0, out uint device);

            if (err == EDSDK.EDS_ERR_OK) {
                device &= ~EDSDK.EvfOutputDevice_PC;
                SetProperty(EDSDK.PropID_Evf_OutputDevice, device);
                LiveViewEnabled = false;
            }
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return Task.Run<IExposureData>(async () => {
                IntPtr stream = IntPtr.Zero;
                IntPtr imageRef = IntPtr.Zero;
                IntPtr pointer = IntPtr.Zero;
                ulong bufferSize = 2 * 1024 * 1024;

                try {
                    CheckAndThrowError(EDSDK.EdsCreateMemoryStream(bufferSize, out stream));

                    CheckAndThrowError(EDSDK.EdsCreateEvfImageRef(stream, out imageRef));

                    uint err;
                    do {
                        err = EDSDK.EdsDownloadEvfImage(_cam, imageRef);
                        if (err == EDSDK.EDS_ERR_OBJECT_NOTREADY) {
                            await Utility.Utility.Wait(TimeSpan.FromMilliseconds(100), token);
                        }
                    } while (err == EDSDK.EDS_ERR_OBJECT_NOTREADY);

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
                        return new ImageArrayExposureData(
                            input: outArray,
                            width: bitmap.PixelWidth,
                            height: bitmap.PixelHeight,
                            bitDepth: 16,
                            isBayered: false,
                            metaData: new ImageMetaData());
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
            });
        }
    }
}