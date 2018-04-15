using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(ApplicationSettings))]
    class GuiderSettings {

        private double ditherPixels = 5;
        [XmlElement(nameof(DitherPixels))]
        public double DitherPixels {
            get {
                return ditherPixels;
            }
            set {
                ditherPixels = value;
            }            
        }

        private bool ditherRAOnly = false;
        [XmlElement(nameof(DitherRAOnly))]
        public bool DitherRAOnly {
            get {
                return ditherRAOnly;
            }
            set {
                ditherRAOnly = value;
            }
        }

        private int settleTime = 10;
        [XmlElement(nameof(SettleTime))]
        public int SettleTime {
            get {
                return settleTime;
            }
            set {
                settleTime = value;
            }
        }

        private string pHD2ServerUrl = "localhost";
        [XmlElement(nameof(PHD2ServerUrl))]
        public string PHD2ServerUrl {
            get {
                return pHD2ServerUrl;
            }
            set {
                pHD2ServerUrl = value;
            }
        }

        private string pHD2ServerPort = "4400";
        [XmlElement(nameof(PHD2ServerPort))]
        public string PHD2ServerPort {
            get {
                return pHD2ServerPort;
            }
            set {
                pHD2ServerPort = value;
            }
        }
    }
}
