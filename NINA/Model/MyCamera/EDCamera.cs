using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using NINA.Utility;
using EDSDKLib;

namespace NINA.Model.MyCamera {
    class EDCamera : BaseINPC, ICamera {


        public EDCamera(IntPtr cam, EDSDK.EdsDeviceInfo info) {
            _cam = cam;
            Id = info.szDeviceDescription;
            Name = info.szDeviceDescription;
        }

        private IntPtr _cam;

        public bool HasShutter => throw new NotImplementedException();

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

        public double CCDTemperature => throw new NotImplementedException();

        public double SetCCDTemperature { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public short BinX { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public short BinY { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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

        public string Description => throw new NotImplementedException();

        public string DriverInfo => throw new NotImplementedException();

        public string DriverVersion => throw new NotImplementedException();

        public string SensorName => throw new NotImplementedException();

        public SensorType SensorType => throw new NotImplementedException();

        public int CameraXSize => throw new NotImplementedException();

        public int CameraYSize => throw new NotImplementedException();

        public double ExposureMin => throw new NotImplementedException();

        public double ExposureMax => throw new NotImplementedException();

        public short MaxBinX => throw new NotImplementedException();

        public short MaxBinY => throw new NotImplementedException();

        public double PixelSizeX => throw new NotImplementedException();

        public double PixelSizeY => throw new NotImplementedException();

        public bool CanSetCCDTemperature => throw new NotImplementedException();

        public bool CoolerOn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public double CoolerPower => throw new NotImplementedException();

        public string CameraState => throw new NotImplementedException();

        public bool CanSetOffset => throw new NotImplementedException();

        public bool CanSetUSBLimit => throw new NotImplementedException();

        public bool CanGetGain => throw new NotImplementedException();

        public bool CanSetGain => throw new NotImplementedException();

        public short GainMax => throw new NotImplementedException();

        public short GainMin => throw new NotImplementedException();

        public short Gain { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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

        public bool HasSetupDialog => throw new NotImplementedException();

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

        public void AbortExposure() {
            throw new NotImplementedException();
        }

        public bool Connect() {

            uint err = EDSDK.EdsOpenSession(_cam);
            if (err != EDSDK.EDS_ERR_OK) {
                return false;
            } else {
                Connected = true;
                return true;
            }
        }

        public void Disconnect() {
            uint err = EDSDK.EdsCloseSession(_cam);
        }

        public Task<ImageArray> DownloadExposure(CancellationTokenSource tokenSource) {
            throw new NotImplementedException();
        }

        public void SetBinning(short x, short y) {
            throw new NotImplementedException();
        }

        public void SetupDialog() {
            throw new NotImplementedException();
        }

        public void StartExposure(double exposureTime, bool isLightFrame) {
            throw new NotImplementedException();
        }

        public void StopExposure() {
            throw new NotImplementedException();
        }

        public void UpdateValues() {
            //throw new NotImplementedException();
        }
    }
}
