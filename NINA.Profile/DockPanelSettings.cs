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
    public class DockPanelSettings : Settings, IDockPanelSettings  {
        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            showImagingHistogram = true;
            cameraInfoOnly = false;
            filterWheelInfoOnly = false;
            focuserInfoOnly = false;
            rotatorInfoOnly = false;
            switchInfoOnly = false;
            flatDeviceInfoOnly = false;
            isLocked = false;
        }
        private bool showImagingHistogram;

        [DataMember]
        public bool ShowImagingHistogram {
            get => showImagingHistogram;
            set {
                if (showImagingHistogram != value) {
                    showImagingHistogram = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool cameraInfoOnly;

        [DataMember]
        public bool CameraInfoOnly {
            get => cameraInfoOnly;
            set {
                if (cameraInfoOnly != value) {
                    cameraInfoOnly = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool filterWheelInfoOnly;

        [DataMember]
        public bool FilterWheelInfoOnly {
            get => filterWheelInfoOnly;
            set {
                if (filterWheelInfoOnly != value) {
                    filterWheelInfoOnly = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool focuserInfoOnly;

        [DataMember]
        public bool FocuserInfoOnly {
            get => focuserInfoOnly;
            set {
                if (focuserInfoOnly != value) {
                    focuserInfoOnly = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool rotatorInfoOnly;

        [DataMember]
        public bool RotatorInfoOnly {
            get => rotatorInfoOnly;
            set {
                if (rotatorInfoOnly != value) {
                    rotatorInfoOnly = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool switchInfoOnly;

        [DataMember]
        public bool SwitchInfoOnly {
            get => switchInfoOnly;
            set {
                if (switchInfoOnly != value) {
                    switchInfoOnly = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool flatDeviceInfoOnly;

        [DataMember]
        public bool FlatDeviceInfoOnly {
            get => flatDeviceInfoOnly;
            set {
                if (flatDeviceInfoOnly != value) {
                    flatDeviceInfoOnly = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isLocked;

        [DataMember]
        public bool IsLocked {
            get => isLocked;
            set {
                if (isLocked != value) {
                    isLocked = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
