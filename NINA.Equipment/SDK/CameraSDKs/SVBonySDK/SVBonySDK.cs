#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.SVBonySDK {

    public class SVBonySDK : ISVBonySDK {

        [ExcludeFromCodeCoverage]
        public SVBonySDK(int id) : this(id, new SVBonyPInvokeProxy()) {
        }

        public SVBonySDK(int id, ISVBonyPInvokeProxy sVBonyPInvoke) {
            this.id = id;
            this.sVBonyPInvoke = sVBonyPInvoke;
            this.bitDepth = 16;
            this.formats = new List<SVB_IMG_TYPE>();
            this.controls = new Dictionary<SVB_CONTROL_TYPE, SVBonyControl>();
        }

        private Dictionary<SVB_CONTROL_TYPE, SVBonyControl> controls;
        private List<SVB_IMG_TYPE> formats;
        private int bitDepth;
        private int id;
        private ISVBonyPInvokeProxy sVBonyPInvoke;

        public bool Connected { get; private set; }

        /// <summary>
        /// At least with the SVBony 305M Pro there is a problem with the offset setting when disconnected with offset greater 0.
        /// On a second connection this new offset seems to be considered as offset 0 and anything below that will even clip further below.
        /// This fix here should be removed once fixed by the SDK.
        ///
        /// Steps to reproduce without fix:
        /// Connect to Camera
        /// Set offset to max e.g. 255
        /// Take exposure
        /// The image should have a proper offset visible
        /// Disconnect camera
        /// Connect camera again
        /// Set offset to max again e.g. 255
        /// Take exposure
        /// The image shows now proper offset and is mostly black
        /// </summary>
        private void FixOffsetQuirk() {
            sVBonyPInvoke.SVBOpenCamera(id);

            sVBonyPInvoke.SVBSetControlValue(id, SVB_CONTROL_TYPE.SVB_GAIN, 0, SVB_BOOL.SVB_FALSE);
            sVBonyPInvoke.SVBSetControlValue(id, SVB_CONTROL_TYPE.SVB_BLACK_LEVEL, 0, SVB_BOOL.SVB_FALSE);

            sVBonyPInvoke.SVBCloseCamera(id);
        }

        public void Connect() {
            controls.Clear();

            FixOffsetQuirk();

            CheckAndThrowError(sVBonyPInvoke.SVBOpenCamera(id));

            CheckAndThrowError(sVBonyPInvoke.SVBSetCameraMode(id, SVB_CAMERA_MODE.SVB_MODE_TRIG_SOFT));

            CheckAndThrowError(sVBonyPInvoke.SVBGetCameraProperty(id, out var properties));

            formats.Clear();
            if (properties.SupportedVideoFormat != null) {
                for (int i = 0; i < properties.SupportedVideoFormat.Length; i++) {
                    var format = properties.SupportedVideoFormat[i];
                    if (format == SVB_IMG_TYPE.SVB_IMG_END) { break; }
                    formats.Add(format);
                }
            }

            SetHighestBitDepth();

            sVBonyPInvoke.SVBGetNumOfControls(id, out var numOfControls);

            for (int i = 0; i < numOfControls; i++) {
                sVBonyPInvoke.SVBGetControlCaps(id, i, out var caps);
                if (!controls.ContainsKey(caps.ControlType)) {
                    controls.Add(caps.ControlType, new SVBonyControl(i, caps));

                    Logger.Trace($"Found control {caps.description} - default: {caps.DefaultValue}, min: {caps.MinValue}, max: {caps.MaxValue}");
                    // Set all to default
                    SetControlValue(caps.ControlType, caps.DefaultValue);
                }
            }

            SetROI(0, 0, properties.MaxWidth, properties.MaxHeight, 1);

            CheckAndLogError(sVBonyPInvoke.SVBStartVideoCapture(id));

            Connected = true;
        }

        public int GetBitDepth() {
            return bitDepth;
        }

        private void SetHighestBitDepth() {
            if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_RAW16)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_RAW16));
                bitDepth = 16;
                return;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_Y16)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_Y16));
                bitDepth = 16;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_RAW14)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_RAW14));
                bitDepth = 14;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_Y14)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_Y14));
                bitDepth = 14;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_RAW12)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_RAW12));
                bitDepth = 12;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_Y12)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_Y12));
                bitDepth = 12;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_RAW10)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_RAW10));
                bitDepth = 10;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_Y10)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_Y10));
                bitDepth = 10;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_RAW8)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_RAW8));
                bitDepth = 8;
            } else if (formats.Contains(SVB_IMG_TYPE.SVB_IMG_Y8)) {
                CheckAndLogError(sVBonyPInvoke.SVBSetOutputImageType(id, SVB_IMG_TYPE.SVB_IMG_Y8));
                bitDepth = 8;
            }
        }

        public bool SetROI(int startX, int startY, int width, int height, int binning) {
            var (oldX, oldY, oldWidth, oldHeight, oldBin) = GetROI();
            var (maxWidth, maxHeight) = GetDimensions();

            if (oldX != startX || oldY != startY || oldWidth != width || oldHeight != height || oldBin != binning) {
                CheckAndLogError(sVBonyPInvoke.SVBStopVideoCapture(id));
                startX = startX / binning;
                startY = startY / binning;
                width = width / binning;
                height = height / binning;
                if ((!(binning == 1 && maxWidth == width && maxHeight == height)) && GetSensorInfo() != SensorType.Monochrome) {
                    width = width - width % 2;
                    height = height - height % 2;
                }
                Logger.Debug($"Setting ROI to {startX}x{startY}:{width}x{height} with binning {binning}");
                var result = CheckAndLogError(sVBonyPInvoke.SVBSetROIFormat(id, startX, startY, width, height, binning));
                CheckAndLogError(sVBonyPInvoke.SVBStartVideoCapture(id));
                return result;
            } else {
                return true;
            }
        }

        public (int, int, int, int, int) GetROI() {
            CheckAndLogError(sVBonyPInvoke.SVBGetROIFormat(id, out var startX, out var startY, out var width, out var height, out var binning));
            return (startX, startY, width, height, binning);
        }

        private CancellationTokenSource exposureCts;
        private Task<ushort[]> exposureTask;
        private object lockobj = new object();
        public DateTime StartExposure(double exposureTime, int width, int height) {
            lock(lockobj) {
                var previousExposureToken = exposureCts?.Token;
                try { exposureCts?.Dispose(); } catch (Exception) { }

                exposureCts = new CancellationTokenSource();
                DateTime exposureStart = DateTime.UtcNow;
                exposureTask = GetExposureInternal(exposureTime, width, height, previousExposureToken);
                return exposureStart;
            }
        }
        
        
        public Task<ushort[]> GetExposure(double exposureTime, int width, int height, CancellationToken ct) {
            lock(lockobj) {
                if(ct.IsCancellationRequested) {
                    try { exposureCts?.Cancel(); } catch { }
                }
                exposureCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                return exposureTask;
            }
        }

        public void StopExposure() {
            try {
                lock (lockobj) {
                    exposureCts?.Cancel();
                }
            } catch (Exception) { }
        }

        public bool IsExposureReady() {
            lock (lockobj) {
                return exposureTask.IsCompleted;
            }
        }

        private async Task<ushort[]> GetExposureInternal(double exposureTime, int width, int height, CancellationToken? previousExposureCancellation) {
            int size = width * height;
            ushort[] buffer = new ushort[size];
            int buffersize = width * height * 2;

            var transformedExposureTime = (long)(exposureTime * 1000000d);
            if (transformedExposureTime > int.MaxValue) { transformedExposureTime = int.MaxValue; }

            SetControlValue(SVB_CONTROL_TYPE.SVB_EXPOSURE, (int)transformedExposureTime);

            if(previousExposureCancellation?.IsCancellationRequested == true) { 
                // Restart capture mode on cancelled exposure
                ushort[] tempbuffer = new ushort[size];
                CheckAndLogError(sVBonyPInvoke.SVBStopVideoCapture(id));
                CheckAndLogError(sVBonyPInvoke.SVBStartVideoCapture(id));

                while (sVBonyPInvoke.SVBGetVideoDataMono16(id, tempbuffer, buffersize, (int)(exposureTime * 2 * 100) + 500) == SVB_ERROR_CODE.SVB_SUCCESS) {
                    // See if there are still temp images in the buffer
                    await Task.Delay(100);
                };
            }

            CheckAndThrowError(sVBonyPInvoke.SVBSendSoftTrigger(id));


            try {
                if (exposureTime > 0.1) {
                    await CoreUtil.Wait(TimeSpan.FromSeconds(exposureTime), exposureCts.Token);
                }

                while (true) {
                    if (!Connected) {
                        break;
                    }

                    exposureCts.Token.ThrowIfCancellationRequested();

                    if (sVBonyPInvoke.SVBGetVideoDataMono16(id, buffer, buffersize, (int)(exposureTime * 2 * 100) + 500) == SVB_ERROR_CODE.SVB_SUCCESS) {
                        return buffer;
                    }
                    await Task.Delay(10, exposureCts.Token);
                }
            } catch(OperationCanceledException) { 
            } catch (AccessViolationException ex) {
                Logger.Error($"{nameof(SVBonySDK)} - Access Violation Exception occurred during frame download!", ex);
            } catch (Exception ex) {
                Logger.Error($"{nameof(SVBonySDK)} - Unexpected exception occurred during frame download!", ex);
            }

            if (exposureCts.Token.IsCancellationRequested) {
                // Restart video capture on cancellation
                CheckAndLogError(sVBonyPInvoke.SVBStopVideoCapture(id));
                CheckAndLogError(sVBonyPInvoke.SVBStartVideoCapture(id));
                await Task.Delay(100);

                while (sVBonyPInvoke.SVBGetVideoDataMono16(id, buffer, buffersize, (int)(exposureTime * 2 * 100) + 500) == SVB_ERROR_CODE.SVB_SUCCESS) {
                    // See if there are still temp images in the buffer
                    await Task.Delay(100);
                };
            }

            return null;
        }

        public int[] GetBinningInfo() {
            sVBonyPInvoke.SVBGetCameraProperty(id, out var property);
            List<int> binnings = new List<int>();
            for (int i = 0; i < property.SupportedBins.Length; i++) {
                var bin = property.SupportedBins[i];
                if (bin == 0) { break; }
                binnings.Add(bin);
            }

            return binnings.ToArray();
        }

        public void Disconnect() {
            CheckAndLogError(sVBonyPInvoke.SVBStopVideoCapture(id));
            CheckAndLogError(sVBonyPInvoke.SVBCloseCamera(id));

            controls.Clear();
            Connected = false;
        }

        public bool SetGain(int value) {
            Logger.Trace($"Setting gain to {value}");
            return SetControlValue(SVB_CONTROL_TYPE.SVB_GAIN, value);
        }

        public int GetGain() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_GAIN);
        }

        public int GetMinGain() {
            return GetMinControlValue(SVB_CONTROL_TYPE.SVB_GAIN);
        }

        public int GetMaxGain() {
            return GetMaxControlValue(SVB_CONTROL_TYPE.SVB_GAIN);
        }

        public bool SetOffset(int value) {
            Logger.Trace($"Setting offset to {value}");
            return SetControlValue(SVB_CONTROL_TYPE.SVB_BLACK_LEVEL, value);
        }

        public int GetOffset() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_BLACK_LEVEL);
        }

        public int GetMinOffset() {
            return GetMinControlValue(SVB_CONTROL_TYPE.SVB_BLACK_LEVEL);
        }

        public int GetMaxOffset() {
            return GetMaxControlValue(SVB_CONTROL_TYPE.SVB_BLACK_LEVEL);
        }

        public bool SetUSBLimit(int value) {
            return SetControlValue(SVB_CONTROL_TYPE.SVB_FRAME_SPEED_MODE, value);
        }

        public int GetUSBLimit() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_FRAME_SPEED_MODE);
        }

        public int GetMinUSBLimit() {
            return GetMinControlValue(SVB_CONTROL_TYPE.SVB_FRAME_SPEED_MODE);
        }

        public int GetMaxUSBLimit() {
            return GetMaxControlValue(SVB_CONTROL_TYPE.SVB_FRAME_SPEED_MODE);
        }

        public double GetPixelSize() {
            if (CheckAndLogError(sVBonyPInvoke.SVBGetSensorPixelSize(id, out float size))) {
                return Math.Round(size, 2);
            } else {
                return double.NaN;
            }
        }

        public double GetMinExposureTime() {
            return GetMinControlValue(SVB_CONTROL_TYPE.SVB_EXPOSURE) / 1000000d;
        }

        public double GetMaxExposureTime() {
            return GetMaxControlValue(SVB_CONTROL_TYPE.SVB_EXPOSURE) / 1000000d;
        }

        public SensorType GetSensorInfo() {
            CheckAndLogError(sVBonyPInvoke.SVBGetCameraProperty(id, out var property));
            if (property.IsColorCam == SVB_BOOL.SVB_TRUE) {
                switch (property.BayerPattern) {
                    case SDK.CameraSDKs.SVBonySDK.SVB_BAYER_PATTERN.SVB_BAYER_GB:
                        return SensorType.GBRG;

                    case SDK.CameraSDKs.SVBonySDK.SVB_BAYER_PATTERN.SVB_BAYER_GR:
                        return SensorType.GRBG;

                    case SDK.CameraSDKs.SVBonySDK.SVB_BAYER_PATTERN.SVB_BAYER_BG:
                        return SensorType.BGGR;

                    case SDK.CameraSDKs.SVBonySDK.SVB_BAYER_PATTERN.SVB_BAYER_RG:
                        return SensorType.RGGB;

                    default:
                        return SensorType.BGGR;
                };
            }

            return SensorType.Monochrome;
        }

        public (int, int) GetDimensions() {
            CheckAndLogError(sVBonyPInvoke.SVBGetCameraProperty(id, out var property));
            return ((int)property.MaxWidth, (int)property.MaxHeight);
        }

        public bool HasTemperatureReadout() {
            return controls.ContainsKey(SVB_CONTROL_TYPE.SVB_CURRENT_TEMPERATURE);
        }

        public bool HasTemperatureControl() {
            CheckAndLogError(sVBonyPInvoke.SVBGetCameraPropertyEx(id, out var property));
            return (property.bSupportControlTemp == SVB_BOOL.SVB_TRUE);
        }

        public bool SetTargetTemperature(double temperature) {
            var convertedTemp = temperature * 10;
            var nearest = (int)Math.Round(convertedTemp);

            var minTemperatureSetpoint = GetMinControlValue(SVB_CONTROL_TYPE.SVB_TARGET_TEMPERATURE);
            var maxTemperatureSetpoint = GetMaxControlValue(SVB_CONTROL_TYPE.SVB_TARGET_TEMPERATURE);

            if (nearest > maxTemperatureSetpoint) {
                nearest = maxTemperatureSetpoint;
            } else if (nearest < minTemperatureSetpoint) {
                nearest = minTemperatureSetpoint;
            }
            return SetControlValue(SVB_CONTROL_TYPE.SVB_TARGET_TEMPERATURE, nearest);
        }

        public double GetTargetTemperature() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_TARGET_TEMPERATURE) / 10d;
        }

        public double GetTemperature() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_CURRENT_TEMPERATURE) / 10d;
        }

        public bool SetCooler(bool onOff) {
            return SetControlValue(SVB_CONTROL_TYPE.SVB_COOLER_ENABLE, onOff ? 1 : 0);
        }

        public bool GetCoolerOnOff() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_COOLER_ENABLE) > 0 ? true : false;
        }

        public double GetCoolerPower() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_COOLER_POWER);
        }

        public bool GetBadPixelCorrection() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_BAD_PIXEL_CORRECTION_ENABLE) > 0 ? true : false;
        }

        public bool SetBadPixelCorrection(bool onOff) {
            return SetControlValue(SVB_CONTROL_TYPE.SVB_BAD_PIXEL_CORRECTION_ENABLE, onOff ? 1 : 0);
        }
        public int GetBadPixelCorrectionThreshold() {
            return GetControlValue(SVB_CONTROL_TYPE.SVB_BAD_PIXEL_CORRECTION_THRESHOLD);
        }
        public int GetMinBadPixelCorrectionThreshold() {
            return GetMinControlValue(SVB_CONTROL_TYPE.SVB_BAD_PIXEL_CORRECTION_THRESHOLD);
        }
        public int GetMaxBadPixelCorrectionThreshold() {
            return GetMaxControlValue(SVB_CONTROL_TYPE.SVB_BAD_PIXEL_CORRECTION_THRESHOLD);
        }

        public bool SetBadPixelCorrectionThreshold(int threshold) {
            return SetControlValue(SVB_CONTROL_TYPE.SVB_BAD_PIXEL_CORRECTION_THRESHOLD, threshold);
        }

        private int GetControlValue(SVB_CONTROL_TYPE type) {
            if (controls.TryGetValue(type, out var control)) {
                if (CheckAndLogError(sVBonyPInvoke.SVBGetControlValue(id, type, out var value, out var auto))) {
                    return (int)value;
                }
            }
            return -1;
        }

        private int GetMinControlValue(SVB_CONTROL_TYPE type) {
            if (controls.TryGetValue(type, out var control)) {
                return (int)control.Min;
            }
            return -1;
        }

        private int GetMaxControlValue(SVB_CONTROL_TYPE type) {
            if (controls.TryGetValue(type, out var control)) {
                return (int)control.Max;
            }
            return -1;
        }

        private bool SetControlValue(SVB_CONTROL_TYPE type, int value) {
            if (controls.TryGetValue(type, out var control)) {
                if (value < control.Min) { value = control.Min; }
                if (value > control.Max) { value = control.Max; }

                var oldValue = GetControlValue(type);
                if (oldValue != value) {
                    Logger.Trace($"Setting control {type} to {value}");

                    return CheckAndLogError(sVBonyPInvoke.SVBSetControlValue(id, type, value, SVB_BOOL.SVB_FALSE));
                }
                return true;
            }
            return false;
        }

        private bool CheckAndLogError(SVB_ERROR_CODE code) {
            if (code == SVB_ERROR_CODE.SVB_SUCCESS) { return true; }

            Logger.Error(code.ToString());
            return false;
        }

        private void CheckAndThrowError(SVB_ERROR_CODE code) {
            if (code == SVB_ERROR_CODE.SVB_SUCCESS) { return; }

            throw new Exception(code.ToString());
        }

        public bool HasDewHeater() {
            return false;
        }

        public bool IsDewHeaterOn() {
            return false;
        }

        public bool SetDewHeater(int power) {
            throw new NotImplementedException();
        }

        public void StartVideoCapture(double exposureTime, int width, int height) {
            throw new NotImplementedException();
        }

        public void StopVideoCapture() {
            throw new NotImplementedException();
        }

        public Task<ushort[]> GetVideoCapture(double exposureTime, int width, int height, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public List<string> GetReadoutModes() {
            return new List<string> { "Default" };
        }

        public int GetReadoutMode() {
            return 0;
        }
        public void SetReadoutMode(int modeIndex) {
        }        

        public bool HasAdjustableFan() {
            return false;
        }

        public bool SetFanPercentage(int fanPercentage) {
            return true;
        }

        public int GetFanPercentage() {
            return 0;
        }
    }
}