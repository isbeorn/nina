using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class TelescopeSettings : ITelescopeSettings {
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

        private int focalLength = 800;

        [DataMember]
        public int FocalLength {
            get {
                return focalLength;
            }
            set {
                focalLength = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
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
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}