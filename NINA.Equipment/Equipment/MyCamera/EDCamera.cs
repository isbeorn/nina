#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EDSDKLib;
using NINA.Core.Enum;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.MyMessageBox;
using NINA.Image.RawConverter;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;

namespace NINA.Equipment.Equipment.MyCamera {

    public class EDCamera : BaseINPC, ICamera {

        public EDCamera(IntPtr cam, EDSDK.EdsDeviceInfo info, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            _cam = cam;
            portName = info.szPortName;
            Id = info.szDeviceDescription;
            Name = info.szDeviceDescription;
        }
        private string portName;
        public string Category { get; } = "Canon";

        private IProfileService profileService;

        private readonly IExposureDataFactory exposureDataFactory;

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

        public double ExposureMin => this.ShutterSpeeds.Min(v => (double?)v.Value).GetValueOrDefault(0);

        public double ExposureMax => double.PositiveInfinity;

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX => 1;

        public short MaxBinY => 1;

        public double PixelSizeX => double.NaN;

        public double PixelSizeY => double.NaN;

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

        public CameraStates CameraState => CameraStates.NoState;

        public IList<string> SupportedActions => new List<string>();

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
                    Notification.ShowExternalError(Loc.Instance["LblUnableToSetISO"], Loc.Instance["LblCanonDriverError"]);
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

        public IList<string> ReadoutModes => new List<string> { "Default" };

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
            GetBatteryLevel();
            SetRawFormat();
            SetSaveLocation();
            SubscribeEvents();

            return true;
        }

        protected event EDSDK.EdsObjectEventHandler SDKObjectEvent;

        protected event EDSDK.EdsStateEventHandler SDKStateEvent;

        protected event EDSDK.EdsPropertyEventHandler SDKPropertyEvent;

        private void SubscribeEvents() {
            SDKObjectEvent += new EDSDK.EdsObjectEventHandler(Camera_SDKObjectEvent);
            SDKStateEvent += new EDSDK.EdsStateEventHandler(Camera_SDKStateEvent);
            SDKPropertyEvent += new EDSDK.EdsPropertyEventHandler(Camera_SDKPropertyEvent);

            EDSDK.EdsSetObjectEventHandler(_cam, EDSDK.ObjectEvent_All, SDKObjectEvent, IntPtr.Zero);
            EDSDK.EdsSetCameraStateEventHandler(_cam, EDSDK.StateEvent_All, SDKStateEvent, IntPtr.Zero);
            EDSDK.EdsSetPropertyEventHandler(_cam, EDSDK.PropertyEvent_All, SDKPropertyEvent, IntPtr.Zero);
        }

        private uint Camera_SDKStateEvent(uint inEvent, uint inParameter, IntPtr inContext) {
            Logger.Debug($"CANON: Received state event: 0x{inEvent:x}, parameter: 0x{inParameter:x}");

            switch (inEvent) {
                case EDSDK.StateEvent_WillSoonShutDown:
                    Logger.Info("CANON: Will soon shutdown - sending request to extend shutdown timer");
                    CheckError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_ExtendShutDownTimer, 0));
                    break;

                case EDSDK.StateEvent_Shutdown:
                    Logger.Error("CANON: Camera has suddenly disconnected");
                    Notification.ShowExternalError(string.Format(Loc.Instance["LblCanonCameraDisconnected"], Name), Loc.Instance["LblCanonDriverError"]);
                    Disconnect();
                    break;

