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
    public class FocuserSettings : Settings, IFocuserSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            id = "No_Device";
            lastDeviceName = string.Empty;
            useFilterWheelOffsets = false;
            autoFocusStepSize = 50;
            autoFocusInitialOffsetSteps = 4;
            autoFocusExposureTime = 4;
            autoFocusDisableGuiding = false;
            focuserSettleTime = 0;
            autoFocusMethod = AFMethodEnum.STARHFR;
            autoFocusTotalNumberOfAttempts = 1;
            autoFocusNumberOfFramesPerPoint = 1;
            autoFocusInnerCropRatio = 1;
            autoFocusOuterCropRatio = 1;
            autoFocusUseBrightestStars = 0;
            backlashIn = 0;
            backlashOut = 0;
            autoFocusBinning = 1;
            autoFocusCurveFitting = AFCurveFittingEnum.HYPERBOLIC;
            contrastDetectionMethod = ContrastDetectionMethodEnum.Statistics;
            backlashCompensationModel = BacklashCompensationModel.OVERSHOOT;
            autoFocusTimeoutSeconds = 600;
            rSquaredThreshold = 0.7;
        }

        private string id;

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

        private string lastDeviceName;

        [DataMember]
        public string LastDeviceName {
            get => lastDeviceName;
            set {
                if (lastDeviceName != value) {
                    lastDeviceName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool useFilterWheelOffsets;

        [DataMember]
        public bool UseFilterWheelOffsets {
            get => useFilterWheelOffsets;
            set {
                if (useFilterWheelOffsets != value) {
                    useFilterWheelOffsets = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int autoFocusStepSize;

        [DataMember]
        public int AutoFocusStepSize {
            get => autoFocusStepSize;
            set {
                if (autoFocusStepSize != value) {
                    autoFocusStepSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int autoFocusInitialOffsetSteps;

        [DataMember]
        public int AutoFocusInitialOffsetSteps {
            get => autoFocusInitialOffsetSteps;
            set {
                if (value < 1) { value = 1; }
                if (value > 10) { value = 10; }
                if (autoFocusInitialOffsetSteps != value) {
                    autoFocusInitialOffsetSteps = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double autoFocusExposureTime;

        [DataMember]
        public double AutoFocusExposureTime {
            get => autoFocusExposureTime;
            set {
                if (autoFocusExposureTime != value) {
                    autoFocusExposureTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private AFMethodEnum autoFocusMethod;

        [DataMember]
        public AFMethodEnum AutoFocusMethod {
            get => autoFocusMethod;
            set {
                if (autoFocusMethod != value) {
                    autoFocusMethod = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ContrastDetectionMethodEnum contrastDetectionMethod;

        [DataMember]
        public ContrastDetectionMethodEnum ContrastDetectionMethod {
            get => contrastDetectionMethod;
            set {
                if (contrastDetectionMethod != value) {
                    contrastDetectionMethod = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool autoFocusDisableGuiding;

        [DataMember]
        public bool AutoFocusDisableGuiding {
            get => autoFocusDisableGuiding;
            set {
                if (autoFocusDisableGuiding != value) {
                    autoFocusDisableGuiding = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int focuserSettleTime;

        [DataMember]
        public int FocuserSettleTime {
            get => focuserSettleTime;
            set {
                if (focuserSettleTime != value) {
                    focuserSettleTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int autoFocusTotalNumberOfAttempts;

        [DataMember]
        public int AutoFocusTotalNumberOfAttempts {
            get {
                if (autoFocusTotalNumberOfAttempts < 1) { return 1; }
                if (autoFocusTotalNumberOfAttempts > 5) { return 5; }
                return autoFocusTotalNumberOfAttempts;
            }
            set {
                if(value < 1) { value = 1; }
                if(value > 5) { value = 5; }
                if (autoFocusTotalNumberOfAttempts != value) {
                    autoFocusTotalNumberOfAttempts = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int autoFocusNumberOfFramesPerPoint;

        [DataMember]
        public int AutoFocusNumberOfFramesPerPoint {
            get {
                if (autoFocusNumberOfFramesPerPoint < 1) { return 1; }
                return autoFocusNumberOfFramesPerPoint;
            }
            set {
                if (autoFocusNumberOfFramesPerPoint != value) {
                    autoFocusNumberOfFramesPerPoint = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double autoFocusInnerCropRatio;

        [DataMember]
        public double AutoFocusInnerCropRatio {
            get => autoFocusInnerCropRatio;
            set {
                if (autoFocusInnerCropRatio != value) {
                    autoFocusInnerCropRatio = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double autoFocusOuterCropRatio;

        [DataMember]
        public double AutoFocusOuterCropRatio {
            get => autoFocusOuterCropRatio;
            set {
                if (autoFocusOuterCropRatio != value) {
                    autoFocusOuterCropRatio = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int backlashIn;

        [DataMember]
        public int BacklashIn {
            get => backlashIn;
            set {
                if (backlashIn != value) {
                    backlashIn = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int backlashOut;

        [DataMember]
        public int BacklashOut {
            get => backlashOut;
            set {
                if (backlashOut != value) {
                    backlashOut = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short autoFocusBinning;

        [DataMember]
        public short AutoFocusBinning {
            get => autoFocusBinning;
            set {
                if (autoFocusBinning != value) {
                    if (value > 4) {
                        autoFocusBinning = 4;
                    } else {
                        autoFocusBinning = value;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private int autoFocusUseBrightestStars;

        [DataMember]
        public int AutoFocusUseBrightestStars {
            get => autoFocusUseBrightestStars;
            set {
                if (autoFocusUseBrightestStars != value) {
                    autoFocusUseBrightestStars = value;
                    RaisePropertyChanged();
                }
            }
        }

        private AFCurveFittingEnum autoFocusCurveFitting;

        [DataMember]
        public AFCurveFittingEnum AutoFocusCurveFitting {
            get => autoFocusCurveFitting;
            set {
                if (autoFocusCurveFitting != value) {
                    autoFocusCurveFitting = value;
                    RaisePropertyChanged();
                }
            }
        }

        private BacklashCompensationModel backlashCompensationModel;

        [DataMember]
        public BacklashCompensationModel BacklashCompensationModel {
            get => backlashCompensationModel;
            set {
                if (backlashCompensationModel != value) {
                    backlashCompensationModel = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int autoFocusTimeoutSeconds;

        [DataMember]
        public int AutoFocusTimeoutSeconds {
            get => autoFocusTimeoutSeconds;
            set {
                if (autoFocusTimeoutSeconds != value) {
                    autoFocusTimeoutSeconds = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double rSquaredThreshold;

        [DataMember]
        public double RSquaredThreshold {
            get => rSquaredThreshold;
            set {
                if (value < 0) { value = 0; }
                if (value > 1) { value = 1; }

                if (rSquaredThreshold != value) {
                    rSquaredThreshold = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}