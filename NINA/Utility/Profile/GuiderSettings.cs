using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class GuiderSettings : Settings, IGuiderSettings {
        private double ditherPixels = 5;

        [DataMember]
        public double DitherPixels {
            get {
                return ditherPixels;
            }
            set {
                ditherPixels = value;
                RaisePropertyChanged();
            }
        }

        private bool ditherRAOnly = false;

        [DataMember]
        public bool DitherRAOnly {
            get {
                return ditherRAOnly;
            }
            set {
                ditherRAOnly = value;
                RaisePropertyChanged();
            }
        }

        private int settleTime = 10;

        [DataMember]
        public int SettleTime {
            get {
                return settleTime;
            }
            set {
                settleTime = value;
                RaisePropertyChanged();
            }
        }

        private string pHD2ServerUrl = "localhost";

        [DataMember]
        public string PHD2ServerUrl {
            get {
                return pHD2ServerUrl;
            }
            set {
                pHD2ServerUrl = value;
                RaisePropertyChanged();
            }
        }

        private int pHD2ServerPort = 4400;

        [DataMember]
        public int PHD2ServerPort {
            get {
                return pHD2ServerPort;
            }
            set {
                pHD2ServerPort = value;
                RaisePropertyChanged();
            }
        }

        private int pHD2LargeHistorySize = 100;

        [DataMember]
        public int PHD2HistorySize {
            get {
                return pHD2LargeHistorySize;
            }
            set {
                pHD2LargeHistorySize = value;
                RaisePropertyChanged();
            }
        }

        private GuiderScaleEnum pHD2GuiderScale = GuiderScaleEnum.PIXELS;

        [DataMember]
        public GuiderScaleEnum PHD2GuiderScale {
            get {
                return pHD2GuiderScale;
            }
            set {
                pHD2GuiderScale = value;
                RaisePropertyChanged();
            }
        }
    }
}