                case EDSDK.StateEvent_InternalError:
                    Logger.Error("CANON: Canon SDK has encountered an internal error");
                    Notification.ShowExternalError(Loc.Instance["LblCanonSdkError"], Loc.Instance["LblCanonDriverError"]);
                    break;
            }

            return EDSDK.EDS_ERR_OK;
        }

        private uint Camera_SDKObjectEvent(uint inEvent, IntPtr inRef, IntPtr inContext) {
            Logger.Debug($"CANON: Received object event: 0x{inEvent:x}");

            if (inEvent == EDSDK.ObjectEvent_DirItemRequestTransfer) {
                this.DirectoryItem = inRef;
                try { bulbCompletionCTS?.Cancel(); } catch { }
                downloadExposure?.TrySetResult(true);
            }
            return EDSDK.EDS_ERR_OK;
        }

        private uint Camera_SDKPropertyEvent(uint inEvent, uint inPropertyID, uint inParam, IntPtr inContext) {
            Logger.Debug($"CANON: Received property event: 0x{inEvent:x}, propertyID: 0x{inPropertyID:x}, parameter: 0x{inParam:x}");

            if (inEvent == EDSDK.PropertyEvent_PropertyChanged) {
                EDSDK.EdsGetPropertyData(_cam, inPropertyID, 0, out uint data);
                Logger.Trace($"CANON: Property changed: 0x{inPropertyID:x} = 0x{data:x}");

                switch (inPropertyID) {
                    case EDSDK.PropID_AEMode:
                        Logger.Info($"CANON: Camera mode switched to {EDSDKLocal.AeModes[data]}");
                        break;

                    case EDSDK.PropID_BatteryLevel:
                        GetBatteryLevel();
                        break;
                }
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
            CheckError(SetProperty(EDSDK.PropID_ImageQuality, (uint)EDSDK.ImageQuality.EdsImageQuality_LR));
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
            var err = EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_AEMode, 0, out uint mode);
            if (err == EDSDK.EDS_ERR_INVALID_HANDLE) {
                throw new CameraConnectionLostException(string.Format(Loc.Instance["LblCanonCameraDisconnected"], Name));
            }

            if (mode == EDSDK.AEMode_Mamual) {
                Logger.Debug("CANON: Camera is in MANUAL mode");
                return true;
            } else {
                return false;
            }
        }

        private bool IsBulbMode() {
            bool isBulb = false;

            var err = EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_AEMode, 0, out uint mode);
            if (err == EDSDK.EDS_ERR_INVALID_HANDLE) {
                throw new CameraConnectionLostException(string.Format(Loc.Instance["LblCanonCameraDisconnected"], Name));
            }

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
                    isBulb = IsManualModeBulb = true;
                } else {
                    IsManualModeBulb = false;
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

        private void GetBatteryLevel() {
            try {
                if (!CheckError(EDSDK.EdsGetPropertyData(_cam, EDSDK.PropID_BatteryLevel, 0, out uint batteryLevel))) {
                    BatteryLevel = (int)batteryLevel;
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                BatteryLevel = -1;
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

                EDSDK.EdsDirectoryItemInfo directoryItemInfo;
                var memoryStreamHandle = IntPtr.Zero;
                var imageDataPointer = IntPtr.Zero;

                try {
                    using (token.Register(() => CancelDownloadExposure())) {
                        await downloadExposure.Task;
                    }

                    using (MyStopWatch.Measure("Canon - Image Download")) {
                        if (DirectoryItem == IntPtr.Zero) {
                            Logger.Error("CANON: No new image is ready for downlaod");
                            return null;
                        }
                        CheckAndThrowError(EDSDK.EdsGetDirectoryItemInfo(DirectoryItem, out directoryItemInfo));

                        // Create a memory stream to accept the image
                        CheckAndThrowError(EDSDK.EdsCreateMemoryStream(directoryItemInfo.Size, out memoryStreamHandle));

                        // Download image
                        Logger.Debug($"CANON: Downloading {directoryItemInfo.szFileName} ({directoryItemInfo.Size} bytes)");
                        CheckAndThrowError(EDSDK.EdsDownload(DirectoryItem, directoryItemInfo.Size, memoryStreamHandle));

                        // Complete download
                        CheckAndThrowError(EDSDK.EdsDownloadComplete(DirectoryItem));
                        Logger.Debug($"CANON: Image {directoryItemInfo.szFileName} downloaded");

                        token.ThrowIfCancellationRequested();
                    }

                    using (MyStopWatch.Measure("Canon - Creating Image Array")) {
                        //convert to memory stream
                        EDSDK.EdsGetPointer(memoryStreamHandle, out imageDataPointer);
                        EDSDK.EdsGetLength(memoryStreamHandle, out ulong length);

                        byte[] rawImageData = new byte[length];

                        // Move from unmanaged to managed code.
                        Marshal.Copy(imageDataPointer, rawImageData, 0, rawImageData.Length);

                        // Release directory item
                        if (this.DirectoryItem != IntPtr.Zero) {
                            CheckAndThrowError(EDSDK.EdsRelease(DirectoryItem));
                        }

                        // Release stream
                        if (memoryStreamHandle != IntPtr.Zero) {
                            CheckAndThrowError(EDSDK.EdsRelease(memoryStreamHandle));
                        }

                        token.ThrowIfCancellationRequested();

                        var metaData = new ImageMetaData();
                        metaData.FromCamera(this);
                        return this.exposureDataFactory.CreateRAWExposureData(
                            converter: profileService.ActiveProfile.CameraSettings.RawConverter,
                            rawBytes: rawImageData,
                            rawType: GetFileType(directoryItemInfo),
                            bitDepth: BitDepth,
                            metaData: metaData);
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
            try { bulbCompletionCTS?.Cancel(); } catch { }
            downloadExposure.TrySetCanceled();
        }

        public void SetBinning(short x, short y) {
        }

        public void SetupDialog() {
        }

        private string GetFileType(EDSDK.EdsDirectoryItemInfo directoryItemInfo) {
            if (directoryItemInfo.szFileName.EndsWith(".cr3", StringComparison.InvariantCultureIgnoreCase)) {
                return "cr3";
            } else {
                return "cr2";
            }
        }

        private void ValidateMode() {
            if (!IsManualMode() && !IsBulbMode()) {
                var result = MyMessageBox.Show(
                    Loc.Instance["LblEDCameraNotInManualMode"],
                    Loc.Instance["LblInvalidMode"],
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
                var result = MyMessageBox.Show(
                    Loc.Instance["LblEDCameraNotInManualMode"],
                    Loc.Instance["LblInvalidMode"],
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
                        var result = MyMessageBox.Show(
                            Loc.Instance["LblChangeToBulbMode"],
                            Loc.Instance["LblInvalidModeManual"],
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
                var result = MyMessageBox.Show(
                    Loc.Instance["LblChangeToManualMode"],
                    Loc.Instance["LblInvalidModeBulb"],
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxResult.OK);
                if (result == System.Windows.MessageBoxResult.OK) {
                    ValidateModeForExposure(exposureTime);
                } else {
                    throw new Exception("Invalid camera mode [Bulb] for taking exposures < 1s");
                }
            };
        }

        private Task bulbCompletionTask = null;
        private CancellationTokenSource bulbCompletionCTS = null;

        public void StartExposure(CaptureSequence sequence) {
            if (downloadExposure?.Task?.Status <= TaskStatus.Running) {
                Logger.Warning("An exposure was still in progress. Cancelling it to start another.");
                CancelDownloadExposure();
            }

            downloadExposure = new TaskCompletionSource<object>();
            var exposureTime = sequence.ExposureTime;
            bool useBulb = (IsManualMode() && exposureTime > 30.0) || (IsBulbMode() && exposureTime >= 1.0);

            ValidateModeForExposure(exposureTime);

            // Start exposure
            uint error = SendStartExposureCmd(useBulb);

            // This is to catch 450D/Rebel XSi cameras that do not support MLU exposures via USB.
            // TODO: implement MLU-like functionality by driving the camera in LiveView mode and taking the exposures via that
            if (error == EDSDK.EDS_ERR_TAKE_PICTURE_MIRROR_UP_NG) {
                throw new CameraExposureFailedException(string.Format(Loc.Instance["LblCanonMluNotSupported"], Name));
            }

            // Do mirror lockup
            if (MirrorLockupDelay > 0d) {
                Logger.Debug($"CANON: MLU: Releasing first shutter trigger");

                // Release the shutter button after the first press. The mirror should remain flipped (locked) up.
                SendStopExposureCmd(useBulb);

                // Sleep for the user-specified delay
                Logger.Debug($"CANON: MLU: Waiting {MirrorLockupDelay} seconds before 2nd trigger");
                Thread.Sleep(Convert.ToInt32(MirrorLockupDelay * 1000d));

                // Press the shutter button again to open the curtain and start the actual exposure
                Logger.Debug($"CANON: MLU: Starting 2nd trigger");
                SendStartExposureCmd(useBulb);
            }

            // Finish exposure
            if (useBulb) {
                /* Stop Exposure after exposure time */
                try { bulbCompletionCTS?.Cancel(); } catch { }
                bulbCompletionCTS = new CancellationTokenSource();
                bulbCompletionTask = Task.Run(async () => {
                    await CoreUtil.Wait(TimeSpan.FromSeconds(exposureTime), bulbCompletionCTS.Token);
                    if (!bulbCompletionCTS.IsCancellationRequested) {
                        SendStopExposureCmd(true);
                    }
                }, bulbCompletionCTS.Token);
            } else {
                // Immediately release shutter button when having a set exposure
                SendStopExposureCmd(false);
            }
        }

        private uint SendStartExposureCmd(bool useBulb) {
            uint error;
            const int PsbNonAF = (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_Completely_NonAF;

            if (useBulb) {
                /*
                 * Cameras that feature a Bulb ("B") mode on the mode selection dial use the PressShutterButton command to
                 * start and stop bulb exposures when in that mode. Cameras that lack a "B" mode on the mode selection dial
                 * and instead set bulb mode as a shutter speed while in Manual ("M") mode on the dial use BulbStart/BulbEnd.
                 */
                if (IsManualModeBulb && usesCameraCommandBulb) {
                    Logger.Debug("CANON: Initiating BULB mode exposure (via BulbStart)");
                    error = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_BulbStart, 0);

                    if (error == EDSDK.EDS_ERR_TAKE_PICTURE_MIRROR_UP_NG) return error;

                    // workaround: 500D is returns 44313 (0xAD19) for BulbStart yet it still successfully starts a bulb exposure.
                    // it's also an unknown error code, probably safe to assume OK here for other devices
                    if (error == 44313) error = EDSDK.EDS_ERR_OK;

                    if (error != EDSDK.EDS_ERR_OK) {
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

            return error;
            //CheckAndThrowError(error);
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

        public int USBLimitMax => -1;
        public int USBLimitMin => -1;
        public int USBLimitStep => -1;

        private int batteryLevel = -1;

        public int BatteryLevel {
            get => batteryLevel;
            private set => batteryLevel = value;
        }

        public void StopExposure() {
            SendStopExposureCmd(false);
        }

        private void SendStopExposureCmd(bool useBulb) {
            uint error;

            if (useBulb) {
                if (IsManualModeBulb && usesCameraCommandBulb) {
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
            uint err;
            int retries = 0;
            int propsize;
            do {
                err = EDSDK.EdsGetPropertySize(_cam, property, 0, out EDSDK.EdsDataType proptype, out propsize);
                if (err != EDSDK.EDS_ERR_OK && err != EDSDK.EDS_ERR_DEVICE_BUSY) {
                    return err;
                }
                err = EDSDK.EdsSetPropertyData(_cam, property, 0, propsize, value);
                if (err == EDSDK.EDS_ERR_DEVICE_BUSY) {
                    Thread.Sleep(1000);
                    retries++;
                }
            } while (err == EDSDK.EDS_ERR_DEVICE_BUSY && retries < 32);

            return err;
        }

        private bool CheckError(uint err, [CallerMemberName] string memberName = "") {
            if (err == EDSDK.EDS_ERR_OK) {
                return false;
            } else {
                Logger.Error(new Exception(string.Format(Loc.Instance["LblCanonErrorOccurred"], ErrorCodeToString(err))), memberName);
                return true;
            }
        }

        private void CheckAndThrowError(uint err, [CallerMemberName] string memberName = "") {
            if (err != EDSDK.EDS_ERR_OK) {
                var ex = new Exception(string.Format(Loc.Instance["LblCanonErrorOccurred"], ErrorCodeToString(err)));
                if (err == EDSDK.EDS_ERR_INVALID_HANDLE) {                    
                    ex = new CameraConnectionLostException(string.Format(Loc.Instance["LblCanonCameraDisconnected"], Name));
                }

                Logger.Error(ex, memberName);
                throw ex;
            }
        }

        private void TryToRegainHandle() {
            uint err = EDSDK.EdsGetCameraList(out var cameraList);
            if (err == EDSDK.EDS_ERR_OK) {
                int count;
                err = EDSDK.EdsGetChildCount(cameraList, out count);

                Logger.Info($"Found {count} Canon Cameras");
                for (int i = 0; i < count; i++) {
                    IntPtr cam;
                    err = EDSDK.EdsGetChildAtIndex(cameraList, i, out cam);

                    EDSDK.EdsDeviceInfo info;
                    err = EDSDK.EdsGetDeviceInfo(cam, out info);

                    if(info.szPortName == portName) {
                        Logger.Info("CANON: Successfully regained camera handle");
                        this._cam = cam;
                    }
                }
            }
        }

        private uint SingleAutoRetry(Func<uint> fn) {
            var err = fn();
            if(err == EDSDK.EDS_ERR_INVALID_HANDLE) {
                Logger.Info("CANON: Camera handle lost. Trying to regain handle to connect");
                TryToRegainHandle();
                err = fn();
            }
            return err;
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
                    CheckAndThrowError(SingleAutoRetry(() => EDSDK.EdsOpenSession(_cam)));

                    if (!Initialize()) {
                        Disconnect();
                        return false;
                    }
                    Connected = true;
                    RaiseAllPropertiesChanged();

                    return true;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblCanonDriverError"]);
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
        private bool IsManualModeBulb { get; set; } = false;

        public int BitDepth => (int)profileService.ActiveProfile.CameraSettings.BitDepth;

        public bool HasBattery => true;

        public double MirrorLockupDelay {
            get => profileService.ActiveProfile.CameraSettings.MirrorLockupDelay;
            set => profileService.ActiveProfile.CameraSettings.MirrorLockupDelay = value;
        }

        public void StartLiveView(CaptureSequence sequence) {
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
                            await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), token);
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

                        var metaData = new ImageMetaData();
                        metaData.FromCamera(this);
                        return exposureDataFactory.CreateImageArrayExposureData(
                            input: outArray,
                            width: bitmap.PixelWidth,
                            height: bitmap.PixelHeight,
                            bitDepth: 16,
                            isBayered: false,
                            metaData: metaData);
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
    }
}