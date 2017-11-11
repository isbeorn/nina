using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using NINA.Utility;
using EDSDKLib;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using System.Diagnostics;
using NINA.Utility.Notification;
using System.Collections;
using NINA.Utility.DCRaw;

namespace NINA.Model.MyCamera {
    class EDCamera : BaseINPC, ICamera {


        public EDCamera(IntPtr cam, EDSDK.EdsDeviceInfo info) {
            _cam = cam;
            Id = info.szDeviceDescription;
            Name = info.szDeviceDescription;
        }

        

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

        public double CCDTemperature {
            get {
                return double.NaN;
            }
        }

        public double SetCCDTemperature {
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
        public short BinY {
            get {
                return 1;
            }
            set {

            }
        }

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

        public bool CanSetCCDTemperature {
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
                var iso = ISOSpeeds.Where((x) => x.Key == value).FirstOrDefault().Value;
                if (HasError(SetProperty(EDSDK.PropID_ISOSpeed, iso))) {
                    Notification.ShowError("Could not set ISO.");                    
                }
                RaisePropertyChanged();
            }
        }


        private ArrayList _gains;
        public ArrayList Gains {
            get {
                if(_gains == null) {
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
                if(!Initialize()) { Disconnect(); }
                RaiseAllPropertiesChanged();
                return true;
            }
        }

        private bool Initialize() {
            if(IsManualMode()) {
                GetShutterSpeeds();
                GetISOSpeeds();
                SetSaveLocation();
                SubscribeEvents();
                return true;
            } else {
                return false;
            }          
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
                return;
            }

            EDSDK.EdsCapacity capacity = new EDSDK.EdsCapacity();
            capacity.NumberOfFreeClusters = 0x7FFFFFFF;
            capacity.BytesPerSector = 0x1000;
            capacity.Reset = 1;
            EDSDK.EdsSetCapacity(_cam, capacity);
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
            if(!isManual) {
                Notification.ShowError("Camera is not set to manual mode! Please set it to manual mode before connecting");
            }
            return isManual;
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

        public async Task<ImageArray> DownloadExposure(CancellationTokenSource tokenSource) {
            return await Task<ImageArray>.Run(async () => {

                while (!DownloadReady) {
                    await Task.Delay(100);
                    tokenSource.Token.ThrowIfCancellationRequested();
                }

                var sw = Stopwatch.StartNew();


                IntPtr stream = IntPtr.Zero;

                EDSDK.EdsDirectoryItemInfo directoryItemInfo;


                if (HasError(EDSDK.EdsGetDirectoryItemInfo(this.DirectoryItem, out directoryItemInfo))) {
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
                tokenSource.Token.ThrowIfCancellationRequested();

                Debug.Print("Download from Camera: " + sw.Elapsed);
                sw.Restart();


                //convert to memory stream
                IntPtr pointer; //pointer to image stream
                EDSDK.EdsGetPointer(stream, out pointer);

                ulong length = 0;
                EDSDK.EdsGetLength(stream, out length);

                byte[] bytes = new byte[length];

                //Move from unmanaged to managed code.
                Marshal.Copy(pointer, bytes, 0, bytes.Length);

                Debug.Print("Getting pixels to managed code : " + sw.Elapsed);
                sw.Restart();

                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(bytes);
                var fileextension = ".cr2";
                System.IO.FileStream filestream = new System.IO.FileStream(DCRaw.TMPIMGFILEPATH + fileextension, System.IO.FileMode.Create);
                memoryStream.WriteTo(filestream);

                memoryStream.Dispose();
                filestream.Dispose();

                Debug.Print("Write temp file: " + sw.Elapsed);
                sw.Restart();

                var iarr = await new DCRaw().ConvertToImageArray(fileextension,tokenSource.Token);

                Debug.Print("Get Pixels from temp tiff: " + sw.Elapsed);
                sw.Restart();
                tokenSource.Token.ThrowIfCancellationRequested();
               
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

                return iarr;
            });
        }

        public void SetBinning(short x, short y) {

        }

        public void SetupDialog() {

        }

        public void StartExposure(double exposureTime, bool isLightFrame) {
            DownloadReady = false;
            if (exposureTime < 1) {
                SetExposureTime(exposureTime);
            } else {
                SetExposureTime(double.MaxValue);
            }

            /* Start exposure */
            if(HasError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_Completely_NonAF))) {
                Notification.ShowError("Could not start camera exposure");
            }
            DateTime d = DateTime.Now;
            /*Stop Exposure after exposure time */
            Task.Run(() => {
                exposureTime = exposureTime * 1000;
                do {
                    Thread.Sleep(100);
                } while ((DateTime.Now - d).TotalMilliseconds < exposureTime);
                                
                if(HasError(EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF))) {
                    Notification.ShowError("Could not stop camera exposure");
                }
                
            });

        }
        

        private void SetExposureTime(double exposureTime) {
            double key;
            if (exposureTime != double.MaxValue) {
                var l = new List<double>(ShutterSpeeds.Keys);
                key = l.Aggregate((x, y) => Math.Abs(x - exposureTime) < Math.Abs(y - exposureTime) ? x : y);
            } else {
                key = double.MaxValue;
            }

            /* Shutter speed to Bulb */
            if (HasError(SetProperty(EDSDK.PropID_Tv, ShutterSpeeds[key]))) {
                Notification.ShowError("Unable to set exposure time");                
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

        public void StopExposure() {
            var err = EDSDK.EdsSendCommand(_cam, EDSDK.CameraCommand_PressShutterButton, (int)EDSDK.EdsShutterButton.CameraCommand_ShutterButton_OFF);
        }

        public void UpdateValues() {
            //throw new NotImplementedException();
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
                //Todo React to error
                return true;
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                uint err = EDSDK.EdsOpenSession(_cam);
                if (err != (uint)EDSDK.EDS_ERR.OK) {
                    return false;
                }
                else {
                    Connected = true;
                    if (!Initialize()) { Disconnect(); }
                    RaiseAllPropertiesChanged();
                    return true;
                }
            });
        }
    }

}
