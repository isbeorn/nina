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
            }
        }
    }
}
