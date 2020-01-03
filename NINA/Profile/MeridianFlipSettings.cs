#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

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
            enabled = false;
            recenter = true;
            minutesAfterMeridian = 1;
            useSideOfPier = false;
            settleTime = 5;
            pauseTimeBeforeMeridian = 1;
        }

        private bool enabled;

        [DataMember]
        public bool Enabled {
            get {
                return enabled;
            }
            set {
                if (enabled != value) {
                    enabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool recenter;

        [DataMember]
        public bool Recenter {
            get {
                return recenter;
            }
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
            get {
                return minutesAfterMeridian;
            }
            set {
                if (minutesAfterMeridian != value) {
                    minutesAfterMeridian = value;
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
            get {
                return useSideOfPier;
            }
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
            get {
                return settleTime;
            }
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
            get {
                return pauseTimeBeforeMeridian;
            }
            set {
                if (pauseTimeBeforeMeridian != value) {
                    pauseTimeBeforeMeridian = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}