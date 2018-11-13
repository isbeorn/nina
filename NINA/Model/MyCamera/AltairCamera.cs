using Altair;
using ASCOM.DeviceInterface;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    internal class AltairCamera : BaseINPC, ICamera {
        private AltairCam camera;

        public AltairCamera(AltairCam.InstanceV2 instance, IProfileService profileService) {
            this.profileService = profileService;
            this.Id = instance.id;
            this.Name = instance.displayname;
            this.Description = instance.model.name;
            this.PixelSizeX = instance.model.xpixsz;
            this.PixelSizeY = instance.model.ypixsz;
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
                    camera.get_Option(AltairCam.eOPTION.OPTION_TECTARGET, out var target);
                    return target / 10.0;
                } else {
                    return double.MinValue;
                }
            }
            set {
                if (CanSetTemperature) {
                    if (camera.put_Option(AltairCam.eOPTION.OPTION_TECTARGET, (int)(value * 10))) {
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
                return 0;
            }
        }

        public double ExposureMax {
            get {
                return double.PositiveInfinity;
            }
        }

        public short MaxBinX { get; private set; }

        public short MaxBinY { get; private set; }

        public double PixelSizeX { get; }

        public double PixelSizeY { get; }

        public bool CanSetTemperature {
            get {
                return camera.get_Temperature(out var temp);
            }
        }

        public bool CoolerOn {
            get {
                camera.get_Option(AltairCam.eOPTION.OPTION_TEC, out var cooler);
                return cooler == 1;
            }

            set {
                if (camera.put_Option(AltairCam.eOPTION.OPTION_TEC, value ? 1 : 0)) {
                    RaisePropertyChanged();
                }
            }
        }

        public double CoolerPower {
            get {
                /* todo */
                return 0.0;
            }
        }

        public string CameraState {
            get {
                /* todo */
                return string.Empty;
            }
        }

        public bool CanSubSample {
            get {
                /* todo */
                return false;
            }
        }

        public bool EnableSubSample {
            get {
                /* todo */
                return false;
            }

            set {
                /* todo */
            }
        }

        public int SubSampleX {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleY {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleWidth {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleHeight {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanShowLiveView {
            get {
                return true;
            }
        }

        public bool LiveViewEnabled {
            get {
                /* todo */
                return false;
            }

            set {
                /* todo */
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

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                return new AsyncObservableCollection<BinningMode>();
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
                return "Altair Astro SDK";
            }
        }

        public string DriverVersion {
            get {
                return AltairCam.Version();
            }
        }

        public void AbortExposure() {
            throw new NotImplementedException();
        }

        private void ReadOutBinning() {
            camera.get_Option(AltairCam.eOPTION.OPTION_BINNING, out var binning);

            switch (binning) {
                case 0x02:
                    MaxBinX = 2;
                    MaxBinY = 2;
                    break;

                case 0x03:
                    MaxBinX = 3;
                    MaxBinY = 3;
                    break;

                case 0x04:
                    MaxBinX = 4;
                    MaxBinY = 4;
                    break;

                case 0x82:
                case 0x83:
                case 0x84:
                default:
                    MaxBinX = 1;
                    MaxBinY = 1;
                    break;
            }
            BinningModes.Clear();
            for (short i = 0; i < MaxBinX; i++) {
                BinningModes.Add(new BinningMode(i, i));
            }
            BinX = 1;
            BinY = 1;
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task<bool>.Run(() => {
                var success = false;
                try {
                    camera = new AltairCam();
                    camera.Open(this.Id);

                    success = true;

                    /* Use maximum bit depth */
                    camera.put_Option(AltairCam.eOPTION.OPTION_BITDEPTH, 1);

                    /* Use Mono Mode */
                    camera.put_Option(AltairCam.eOPTION.OPTION_RAW, 1);

                    camera.put_AutoExpoEnable(false);

                    ReadOutBinning();

                    camera.get_Size(out var width, out var height);
                    this.CameraXSize = width;
                    this.CameraYSize = height;

                    /* Todo Readout flags */

                    /* Todo Sensor*/

                    if (!camera.put_Option(AltairCam.eOPTION.OPTION_TRIGGER, 1)) {
                        Logger.Warning("Could not set Altair Trigger manual mode");
                        Disconnect();
                    }

                    if (!camera.StartPushModeV2(new AltairCam.DelegateDataCallbackV2(OnImageCallback))) {
                        Logger.Warning("Could not start push mode");
                        Disconnect();
                    }

                    if (camera.MonoMode) {
                        SensorType = SensorType.Monochrome;
                    } else {
                        /* Todo Determine color matrix */
                        SensorType = SensorType.RGGB;
                        //camera.get_RawFormat(out var fourCC, out var bitDepth);
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
            Connected = false;
            camera.Close();
            camera = null;
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token) {
            await downloadExposure.Task;
            return await imageTask;
        }

        public async Task<ImageArray> DownloadLiveView(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void SetBinning(short x, short y) {
            if (x <= MaxBinX) {
                camera.put_Option(AltairCam.eOPTION.OPTION_BINNING, x);
            }
        }

        public void SetupDialog() {
        }

        public void StartExposure(double exposureTime, bool isLightFrame) {
            downloadExposure = new TaskCompletionSource<object>();
            camera.put_ExpoTime((uint)(exposureTime * 1000000));
            camera.Trigger(1);
        }

        private void OnImageCallback(IntPtr pData, ref AltairCam.FrameInfoV2 info, bool bSnap) {
            var size = (this.CameraXSize * this.CameraYSize * 2);
            var pointer = Marshal.AllocHGlobal(size);

            AltairCam.CopyMemory(pointer, pData, (uint)size);
            ushort[] arr = CopyToUShort(pData, size / 2);
            Marshal.FreeHGlobal(pointer);

            /* todo - format of pixels is not yet correct */

            imageTask = ImageArray.CreateInstance(arr, this.CameraXSize, this.CameraYSize, SensorType != SensorType.Monochrome, true, profileService.ActiveProfile.ImageSettings.HistogramResolution);
            downloadExposure.TrySetResult(true);
        }

        private TaskCompletionSource<object> downloadExposure;
        private Task<ImageArray> imageTask;

        private void OnAltairEvent(AltairCam.eEVENT ev) {
            switch (ev) {
                case AltairCam.eEVENT.EVENT_ERROR:
                    //OnEventError();
                    break;

                case AltairCam.eEVENT.EVENT_DISCONNECTED:
                    OnEventDisconnected();
                    break;

                case AltairCam.eEVENT.EVENT_EXPOSURE:
                    //OnEventExposure();
                    break;

                case AltairCam.eEVENT.EVENT_IMAGE:

                    break;

                case AltairCam.eEVENT.EVENT_STILLIMAGE:
                    PullImage();
                    break;

                case AltairCam.eEVENT.EVENT_TEMPTINT:
                    //OnEventTempTint();
                    break;
            }
        }

        private void OnEventDisconnected() {
            StopExposure();
            Disconnect();
        }

        private void PullImage() {
            //var bmp = new System.Drawing.Bitmap(this.CameraXSize, this.CameraYSize, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
            //var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

            int size = this.CameraXSize * this.CameraYSize * 2;
            var pointer = Marshal.AllocHGlobal(size);
            //int buffersize = (width * height * 16 + 7) / 8;

            camera.PullImageV2(pointer, 0, out var info);

            ushort[] arr = CopyToUShort(pointer, size / 2);
            Marshal.FreeHGlobal(pointer);

            var imageTask = ImageArray.CreateInstance(arr, this.CameraXSize, this.CameraYSize, SensorType != SensorType.Monochrome, true, profileService.ActiveProfile.ImageSettings.HistogramResolution);
        }

        private ushort[] CopyToUShort(IntPtr source, int length) {
            var destination = new ushort[length];
            unsafe {
                var sourcePtr = (ushort*)source;
                for (int i = 0; i < length; ++i) {
                    destination[i] = *sourcePtr++;
                }
            }
            return destination;
        }

        public void StartLiveView() {
            throw new NotImplementedException();
        }

        public void StopExposure() {
            throw new NotImplementedException();
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }
    }
}