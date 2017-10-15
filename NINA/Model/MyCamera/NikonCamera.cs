using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using NINA.Utility;
using Nikon;
using NINA.Utility.DCRaw;

namespace NINA.Model.MyCamera
{
    public class NikonCamera: BaseINPC, ICamera {
        public NikonCamera(NikonDevice cam) {
            _camera = cam;
            _camera.ImageReady += Camera_ImageReady;
            _camera.CaptureComplete += _camera_CaptureComplete;


            //Set to shoot in RAW
            var compression = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel);
            for(int i = 0; i < compression.Length; i++) {
                var val = compression.GetEnumValueByIndex(i);
                if(val.ToString() == "RAW") {
                    compression.Index = i;
                    _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel,compression);
                    break;
                }
            }
        }

        private TaskCompletionSource<object> _downloadExposure;

        private void _camera_CaptureComplete(NikonDevice sender,int data) {
            _downloadExposure.TrySetResult(null);
        }

        private string _fileExtension;

        private void Camera_ImageReady(NikonDevice sender,NikonImage image) {
            //idea: write to temp file; call dcraw in downloadexposure and parse to imagearray
            _fileExtension = (image.Type == NikonImageType.Jpeg) ? ".jpg" : ".nef";
            string filename = DCRaw.TMPIMGFILEPATH + _fileExtension;

            using (System.IO.FileStream s = new System.IO.FileStream(filename,System.IO.FileMode.Create,System.IO.FileAccess.Write)) {
                s.Write(image.Buffer,0,image.Buffer.Length);
            }
        }

        private NikonDevice _camera;


        public string Id {
            get {
                return _camera.Id.ToString();
            }
        }


        public string Name {
            get {
                return _camera.Name;
            }
        }

        public string Description {
            get {
                return _camera.Name;
            }
        }

        public bool HasShutter {
            get {
                return true;
            }
        }

        public bool Connected {
            get {
                return _camera != null;
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

        public string DriverInfo {
            get {
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                throw new NotImplementedException();
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
                return 0;
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
            get {
                return false;
            }
        }

        public bool CoolerOn {
            get {
                return false;
            }

            set {
                
            }
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
                return _camera.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
            }
        }

        public bool CanSetGain {
            get {
                return _camera.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
            }
        }

        public short GainMax {
            get {
                if(Gains != null) {
                    return ISOSpeeds.Aggregate((l,r) => l.Value > r.Value ? l : r).Key;
                } else {
                    return 0;
                }
            }
        }

        public short GainMin {
            get {
                if (Gains != null) {
                    return ISOSpeeds.Aggregate((l,r) => l.Value < r.Value ? l : r).Key;
                }
                else {
                    return 0;
                }
            }
        }

        public short Gain {
            get {
                NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                return (short)e.Value;

            }
            set {
                var iso = ISOSpeeds.Where((x) => x.Key == value).FirstOrDefault().Value;
                NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                e.Index = iso;
                _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity,e);
                RaisePropertyChanged();
            }
        }

        private Dictionary<short,int> ISOSpeeds = new Dictionary<short,int>();

        private ArrayList _gains;
        public ArrayList Gains {
            get {
                if(_gains == null) {
                    _gains = new ArrayList();
                    if(CanGetGain) {
                        NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                        for(int i = 0; i < e.Length; i++) {
                            var iso = (int)e.GetEnumValueByIndex(i);
                            ISOSpeeds.Add((short)iso,i);
                            _gains.Add(i);
                        }                        
                    }
                }
                return _gains;
            }
        }

        private AsyncObservableCollection<BinningMode> _binningModes;
        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (_binningModes == null) {
                    _binningModes = new AsyncObservableCollection<BinningMode>();
                    _binningModes.Add(new BinningMode(1,1));
                }
                return _binningModes;
            }
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        

        public void AbortExposure() {
            _camera.StopBulbCapture();
        }

        public bool Connect() {
            return true;
        }

        public void Disconnect() {
            
        }

        public async Task<ImageArray> DownloadExposure(CancellationTokenSource tokenSource) {
            await _downloadExposure.Task;

            var iarr = await new DCRaw().ConvertToImageArray(_fileExtension,tokenSource.Token);
            return iarr;
        }

        public void SetBinning(short x,short y) {

        }

        public void SetupDialog() {

        }

        private Dictionary<int,int> _shutterSpeeds = new Dictionary<int,int>();
        
        
        public void StartExposure(double exposureTime,bool isLightFrame) {
            _downloadExposure = new TaskCompletionSource<object>();

            if(Settings.UseTelescopeSnapPort) {
                Mediator.Instance.Notify(MediatorMessages.TelescopeSnapPort,true);
                DateTime d = DateTime.Now;
                
                /*Stop Exposure after exposure time */
                Task.Run(() => {
                    exposureTime = exposureTime * 1000;
                    do {
                        Thread.Sleep(100);
                    } while ((DateTime.Now - d).TotalMilliseconds < exposureTime);

                    Mediator.Instance.Notify(MediatorMessages.TelescopeSnapPort,false);
                });

            } else {
                if (exposureTime < 1.0) {
                    var shutterspeed = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);

                    //Todo set to native shutter speeds


                }
                else {
                    DateTime d = DateTime.Now;
                    _camera.StartBulbCapture();
                    
                    /*Stop Exposure after exposure time */
                    Task.Run(() => {
                        exposureTime = exposureTime * 1000;
                        do {
                            Thread.Sleep(100);
                        } while ((DateTime.Now - d).TotalMilliseconds < exposureTime);

                        _camera.StopBulbCapture();
                    });
                }
            }
            
                      
        }

        public void StopExposure() {
            _camera.StopBulbCapture();
        }

        public void UpdateValues() {
        }
    }
}
