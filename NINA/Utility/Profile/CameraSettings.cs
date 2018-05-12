using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class CameraSettings : ICameraSettings {
        private string id = "No_Device";

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}