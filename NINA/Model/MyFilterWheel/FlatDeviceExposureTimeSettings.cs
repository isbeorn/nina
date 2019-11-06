using NINA.Model.MyCamera;
using NINA.Utility;
using System;
using System.Runtime.Serialization;

namespace NINA.Model.MyFilterWheel {

    [Serializable]
    [DataContract]
    public class FlatDeviceExposureTimeSettings : BaseINPC {
        private BinningMode _binningMode;
        private double _exposureTime;
        private int _flatDeviceBrightness;
        private double _gain;

        [DataMember]
        public BinningMode BinningMode {
            get => _binningMode;
            set {
                _binningMode = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public double ExposureTime {
            get => _exposureTime;
            set {
                _exposureTime = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public int FlatDeviceBrightness {
            get => _flatDeviceBrightness;
            set {
                _flatDeviceBrightness = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public double Gain {
            get => _gain;
            set {
                _gain = value;
                RaisePropertyChanged();
            }
        }
    }
}