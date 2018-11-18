using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class TelescopeSettings : Settings, ITelescopeSettings {
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

        private int focalLength = 800;

        [DataMember]
        public int FocalLength {
            get {
                return focalLength;
            }
            set {
                focalLength = value;
                RaisePropertyChanged();
            }
        }

        private string snapPortStart = ":SNAP1,1#";

        [DataMember]
        public string SnapPortStart {
            get {
                return snapPortStart;
            }
            set {
                snapPortStart = value;
                RaisePropertyChanged();
            }
        }

        private string snapPortStop = "SNAP1,0#";

        [DataMember]
        public string SnapPortStop {
            get {
                return snapPortStop;
            }
            set {
                snapPortStop = value;
                RaisePropertyChanged();
            }
        }

        private int settleTime = 5;

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
    }
}