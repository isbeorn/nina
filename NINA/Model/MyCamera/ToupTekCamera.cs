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

using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToupTek;

namespace NINA.Model.MyCamera {

    internal class ToupTekCamera : BaseINPC, ICamera {
        private ToupCam.eFLAG flags;
        private ToupCam camera;

        public ToupTekCamera(ToupCam.InstanceV2 instance, IProfileService profileService) {
            this.profileService = profileService;
            this.Id = instance.id;
            this.Name = instance.displayname;
            this.Description = instance.model.name;
            this.PixelSizeX = instance.model.xpixsz;
            this.PixelSizeY = instance.model.ypixsz;

            this.flags = (ToupCam.eFLAG)instance.model.flag;
        }

        private IProfileService profileService;

        public bool HasShutter {
            get {
                return false;
            }
        }

        public double Temperature {
            get {
                camera.get_Temperature(out var temp);
                return temp / 10.0;
            }
        }

        public double TemperatureSetPoint {
            get {
                if (CanSetTemperature) {
                    camera.get_Option(ToupCam.eOPTION.OPTION_TECTARGET, out var target);
                    return target / 10.0;
                } else {
                    return double.MinValue;
                }
            }
            set {
                if (CanSetTemperature) {
                    if (camera.put_Option(ToupCam.eOPTION.OPTION_TECTARGET, (int)(value * 10))) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private short binX;

        public short BinX {
            get {
                return binX;
            }

            set {
                binX = value;
                RaisePropertyChanged();
            }
        }

        private short binY;

        public short BinY {
            get {
                return binY;
            }

            set {
                binY = value;
                RaisePropertyChanged();
            }
        }

        public string SensorName {
            get {
                return string.Empty;
            }
        }

        public SensorType SensorType { get; private set; }

        public int CameraXSize { get; private set; }

        public int CameraYSize { get; private set; }

        public double ExposureMin {
            get {
                camera.get_ExpTimeRange(out var min, out var max, out var def);
                return min / 1000000.0;
            }
        }

        public double ExposureMax {
            get {
                camera.get_ExpTimeRange(out var min, out var max, out var def);
                return max / 1000000.0;
            }
        }

        public short MaxBinX { get; private set; }

        public short MaxBinY { get; private set; }

        public double PixelSizeX { get; }

        public double PixelSizeY { get; }

        private bool canGetTemperature;

        public bool CanGetTemperature {
            get {
                return canGetTemperature;
            }
            private set {
                canGetTemperature = value;
                RaisePropertyChanged();
            }
        }

        private bool canSetTemperature;

        public bool CanSetTemperature {
            get {
                return canSetTemperature;
            }
            private set {
                canSetTemperature = value;
                RaisePropertyChanged();
            }
        }

        public bool CoolerOn {
            get {
                camera.get_Option(ToupCam.eOPTION.OPTION_TEC, out var cooler);
                return cooler == 1;
            }

            set {
                if (camera.put_Option(ToupCam.eOPTION.OPTION_TEC, value ? 1 : 0)) {
                    //Toggle fan, if supported
                    camera.put_Option(ToupCam.eOPTION.OPTION_FAN, value ? 1 : 0);
                    RaisePropertyChanged();
                }
            }
        }

        private double coolerPower = 0.0;

        public double CoolerPower {
            get {
                return coolerPower;
            }
            private set {
                coolerPower = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource coolerPowerReadoutCts;

        /// <summary>
        /// This task will update cooler power based on TEC Volatage readout every three seconds
        /// Due to the fact that this value must not be updated more than every two seconds according to the documentation
        /// a helper method is required in case the device polling interval is faster than that.
        /// </summary>
        private void CoolerPowerUpdateTask() {
            Task.Run(async () => {
                coolerPowerReadoutCts = new CancellationTokenSource();
                try {
                    camera.get_Option(ToupCam.eOPTION.OPTION_TEC_VOLTAGE_MAX, out var maxVoltage);
                    while (true) {
                        coolerPowerReadoutCts.Token.ThrowIfCancellationRequested();

                        camera.get_Option(ToupCam.eOPTION.OPTION_TEC_VOLTAGE, out var voltage);

                        CoolerPower = 100 * voltage / (double)maxVoltage;

                        //Recommendation to not readout CoolerPower in less than two seconds.
                        await Task.Delay(TimeSpan.FromSeconds(3), coolerPowerReadoutCts.Token);
                    }
                } catch (OperationCanceledException) {
                }
            });
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

        public string CameraState {
            get {
                /* No State available */
                return string.Empty;
            }
        }

        public bool CanSubSample {
            get {
                /*
                 Currently ToupTek Cameras are fixed to certain subsample resolutions, which is incompatible to NINA's approach
                 */
                return false;
            }
        }

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        public bool CanShowLiveView {
            get {
                return true;
            }
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

        public bool HasBattery {
            get {
                return false;
            }
        }

        public int BatteryLevel {
            get {
                return -1;
            }
        }

        public int Offset {
            get {
                camera.get_Option(ToupCam.eOPTION.OPTION_BLACKLEVEL, out var level);
                return level;
            }
            set {
                if (!camera.put_Option(ToupCam.eOPTION.OPTION_BLACKLEVEL, value)) {
                } else {
                    RaisePropertyChanged();
                }
            }
        }

        public int OffsetMin {
            get {
                return 0;
            }
        }

        public int OffsetMax {
            get {
                return 31 * (1 << BitDepth - 8);
            }
        }

        public int USBLimit {
            get {
                return -1;
            }
            set {
            }
        }

        private bool canSetOffset;

        public bool CanSetOffset {
            get {
                return canSetOffset;
            }
            set {
                canSetOffset = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetUSBLimit {
            get {
                return false;
            }
        }

        public bool CanGetGain {
            get {
                return camera.get_ExpoAGain(out var gain);
            }
        }

        public bool CanSetGain {
            get {
                return GainMax != GainMin;
            }
        }

        public short GainMax {
            get {
                camera.get_ExpoAGainRange(out var min, out var max, out var def);
                return (short)max;
            }
        }

        public short GainMin {
            get {
                camera.get_ExpoAGainRange(out var min, out var max, out var def);
                return (short)min;
            }
        }

        public short Gain {
            get {
                camera.get_ExpoAGain(out var gain);
                return (short)gain;
            }

            set {
                if (value >= GainMin && value <= GainMax) {
                    camera.put_ExpoAGain((ushort)value);
                    RaisePropertyChanged();
                }
            }
        }

        public ArrayList Gains {
            get {
                return new ArrayList();
            }
        }

        private AsyncObservableCollection<BinningMode> binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (binningModes == null) {
                    binningModes = new AsyncObservableCollection<BinningMode>();
                }
                return binningModes;
            }
            private set {
                binningModes = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        private string id;

        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string name;

        public string Name {
            get {
                return name;
            }
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                return _connected;
            }
            set {
                _connected = value;
                if (!_connected) coolerPowerReadoutCts?.Cancel();
                RaisePropertyChanged();
            }
        }

        private string description;

        public string Description {
            get {
                return description;
            }
            set {
                description = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo {
            get {
                return "ToupTek SDK";
            }
        }

        public string DriverVersion {
            get {
                return ToupCam.Version();
            }
        }

        public void AbortExposure() {
            StopExposure();
        }

        private void ReadOutBinning() {
            /* Found no way to readout available binning modes. Assume 4x4 for all cams for now */
            BinningModes.Clear();
            MaxBinX = 4;
            MaxBinY = 4;
            for (short i = 1; i <= MaxBinX; i++) {
                BinningModes.Add(new BinningMode(i, i));
            }
            BinX = 1;
            BinY = 1;
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task<bool>.Run(() => {
                var success = false;
                try {
                    camera = new ToupCam();
                    camera.Open(this.Id);

                    success = true;

                    /* Use maximum bit depth */
                    if (!camera.put_Option(ToupCam.eOPTION.OPTION_BITDEPTH, 1)) {
                        throw new Exception("ToupTekCamera - Could not set bit depth");
                    }

                    /* Use RAW Mode */
                    if (!camera.put_Option(ToupCam.eOPTION.OPTION_RAW, 1)) {
                        throw new Exception("ToupTekCamera - Could not set RAW mode");
                    }

                    camera.put_AutoExpoEnable(false);

                    ReadOutBinning();

                    camera.get_Size(out var width, out var height);
                    this.CameraXSize = width;
                    this.CameraYSize = height;

                    /* Readout flags */
                    if ((this.flags & ToupCam.eFLAG.FLAG_TEC_ONOFF) != 0) {
                        /* Can set Target Temp */
                        CanSetTemperature = true;
                        TemperatureSetPoint = 20;
                        CoolerPowerUpdateTask();
                    }

                    if ((this.flags & ToupCam.eFLAG.FLAG_GETTEMPERATURE) != 0) {
                        /* Can get Target Temp */
                        CanGetTemperature = true;
                    }

                    if ((this.flags & ToupCam.eFLAG.FLAG_BLACKLEVEL) != 0) {
                        CanSetOffset = true;
                    }

                    if ((this.flags & ToupCam.eFLAG.FLAG_TRIGGER_SOFTWARE) == 0) {
                        throw new Exception("ToupTekCamera - This camera is not capable to be triggered by software and is not supported");
                    }

                    if (!camera.put_Option(ToupCam.eOPTION.OPTION_TRIGGER, 1)) {
                        throw new Exception("ToupTekCamera - Could not set Trigger manual mode");
                    }

                    if (!camera.StartPushModeV2(new ToupCam.DelegateDataCallbackV2(OnImageCallback))) {
                        throw new Exception("ToupTekCamera - Could not start push mode");
                    }

                    if (!camera.get_RawFormat(out var fourCC, out var bitDepth)) {
                        throw new Exception("ToupTekCamera - Unable to get format information");
                    }

                    this.BitDepth = (int)bitDepth;

                    if (camera.MonoMode) {
                        SensorType = SensorType.Monochrome;
                    } else {
                        /* Todo Determine color matrix */
                        SensorType = SensorType.RGGB;
                    }

                    Connected = true;
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return success;
            });
        }

        public void Disconnect() {
            coolerPowerReadoutCts?.Cancel();
            Connected = false;
            camera.Close();
            camera = null;
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token) {
            await downloadExposure.Task;
            return await imageTask;
        }

        public async Task<ImageArray> DownloadLiveView(CancellationToken token) {
            await downloadLiveExposure.Task;
            var arr = await imageTask;
            downloadLiveExposure = new TaskCompletionSource<object>();
            return arr;
        }

        public void SetBinning(short x, short y) {
            if (x <= MaxBinX) {
                camera.put_Option(ToupCam.eOPTION.OPTION_BINNING, x);
            }
        }

        public void SetupDialog() {
        }

        /// <summary>
        /// Sets the exposure time. When given exposure time is out of bounds it will set it to nearest bound.
        /// </summary>
        /// <param name="time">Time in seconds</param>
        private void SetExposureTime(double time) {
            if (time < ExposureMin) {
                time = ExposureMin;
            }
            if (time > ExposureMax) {
                time = ExposureMax;
            }

            var µsTime = (uint)(time * 1000000);
            if (!camera.put_ExpoTime(µsTime)) {
                throw new Exception("ToupTekCamera - Could not set exposure time");
            }
        }

        public void StartExposure(double exposureTime, bool isLightFrame) {
            downloadExposure = new TaskCompletionSource<object>();

            SetExposureTime(exposureTime);

            if (!camera.Trigger(1)) {
                throw new Exception("ToupTekCamera - Failed to trigger camera");
            }
        }

        private void OnImageCallback(IntPtr pData, ref ToupCam.FrameInfoV2 info, bool bSnap) {
            if ((LiveViewEnabled && downloadLiveExposure?.Task.IsCompleted != true) || (!LiveViewEnabled && downloadExposure?.Task.IsCompleted != true)) {
                int width = (int)info.width;
                int height = (int)info.height;

                var cameraDataToManaged = new CameraDataToManaged(pData, width, height, BitDepth);
                var arr = cameraDataToManaged.GetData();

                imageTask = ImageArray.CreateInstance(arr, width, height, BitDepth, SensorType != SensorType.Monochrome, true, profileService.ActiveProfile.ImageSettings.HistogramResolution);
                if (LiveViewEnabled) {
                    downloadLiveExposure?.TrySetResult(true);
                } else {
                    downloadExposure?.TrySetResult(true);
                }
            }
        }

        private TaskCompletionSource<object> downloadExposure;
        private TaskCompletionSource<object> downloadLiveExposure;
        private Task<ImageArray> imageTask;
        private int bitDepth;

        public int BitDepth {
            get {
                return bitDepth;
            }
            private set {
                bitDepth = value;
                RaisePropertyChanged();
            }
        }

        private void OnEventDisconnected() {
            StopExposure();
            Disconnect();
        }

        public void StartLiveView() {
            if (!camera.put_Option(ToupCam.eOPTION.OPTION_TRIGGER, 0)) {
                throw new Exception("ToupTekCamera - Could not set Trigger video mode");
            }
            downloadLiveExposure = new TaskCompletionSource<object>();
            LiveViewEnabled = true;
        }

        public void StopExposure() {
            if (!camera.Trigger(0)) {
                Logger.Warning("ToupTekCamera - Could not stop exposure");
            }
        }

        public void StopLiveView() {
            if (!camera.put_Option(ToupCam.eOPTION.OPTION_TRIGGER, 1)) {
                Disconnect();
                throw new Exception("ToupTekCamera - Could not set Trigger manual mode. Reconnect Camera!");
            }
            LiveViewEnabled = false;
        }
    }
}