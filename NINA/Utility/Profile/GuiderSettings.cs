#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility.Enum;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class GuiderSettings : Settings, IGuiderSettings {

        public GuiderSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDesiralization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            ditherPixels = 5;
            ditherRAOnly = false;
            settleTime = 10;
            pHD2ServerUrl = "localhost";
            pHD2ServerPort = 4400;
            pHD2LargeHistorySize = 100;
            pHD2GuiderScale = GuiderScaleEnum.PIXELS;
            settlePixels = 1.5;
            settleTimeout = 40;
            phd2Path = File.Exists(Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\PHDGuiding2\phd2.exe")) ? Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\PHDGuiding2\phd2.exe") : "";
        }

        private double ditherPixels;

        [DataMember]
        public double DitherPixels {
            get {
                return ditherPixels;
            }
            set {
                ditherPixels = value;
                RaisePropertyChanged();
            }
        }

        private bool ditherRAOnly;

        [DataMember]
        public bool DitherRAOnly {
            get {
                return ditherRAOnly;
            }
            set {
                ditherRAOnly = value;
                RaisePropertyChanged();
            }
        }

        private int settleTime;

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

        private string pHD2ServerUrl;

        [DataMember]
        public string PHD2ServerUrl {
            get {
                return pHD2ServerUrl;
            }
            set {
                pHD2ServerUrl = value;
                RaisePropertyChanged();
            }
        }

        private int pHD2ServerPort;

        [DataMember]
        public int PHD2ServerPort {
            get {
                return pHD2ServerPort;
            }
            set {
                pHD2ServerPort = value;
                RaisePropertyChanged();
            }
        }

        private int pHD2LargeHistorySize;

        [DataMember]
        public int PHD2HistorySize {
            get {
                return pHD2LargeHistorySize;
            }
            set {
                pHD2LargeHistorySize = value;
                RaisePropertyChanged();
            }
        }

        private string phd2Path;

        [DataMember]
        public string PHD2Path {
            get => phd2Path;
            set {
                phd2Path = value;
                RaisePropertyChanged();
            }
        }

        private GuiderScaleEnum pHD2GuiderScale;

        [DataMember]
        public GuiderScaleEnum PHD2GuiderScale {
            get {
                return pHD2GuiderScale;
            }
            set {
                pHD2GuiderScale = value;
                RaisePropertyChanged();
            }
        }

        private double settlePixels;

        [DataMember]
        public double SettlePixels {
            get {
                return settlePixels;
            }

            set {
                settlePixels = value;
                RaisePropertyChanged();
            }
        }

        private int settleTimeout;

        [DataMember]
        public int SettleTimeout {
            get {
                return settleTimeout;
            }

            set {
                settleTimeout = value;
                RaisePropertyChanged();
            }
        }
    }
}