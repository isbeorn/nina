#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class MeridianFlipSettings : Settings, IMeridianFlipSettings {
        private bool enabled = false;

        [DataMember]
        public bool Enabled {
            get {
                return enabled;
            }
            set {
                enabled = value;
                RaisePropertyChanged();
            }
        }

        private bool recenter = true;

        [DataMember]
        public bool Recenter {
            get {
                return recenter;
            }
            set {
                recenter = value;
                RaisePropertyChanged();
            }
        }

        private double minutesAfterMeridian = 1;

        [DataMember]
        public double MinutesAfterMeridian {
            get {
                return minutesAfterMeridian;
            }
            set {
                minutesAfterMeridian = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Adding this user-side parameter for backwards compatibility: some mounts always report PierSide as East or mix up West and East.
        /// If method were to consider SideOfPier by default and flip only if not East, such mounts would stop performing flip.
        /// Default value is False, to preserve prior behavior.
        /// </summary>
        
        private bool useSideOfPier = false;

        [DataMember]
        public bool UseSideOfPier {
            get {
                return useSideOfPier;
            }
            set {
                useSideOfPier = value;
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

        private double pauseTimeBeforeMeridian = 1;

        [DataMember]
        public double PauseTimeBeforeMeridian {
            get {
                return pauseTimeBeforeMeridian;
            }
            set {
                pauseTimeBeforeMeridian = value;
                RaisePropertyChanged();
            }
        }
    }
}