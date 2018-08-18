using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class CameraSettings : Settings, ICameraSettings {
        private string id = "No_Device";

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private double pixelSize = 3.8;

        [DataMember]
        public double PixelSize {
            get {
                return pixelSize;
            }
            set {
                pixelSize = value;
                RaisePropertyChanged();
            }
        }

        private CameraBulbModeEnum bulbMode = CameraBulbModeEnum.NATIVE;

        [DataMember]
        public CameraBulbModeEnum BulbMode {
            get {
                return bulbMode;
            }
            set {
                bulbMode = value;
                RaisePropertyChanged();
            }
        }

        private string serialPort = "COM1";

        [DataMember]
        public string SerialPort {
            get {
                return serialPort;
            }
            set {
                serialPort = value;
                RaisePropertyChanged();
            }
        }

        private double _readNoise = 0.0;

        [DataMember]
        public double ReadNoise {
            get {
                return _readNoise;
            }
            set {
                _readNoise = value;
                RaisePropertyChanged();
            }
        }

        private double _bitDepth = 16;

        [DataMember]
        public double BitDepth {
            get {
                return _bitDepth;
            }
            set {
                _bitDepth = value;
                RaisePropertyChanged();
            }
        }

        private double _offset = 0;

        [DataMember]
        public double Offset {
            get {
                return _offset;
            }
            set {
                _offset = value;
                RaisePropertyChanged();
            }
        }

        private double _fullWellCapacity = 20000;

        [DataMember]
        public double FullWellCapacity {
            get {
                return _fullWellCapacity;
            }
            set {
                _fullWellCapacity = value;
                RaisePropertyChanged();
            }
        }

        private double _downloadToDataRatio = 9;

        [DataMember]
        public double DownloadToDataRatio {
            get {
                return _downloadToDataRatio;
            }
            set {
                _downloadToDataRatio = value;
                RaisePropertyChanged();
            }
        }

        private RawConverterEnum _rawConverter = RawConverterEnum.DCRAW;

        [DataMember]
        public RawConverterEnum RawConverter {
            get {
                return _rawConverter;
            }
            set {
                _rawConverter = value;
                RaisePropertyChanged();
            }
        }
    }
}