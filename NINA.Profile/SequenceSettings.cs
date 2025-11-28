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

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (!Directory.Exists(DefaultSequenceFolder)) {
                DefaultSequenceFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A");
            }
            if (!Directory.Exists(SequencerTemplatesFolder)) {
                SequencerTemplatesFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A", "Templates");
            }
            if (!Directory.Exists(SequencerTargetsFolder)) {
                SequencerTargetsFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A", "Targets");
            }
            if (!File.Exists(TemplatePath)) {
                TemplatePath = string.Empty;
            }
            if (!File.Exists(StartupSequenceTemplate)) {
                StartupSequenceTemplate = string.Empty;
            }
        }

        protected override void SetDefaultValues() {
            doMeridianFlip = false;
            parkMountAtSequenceEnd = false;
            warmCamAtSequenceEnd = false;
            templatePath = string.Empty;
            estimatedDownloadTime = TimeSpan.FromSeconds(0);
            DefaultSequenceFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A");
            closeDomeShutterAtSequenceEnd = true;
            parkDomeAtSequenceEnd = true;
            templateFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A", "Templates");
            targetsFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "N.I.N.A", "Targets");
            startupSequenceTemplate = string.Empty;
            unparMountAtSequenceStart = true;
            collapseSequencerTemplatesByDefault = false;
            disableSimpleSequencer = false;
        }

        private string templatePath;

        [DataMember]
        // Used by legacy sequencer
        public string TemplatePath {
            get => templatePath;
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
            get => parkMountAtSequenceEnd;
            set {
                if (parkMountAtSequenceEnd != value) {
                    parkMountAtSequenceEnd = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool closeDomeShutterAtSequenceEnd;

        [DataMember]
        public bool CloseDomeShutterAtSequenceEnd {
            get => closeDomeShutterAtSequenceEnd;
            set {
                if (closeDomeShutterAtSequenceEnd != value) {
                    closeDomeShutterAtSequenceEnd = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool parkDomeAtSequenceEnd;

        [DataMember]
        public bool ParkDomeAtSequenceEnd {
            get => parkDomeAtSequenceEnd;
            set {
                if (parkDomeAtSequenceEnd != value) {
                    parkDomeAtSequenceEnd = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool warmCamAtSequenceEnd;

        [DataMember]
        public bool WarmCamAtSequenceEnd {
            get => warmCamAtSequenceEnd;
            set {
                if (warmCamAtSequenceEnd != value) {
                    warmCamAtSequenceEnd = value;
                    RaisePropertyChanged();
                }
            }
        }

        private TimeSpan estimatedDownloadTime;

        public TimeSpan EstimatedDownloadTime {
            get => estimatedDownloadTime;
            set {
                if (estimatedDownloadTime != value) {
                    estimatedDownloadTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public long TimeSpanInTicks {
            get => estimatedDownloadTime.Ticks;
            set => estimatedDownloadTime = new TimeSpan(value);
        }

        private string sequenceFolder;

        [DataMember]
        public string DefaultSequenceFolder {
            get => sequenceFolder;
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

        private string templateFolder;

        [DataMember]
        public string SequencerTemplatesFolder {
            get => templateFolder;
            set {
                if (templateFolder != value) {
                    templateFolder = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string targetsFolder;

        [DataMember]
        public string SequencerTargetsFolder {
            get => targetsFolder;
            set {
                if (targetsFolder != value) {
                    targetsFolder = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string startupSequenceTemplate;

        [DataMember]
        public string StartupSequenceTemplate {
            get => startupSequenceTemplate;
            set {
                if (startupSequenceTemplate != value) {
                    startupSequenceTemplate = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool coolCameraAtSequenceStart;

        [DataMember]
        public bool CoolCameraAtSequenceStart {
            get => coolCameraAtSequenceStart;
            set {
                if (coolCameraAtSequenceStart != value) {
                    coolCameraAtSequenceStart = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool unparMountAtSequenceStart;

        [DataMember]
        public bool UnparMountAtSequenceStart {
            get => unparMountAtSequenceStart;
            set {
                if (unparMountAtSequenceStart != value) {
                    unparMountAtSequenceStart = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool openDomeShutterAtSequenceStart;

        [DataMember]
        public bool OpenDomeShutterAtSequenceStart {
            get => openDomeShutterAtSequenceStart;
            set {
                if (openDomeShutterAtSequenceStart != value) {
                    openDomeShutterAtSequenceStart = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool collapseSequencerTemplatesByDefault;

        [DataMember]
        public bool CollapseSequencerTemplatesByDefault {
            get => collapseSequencerTemplatesByDefault;
            set {
                if (collapseSequencerTemplatesByDefault != value) {
                    collapseSequencerTemplatesByDefault = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool doMeridianFlip;

        [DataMember]
        public bool DoMeridianFlip {
            get => doMeridianFlip;
            set {
                if (doMeridianFlip != value) {
                    doMeridianFlip = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool disableSimpleSequencer;

        [DataMember]
        public bool DisableSimpleSequencer {
            get => disableSimpleSequencer;
            set {
                if (disableSimpleSequencer != value) {
                    disableSimpleSequencer = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool excludeExposureCountFromHasChanges;

        [DataMember]
        public bool ExcludeExposureCountFromHasChanges { 
            get => excludeExposureCountFromHasChanges; 
            set {
                if (excludeExposureCountFromHasChanges != value) {
                    excludeExposureCountFromHasChanges = value;
                    RaisePropertyChanged();
                }
            } 
        }
    }
}