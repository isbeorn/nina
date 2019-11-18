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
using System.IO;
using System.Runtime.Serialization;

namespace NINA.Profile {
    [Serializable()]
    [DataContract]
    public class SequenceSettings : Settings, ISequenceSettings {
        public SequenceSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            parkMountAtSequenceEnd = false;
            warmCamAtSequenceEnd = false;
            templatePath = string.Empty;
            estimatedDownloadTime = TimeSpan.FromSeconds(0);
            DefaultSequenceFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A.");
        }

        private string templatePath;

        [DataMember]
        public string TemplatePath {
            get {
                return templatePath;
            }
            set {
                if (templatePath != value) {
                    templatePath = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool parkMountAtSequenceEnd;

        [DataMember]
        public bool ParkMountAtSequenceEnd {
            get {
                return parkMountAtSequenceEnd;
            }
            set {
                if (parkMountAtSequenceEnd != value) {
                    parkMountAtSequenceEnd = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool warmCamAtSequenceEnd;

        [DataMember]
        public bool WarmCamAtSequenceEnd {
            get {
                return warmCamAtSequenceEnd;
            }
            set {
                if (warmCamAtSequenceEnd != value) {
                    warmCamAtSequenceEnd = value;
                    RaisePropertyChanged();
                }
            }
        }

        private TimeSpan estimatedDownloadTime;

        public TimeSpan EstimatedDownloadTime {
            get {
                return estimatedDownloadTime;
            }
            set {
                if (estimatedDownloadTime != value) {
                    estimatedDownloadTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public long TimeSpanInTicks {
            get {
                return estimatedDownloadTime.Ticks;
            }
            set {
                estimatedDownloadTime = new TimeSpan(value);
            }
        }

        private string sequenceFolder;

        [DataMember]
        public string DefaultSequenceFolder
        {
            get
            {
                return sequenceFolder;
            }
            set
            {
                if (sequenceFolder != value)
                {
                    sequenceFolder = value;
                    RaisePropertyChanged();
                }
            }
        }

    }
}
