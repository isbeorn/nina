#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class MeridianFlipSettings : Settings, IMeridianFlipSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            recenter = true;
            minutesAfterMeridian = 5;
            MaxMinutesAfterMeridian = 10;
            useSideOfPier = true;
            settleTime = 30;
            pauseTimeBeforeMeridian = 0;
            autoFocusAfterFlip = false;
            rotateImageAfterFlip = false;
        }

        private bool recenter;

        [DataMember]
        public bool Recenter {
            get => recenter;
            set {
                if (recenter != value) {
                    recenter = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double minutesAfterMeridian;

        [DataMember]
        public double MinutesAfterMeridian {
            get => minutesAfterMeridian;
            set {
                if (minutesAfterMeridian != value) {
                    minutesAfterMeridian = value;
                    if (MaxMinutesAfterMeridian < minutesAfterMeridian) {
                        MaxMinutesAfterMeridian = minutesAfterMeridian;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private double maxMinutesAfterMeridian;

        [DataMember]
        public double MaxMinutesAfterMeridian {
            get => maxMinutesAfterMeridian;
            set {
                if (maxMinutesAfterMeridian != value) {
                    maxMinutesAfterMeridian = value;
                    if (maxMinutesAfterMeridian < MinutesAfterMeridian) {
                        MinutesAfterMeridian = value;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Adding this user-side parameter for backwards compatibility: some mounts always report PierSide as East or mix up West and East.
        /// If method were to consider SideOfPier by default and flip only if not East, such mounts would stop performing flip.
        /// Default value is False, to preserve prior behavior.
        /// </summary>

        private bool useSideOfPier;

        [DataMember]
        public bool UseSideOfPier {
            get => useSideOfPier;
            set {
                if (useSideOfPier != value) {
                    useSideOfPier = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int settleTime;

        [DataMember]
        public int SettleTime {
            get => settleTime;
            set {
                if (settleTime != value) {
                    settleTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double pauseTimeBeforeMeridian;

        [DataMember]
        public double PauseTimeBeforeMeridian {
            get => pauseTimeBeforeMeridian;
            set {
                if (pauseTimeBeforeMeridian != value) {
                    pauseTimeBeforeMeridian = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool autoFocusAfterFlip;

        [DataMember]
        public bool AutoFocusAfterFlip {
            get => autoFocusAfterFlip;
            set {
                if (autoFocusAfterFlip != value) {
                    autoFocusAfterFlip = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool rotateImageAfterFlip;

        [DataMember]
        public bool RotateImageAfterFlip {
            get => rotateImageAfterFlip;
            set {
                if (rotateImageAfterFlip != value) {
                    rotateImageAfterFlip = value;
                    RaisePropertyChanged();
                }
            }
        }


    }
}