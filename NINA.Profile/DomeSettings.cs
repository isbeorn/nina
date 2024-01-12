#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class DomeSettings : Settings, IDomeSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
            ScopePositionEastWest_mm = 0.0;
            ScopePositionNorthSouth_mm = 0.0;
            ScopePositionUpDown_mm = 0.0;
            DomeRadius_mm = 0.0;
            GemAxis_mm = 0.0;
            AzimuthTolerance_degrees = 2.0;
            FindHomeBeforePark = false;
            DomeSyncTimeoutSeconds = 120;
            SettleTimeSeconds = 1;
            SyncSlewDomeWhenMountSlews = false;
            SynchronizeDuringMountSlew = false;
        }

        private string id = string.Empty;

        [DataMember]
        public string Id {
            get => id;
            set {
                if (id != value) {
                    id = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double scopePositionEastWest_mm = 0.0;

        [DataMember]
        public double ScopePositionEastWest_mm {
            get => scopePositionEastWest_mm;
            set {
                if (scopePositionEastWest_mm != value) {
                    scopePositionEastWest_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double scopePositionNorthSouth_mm = 0.0;

        [DataMember]
        public double ScopePositionNorthSouth_mm {
            get => scopePositionNorthSouth_mm;
            set {
                if (scopePositionNorthSouth_mm != value) {
                    scopePositionNorthSouth_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double scopePositionUpDown_mm = 0.0;

        [DataMember]
        public double ScopePositionUpDown_mm {
            get => scopePositionUpDown_mm;
            set {
                if (scopePositionUpDown_mm != value) {
                    scopePositionUpDown_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double domeRadius_mm = 0.0;

        [DataMember]
        public double DomeRadius_mm {
            get => domeRadius_mm;
            set {
                if (domeRadius_mm != value) {
                    domeRadius_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double gemAxis_mm = 0.0;

        [DataMember]
        public double GemAxis_mm {
            get => gemAxis_mm;
            set {
                if (gemAxis_mm != value) {
                    gemAxis_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double lateralAxis_mm = 0.0;

        [DataMember]
        public double LateralAxis_mm {
            get => lateralAxis_mm;
            set {
                if (lateralAxis_mm != value) {
                    lateralAxis_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double azimuthTolerance_degrees = 2.0;

        [DataMember]
        public double AzimuthTolerance_degrees {
            get => azimuthTolerance_degrees;
            set {
                if (azimuthTolerance_degrees != value) {
                    azimuthTolerance_degrees = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool findHomeBeforePark = false;

        [DataMember]
        public bool FindHomeBeforePark {
            get => findHomeBeforePark;
            set {
                if (findHomeBeforePark != value) {
                    findHomeBeforePark = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int domeSyncTimeoutSeconds = 120;

        [DataMember]
        public int DomeSyncTimeoutSeconds {
            get => domeSyncTimeoutSeconds;
            set {
                if (domeSyncTimeoutSeconds != value) {
                    domeSyncTimeoutSeconds = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool synchronizeDuringMountSlew = false;

        [DataMember]
        public bool SynchronizeDuringMountSlew {
            get => synchronizeDuringMountSlew;
            set {
                if (synchronizeDuringMountSlew != value) {
                    synchronizeDuringMountSlew = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool syncSlewDomeWhenMountSlews = false;

        [DataMember]
        public bool SyncSlewDomeWhenMountSlews {
            get => syncSlewDomeWhenMountSlews;
            set {
                if (syncSlewDomeWhenMountSlews != value) {
                    syncSlewDomeWhenMountSlews = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double manualSlewDegrees = 10.0;

        [DataMember]
        public double RotateDegrees {
            get => manualSlewDegrees;
            set {
                if (manualSlewDegrees != value) {
                    manualSlewDegrees = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool closeOnUnsafe = false;

        [DataMember]
        public bool CloseOnUnsafe {
            get => closeOnUnsafe;
            set {
                if (closeOnUnsafe != value) {
                    closeOnUnsafe = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool parkMountBeforeShutterMove = false;

        [DataMember]
        public bool ParkMountBeforeShutterMove {
            get => parkMountBeforeShutterMove;
            set {
                if (parkMountBeforeShutterMove != value) {
                    parkMountBeforeShutterMove = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool refuseUnsafeShutterMove = false;

        [DataMember]
        public bool RefuseUnsafeShutterMove {
            get => refuseUnsafeShutterMove;
            set {
                if (refuseUnsafeShutterMove != value) {
                    refuseUnsafeShutterMove = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool refuseUnsafeShutterOpenSansSafetyDevice = false;

        [DataMember]
        public bool RefuseUnsafeShutterOpenSansSafetyDevice {
            get => refuseUnsafeShutterOpenSansSafetyDevice;
            set {
                if (refuseUnsafeShutterOpenSansSafetyDevice != value) {
                    refuseUnsafeShutterOpenSansSafetyDevice = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool parkDomeBeforeShutterMove = false;

        [DataMember]
        public bool ParkDomeBeforeShutterMove {
            get => parkDomeBeforeShutterMove;
            set {
                if (parkDomeBeforeShutterMove != value) {
                    parkDomeBeforeShutterMove = value;
                    RaisePropertyChanged();
                }
            }
        }

        private MountTypeEnum mountType = MountTypeEnum.EQUATORIAL;

        [DataMember]
        public MountTypeEnum MountType {
            get => mountType;
            set {
                if (mountType != value) {
                    mountType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double decOffsetHorizontal_mm = 0.0;

        [DataMember]
        public double DecOffsetHorizontal_mm {
            get => decOffsetHorizontal_mm;
            set {
                if (decOffsetHorizontal_mm != value) {
                    decOffsetHorizontal_mm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int settleTimeSeconds = 0;

        [DataMember]
        public int SettleTimeSeconds {
            get => settleTimeSeconds;
            set {
                if (settleTimeSeconds != value) {
                    settleTimeSeconds = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}