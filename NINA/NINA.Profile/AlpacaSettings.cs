using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Profile {
    [Serializable()]
    [DataContract]
    public class AlpacaSettings : Settings, IAlpacaSettings {
        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            numberOfPolls = 1;
            pollInterval = 100; 
            discoveryPort = 32227;
            discoveryDuration = 2d;
            resolveDnsName = false;
            useIPv4 = true;
            useIPv6 = false;
            useHttps = false;
        }

        private int numberOfPolls;

        [DataMember]
        public int NumberOfPolls {
            get => numberOfPolls;
            set {
                if (numberOfPolls != value) {
                    numberOfPolls = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int pollInterval;

        [DataMember]
        public int PollInterval {
            get => pollInterval;
            set {
                if (pollInterval != value) {
                    pollInterval = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int discoveryPort;

        [DataMember]
        public int DiscoveryPort {
            get => discoveryPort;
            set {
                if (discoveryPort != value) {
                    discoveryPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double discoveryDuration;

        [DataMember]
        public double DiscoveryDuration {
            get => discoveryDuration;
            set {
                if (discoveryDuration != value) {
                    discoveryDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool resolveDnsName;

        [DataMember]
        public bool ResolveDnsName {
            get => resolveDnsName;
            set {
                if (resolveDnsName != value) {
                    resolveDnsName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool useIPv4;

        [DataMember]
        public bool UseIPv4 {
            get => useIPv4;
            set {
                if (useIPv4 != value) {
                    useIPv4 = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool useIPv6;

        [DataMember]
        public bool UseIPv6 {
            get => useIPv6;
            set {
                if (useIPv6 != value) {
                    useIPv6 = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool useHttps;

        [DataMember]
        public bool UseHttps {
            get => useHttps;
            set {
                if (useHttps != value) {
                    useHttps = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
