#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
        public string DefaultSequenceFolder {
            get {
                return sequenceFolder;
            }
            set {
                if (sequenceFolder != value) {
                    sequenceFolder = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string sequenceCompleteCommand;

        [DataMember]
        public string SequenceCompleteCommand {
            get => sequenceCompleteCommand;
            set {
                if (sequenceCompleteCommand != value) {
                    sequenceCompleteCommand = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}