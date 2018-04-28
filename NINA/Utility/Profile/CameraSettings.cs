using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(CameraSettings))]
    public class CameraSettings {

        private string id = "No_Device";
        [XmlElement(nameof(Id))]
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
        [XmlElement(nameof(PixelSize))]
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
        [XmlElement(nameof(BulbMode))]
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
        [XmlElement(nameof(SerialPort))]
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
        [XmlElement(nameof(ReadNoise))]
        public double ReadNoise {
            get
            {
                return _readNoise;
            }
            set
            {
                _readNoise = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double _bitDepth = 16;
        [XmlElement(nameof(BitDepth))]
        public double BitDepth
        {
            get
            {
                return _bitDepth;
            }
            set
            {
                _bitDepth = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private double _offset = 0;
        [XmlElement(nameof(Offset))]
        public double Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }


        private double _fullWellCapacity = 20000;
        [XmlElement(nameof(FullWellCapacity))]
        public double FullWellCapacity
        {
            get
            {
                return _fullWellCapacity;
            }
            set
            {
                _fullWellCapacity = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}
