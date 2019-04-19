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
    public class FocuserSettings : Settings, IFocuserSettings {
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

        private bool useFilterWheelOffsets = false;

        [DataMember]
        public bool UseFilterWheelOffsets {
            get {
                return useFilterWheelOffsets;
            }
            set {
                useFilterWheelOffsets = value;
                RaisePropertyChanged();
            }
        }

        private int autoFocusStepSize = 10;

        [DataMember]
        public int AutoFocusStepSize {
            get {
                return autoFocusStepSize;
            }
            set {
                autoFocusStepSize = value;
                RaisePropertyChanged();
            }
        }

        private int autoFocusInitialOffsetSteps = 4;

        [DataMember]
        public int AutoFocusInitialOffsetSteps {
            get {
                return autoFocusInitialOffsetSteps;
            }
            set {
                autoFocusInitialOffsetSteps = value;
                RaisePropertyChanged();
            }
        }

        private int autoFocusExposureTime = 6;

        [DataMember]
        public int AutoFocusExposureTime {
            get {
                return autoFocusExposureTime;
            }
            set {
                autoFocusExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private int focuserSettleTime = 0;

        [DataMember]
        public int FocuserSettleTime {
            get {
                return focuserSettleTime;
            }
            set {
                focuserSettleTime = value;
                RaisePropertyChanged();
            }
        }

        private int autoFocusTotalNumberOfAttempts = 1;

        [DataMember]
        public int AutoFocusTotalNumberOfAttempts {
            get {
                if (autoFocusTotalNumberOfAttempts < 1) { return 1; }
                return autoFocusTotalNumberOfAttempts;
            }
            set {
                autoFocusTotalNumberOfAttempts = value;
                RaisePropertyChanged();
            }
        }

        private int autoFocusNumberOfFramesPerPoint = 1;

        [DataMember]
        public int AutoFocusNumberOfFramesPerPoint {
            get {
                if (autoFocusNumberOfFramesPerPoint < 1) { return 1; }
                return autoFocusNumberOfFramesPerPoint;
            }
            set {
                autoFocusNumberOfFramesPerPoint = value;
                RaisePropertyChanged();
            }
        }
    }
}