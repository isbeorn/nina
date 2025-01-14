using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.SDK.CameraSDKs.SVBonySDK;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.ASTPANSDK {
    public class ASTPANSDK : IGenericCameraSDK {
        private int id;
        private IASTPANPInvokeProxy pInvoke;
        private int bitDepth;
        private List<IMG_TYPE> formats;
        private Dictionary<ASTPAN_AUTO_TYPE, AstpanControl> controls;
        private ASTPAN_CONFIG config = new ASTPAN_CONFIG();


        [ExcludeFromCodeCoverage]
        public ASTPANSDK(int id) : this(id, new ASTPANPInvokeProxy()) {
        }

        public ASTPANSDK(int id, IASTPANPInvokeProxy pInvoke) {
            this.id = id;
            this.pInvoke = pInvoke;
            this.bitDepth = 16;
            this.formats = new List<IMG_TYPE>();
            this.controls = new Dictionary<ASTPAN_AUTO_TYPE, AstpanControl>();
        }
        public bool Connected { get; private set; }

        public void Connect() {
            controls.Clear();

            CheckAndThrowError(pInvoke.ASTPANOpenCamera(id));
            CheckAndThrowError(pInvoke.ASTPANInitCamera(id));
                        
            CheckAndThrowError(pInvoke.ASTPANGetCameraInfoByID(id, out var properties));

            formats.Clear();
            
            for (int i = 0; i < properties.SupportedVideoFormat.Length; i++) {
                var format = properties.SupportedVideoFormat[i];
                if (format == IMG_TYPE.IMG_END) { break; }
                formats.Add(format);
            }

            ASTPAN_AUTO_CONFIG_INFO autoConfigInfo = new ASTPAN_AUTO_CONFIG_INFO();
            pInvoke.ASTPANGetAutoConfigInfo(id, ref autoConfigInfo);
            var idx = 0;
            foreach(ASTPAN_MUL_CONFIG control in autoConfigInfo.AutoConfigInfo) {
                var controlType = (ASTPAN_AUTO_TYPE)idx++;          
                if (!controls.ContainsKey(controlType)) {
                    controls.Add(controlType, new AstpanControl(controlType, control));

                    Logger.Trace($"Found control {control.r_description} - default: {control.r_DefaultValue}, min: {control.r_MinValue}, max: {control.r_MaxValue}");
                    // Set all to default
                    SetControlValue(controlType, control.r_DefaultValue);
                }
            }

            SetROI(0, 0, properties.MaxHeight, properties.MaxWidth, 1);
            SetHighestBitDepth();

            // Set important defaults            
            CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Flip, 0, 0)); 
            CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Gamma, 50, 0)); // Note: Gamma max value is currently incorrect and reported as 0
            CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_HighSpeedMode, 0, 0));
            CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_HardwareBin, 0, 0));
            CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Wb_r, 50, 0));
            CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Wb_b, 50, 0));
            CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_OverClock, 0, 0));
            CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_PatternAdjust, 0, 0));

            Connected = true;
        }

        public void StartVideoCapture(double exposureTime, int width, int height) {
            var transformedExposureTime = (long)(exposureTime * 1000000d);
            if (transformedExposureTime > int.MaxValue) { transformedExposureTime = int.MaxValue; }
            SetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Exposure, (int)transformedExposureTime);

            CheckAndThrowError(pInvoke.ASTPANStartVideoCapture(id));
        }

        public void StopVideoCapture() {
            CheckAndThrowError(pInvoke.ASTPANStopVideoCapture(id));
        }

        public async Task<ushort[]> GetVideoCapture(double exposureTime, int width, int height, CancellationToken ct) {
            var transformedExposureTime = (int)(exposureTime * 1000000d);

            int size = width * height;

            try {
                if (bitDepth > 8) {
                    ushort[] buffer = new ushort[size];
                    int buffersize = width * height;
                    buffersize *= 2;

                    if (CheckAndLogError(pInvoke.ASTPANGetVideoDataMono16(id, buffer, buffersize, (int)(exposureTime * 2 * 100) + 500))) {
                        return buffer;
                    }
                } else {
                    byte[] buffer = new byte[size];
                    int buffersize = width * height;

                    if (CheckAndLogError(pInvoke.ASTPANGetVideoDataMono8(id, buffer, buffersize, (int)(exposureTime * 2 * 100) + 500))) {
                        ushort[] data = new ushort[size];
                        for (int i = 0; i < buffer.Length; i++) {
                            data[i] = (ushort)(buffer[i] << 8);
                        }
                        return data;
                    }
                }                
            } catch (AccessViolationException ex) {
                Logger.Error($"{nameof(ASTPANSDK)} - Access Violation Exception occurred during frame download!", ex);
            } catch (Exception ex) {
                Logger.Error($"{nameof(ASTPANSDK)} - Unexpected exception occurred during frame download!", ex);
            }

            return null;
        }

        public DateTime StartExposure(double exposureTime, int width, int height) {
            long transformedExposureTime = (long)(exposureTime * 1000000d);
            if(transformedExposureTime > int.MaxValue) { transformedExposureTime = int.MaxValue; }
            SetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Exposure, (int)transformedExposureTime);

            DateTime exposureStart = DateTime.UtcNow;
            CheckAndThrowError(pInvoke.ASTPANStartExposure(id));
            return exposureStart;
        }

        public void StopExposure() {
            CheckAndThrowError(pInvoke.ASTPANStopExposure(id));
        }

        public bool IsExposureReady() {
            CheckAndThrowError(pInvoke.ASTPANGetExpStatus(id, out var status));

            return (status != ASTPAN_EXP_TYPE.ASTPAN_EXP_WORKING);
        }

        public async Task<ushort[]> GetExposure(double exposureTime, int width, int height, CancellationToken ct) {
            var transformedExposureTime = (int)(exposureTime * 1000000d);

            int size = width * height;

            try {
                var ready = IsExposureReady();                
                while (!ready) {
                    if (!Connected) {
                        break;
                    }

                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    await CoreUtil.Wait(TimeSpan.FromMilliseconds(10), ct);
                    ready = IsExposureReady();

                }
                if (ready) {
                    if(bitDepth > 8) {
                        ushort[] buffer = new ushort[size];
                        int buffersize = width * height;
                        buffersize *= 2;

                        if (CheckAndLogError(pInvoke.ASTPANGetDataAfterExpMono16(id, buffer, buffersize))) {
                            return buffer;
                        }
                    } else {
                        byte[] buffer = new byte[size];
                        int buffersize = width * height;

                        if (CheckAndLogError(pInvoke.ASTPANGetDataAfterExpMono8(id, buffer, buffersize))) {
                            ushort[] data = new ushort[size];
                            for (int i = 0; i < buffer.Length; i++) {
                                data[i] = (ushort)(buffer[i] << 8);
                            }
                            return data;
                        }
                    }
                    
                }
            } catch (AccessViolationException ex) {
                Logger.Error($"{nameof(ASTPANSDK)} - Access Violation Exception occurred during frame download!", ex);
            } catch (Exception ex) {
                Logger.Error($"{nameof(ASTPANSDK)} - Unexpected exception occurred during frame download!", ex);
            }

            return null;
        }

        public int GetBitDepth() {
            return bitDepth;
        }
        public int[] GetBinningInfo() {
            pInvoke.ASTPANGetCameraInfoByID(id, out var info);

            List<int> binnings = new List<int>();
            
            foreach(var bin in info.SupportedBins) {
                if (bin == 0) { break; }
                binnings.Add(bin);
            }           

            return binnings.ToArray();
        }
        public void Disconnect() {
            CheckAndLogError(pInvoke.ASTPANCloseCamera(id));

            controls.Clear();
            Connected = false;
        }

        public bool SetGain(int value) {
            Logger.Trace($"Setting gain to {value}");
            return SetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Gain, value);
        }

        public int GetGain() {
            return GetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Gain);
        }

        public int GetMinGain() {
            return GetMinControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Gain);
        }

        public int GetMaxGain() {
            return GetMaxControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Gain);
        }

        public bool SetOffset(int value) {
            Logger.Trace($"Setting offset to {value}");
            return SetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Offset, value);
        }

        public int GetOffset() {
            return GetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Offset);
        }

        public int GetMinOffset() {
            return GetMinControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Offset);
        }

        public int GetMaxOffset() {
            return GetMaxControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Offset);
        }

        public bool SetUSBLimit(int value) {
            return SetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_BandwidthOverload, value);
        }

        public int GetUSBLimit() {
            return GetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_BandwidthOverload);
        }

        public int GetMinUSBLimit() {
            return GetMinControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_BandwidthOverload);
        }

        public int GetMaxUSBLimit() {
            return GetMaxControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_BandwidthOverload);
        }
        public double GetPixelSize() {
            pInvoke.ASTPANGetCameraInfoByID(id, out var info);
            return Math.Round(info.PixelSize, 2);
        }

        public double GetMinExposureTime() {
            return GetMinControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Exposure) / 1000000d;
        }

        public double GetMaxExposureTime() {
            return GetMaxControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Exposure) / 1000000d;
        }
        public SensorType GetSensorInfo() {
            pInvoke.ASTPANGetCameraInfoByID(id, out var info);
            if (info.IsColorCam == 1) {
                switch (info.BayerPattern) {
                    case BAYER_PATTERN.BAYER_GB:
                        return SensorType.GBRG;

                    case BAYER_PATTERN.BAYER_GR:
                        return SensorType.GRBG;

                    case BAYER_PATTERN.BAYER_BG:
                        return SensorType.BGGR;

                    case BAYER_PATTERN.BAYER_RG:
                        return SensorType.RGGB;

                    default:
                        return SensorType.BGGR;
                };
            }

            return SensorType.Monochrome;
        }

        public (int, int) GetDimensions() {
            pInvoke.ASTPANGetCameraInfoByID(id, out var info);
            return (info.MaxWidth, info.MaxHeight);
        }

        public bool HasTemperatureReadout() {
            return controls.ContainsKey(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Temperature);
        }

        public bool HasTemperatureControl() {
            pInvoke.ASTPANGetCameraInfoByID(id, out var info);
            return info.IsCoolerCam > 0;
        }
        public bool SetTargetTemperature(double temperature) {            
            int nearest = (int)temperature;

            var minTemperatureSetpoint = GetMinControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_TargetTemp); 
            var maxTemperatureSetpoint = GetMaxControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_TargetTemp); 

            if (nearest > maxTemperatureSetpoint) {
                nearest = maxTemperatureSetpoint;
            } else if (nearest < minTemperatureSetpoint) {
                nearest = minTemperatureSetpoint;
            }
            return CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_TargetTemp, nearest, 0));
        }

        public double GetTargetTemperature() {
            return GetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_TargetTemp);
        }

        public double GetTemperature() {
            return GetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_Temperature) / 10d;
        }

        public bool SetCooler(bool onOff) {
            return SetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_CoolerOn, onOff ? 1 : 0);
        }

        public bool GetCoolerOnOff() {
            return GetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_CoolerOn) > 0 ? true : false;
        }

        public double GetCoolerPower() {
            return GetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_CoolerPowerPerc);
        }

        public bool SetROI(int startX, int startY, int width, int height, int binning) {
            var (oldX, oldY, oldWidth, oldHeight, oldBin) = GetROI();
            var (maxWidth, maxHeight) = GetDimensions();

            if (oldX != startX || oldY != startY || oldWidth != width || oldHeight != height || oldBin != binning) {

                startX = startX / binning;
                startY = startY / binning;
                width = width / binning;
                height = height / binning;
                if ((!(binning == 1 && maxWidth == width && maxHeight == height)) && GetSensorInfo() != SensorType.Monochrome) {
                    width = width - width % 2;
                    height = height - height % 2;
                }

                config.Wr_StartPosX = startX;
                config.Wr_StartPosY = startY;
                config.Wr_ROIWidth = width;
                config.Wr_ROIHeight = height;
                config.Wr_ROIBin = binning;

                Logger.Debug($"Setting ROI to {startX}x{startY}:{width}x{height} with binning {binning}");
                CheckAndLogError(pInvoke.ASTPANSetConfigValue(id, ref config, ASTPAN_CFG_TYPE.ASTPAN_CFG_Wr_ROI));
                var result = CheckAndLogError(pInvoke.ASTPANSetConfigValue(id, ref config, ASTPAN_CFG_TYPE.ASTPAN_CFG_Wr_StartPos));
                return result;
            } else {
                return true;
            }
        }
        public (int, int, int, int, int) GetROI() {
            CheckAndLogError(pInvoke.ASTPANGetConfigValue(id, ref config, ASTPAN_CFG_TYPE.ASTPAN_CFG_Wr_ROI));
            CheckAndLogError(pInvoke.ASTPANGetConfigValue(id, ref config, ASTPAN_CFG_TYPE.ASTPAN_CFG_Wr_StartPos));

            return (config.Wr_StartPosX, config.Wr_StartPosY, config.Wr_ROIWidth, config.Wr_ROIHeight, config.Wr_ROIBin);
        }

        public bool HasDewHeater() {
            if (controls.TryGetValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_AntiDewHedter, out var control)) {
                return control.Supported;
            }
            return false;
        }

        public bool IsDewHeaterOn() {
            var val = GetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_AntiDewHedter);
            return val > 0;
        }

        public bool SetDewHeater(int power) {
            return SetControlValue(ASTPAN_AUTO_TYPE.ASTPAN_AUTO_CFG_AntiDewHedter, power > 0 ? 1 : 0);
        }

        private int GetMinControlValue(ASTPAN_AUTO_TYPE type) {
            if (controls.TryGetValue(type, out var control)) {
                return control.Min;
            }
            return default;
        }

        private int GetMaxControlValue(ASTPAN_AUTO_TYPE type) {
            if (controls.TryGetValue(type, out var control)) {
                return control.Max;
            }
            return default;
        }


        private int GetControlValue(ASTPAN_AUTO_TYPE type) {
            if (controls.TryGetValue(type, out var control)) {
                if (CheckAndLogError(pInvoke.ASTPANGetAutoConfigValue(id, type, out var value, out var auto))) {
                    return (int)value;
                }
            }
            return -1;
        }

        private bool SetControlValue(ASTPAN_AUTO_TYPE type, int value) {
            if (controls.TryGetValue(type, out var control)) {
                if (value < control.Min) { value = control.Min; }
                if (value > control.Max) { value = control.Max; }

                var oldValue = GetControlValue(type);
                if (oldValue != value) {
                    Logger.Trace($"Setting control {type} to {value}");

                    return CheckAndLogError(pInvoke.ASTPANSetAutoConfigValue(id, type, value, 0));
                }
                Logger.Trace($"Control {type} was already set to {value}");
                return true;
            }
            return false;
        }

        private void SetHighestBitDepth() {
            if (formats.Contains(IMG_TYPE.IMG_RAW16)) {
                config.Wr_ROImg_type = IMG_TYPE.IMG_RAW16;
                CheckAndLogError(pInvoke.ASTPANSetConfigValue(id, ref config, ASTPAN_CFG_TYPE.ASTPAN_CFG_Wr_ROI));
                CheckAndLogError(pInvoke.ASTPANSetConfigValue(id, ref config, ASTPAN_CFG_TYPE.ASTPAN_CFG_Wr_StartPos));

                bitDepth = 16;
                return;
            } else if (formats.Contains(IMG_TYPE.IMG_RAW8)) {
                config.Wr_ROImg_type = IMG_TYPE.IMG_RAW8;
                CheckAndLogError(pInvoke.ASTPANSetConfigValue(id, ref config, ASTPAN_CFG_TYPE.ASTPAN_CFG_Wr_ROI));
                CheckAndLogError(pInvoke.ASTPANSetConfigValue(id, ref config, ASTPAN_CFG_TYPE.ASTPAN_CFG_Wr_StartPos));
                bitDepth = 8;
            } else {
                throw new NotSupportedException();
            }
        }

        private bool CheckAndLogError(ASTPAN_RET_TYPE code) {
            if (code == ASTPAN_RET_TYPE.ASTPAN_RET_SUCCESS) { return true; }

            Logger.Error(code.ToString());
            return false;
        }

        private void CheckAndThrowError(ASTPAN_RET_TYPE code) {
            if (code == ASTPAN_RET_TYPE.ASTPAN_RET_SUCCESS) { return; }

            throw new Exception(code.ToString());
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
