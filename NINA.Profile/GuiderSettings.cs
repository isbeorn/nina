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
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class GuiderSettings : Settings, IGuiderSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            ditherPixels = 5;
            ditherRAOnly = false;
            settleTime = 10;
            pHD2ServerUrl = "localhost";
            pHD2ServerPort = 4400;
            pHD2InstanceNumber = 1;
            pHD2LargeHistorySize = 100;
            pHD2GuiderScale = GuiderScaleEnum.PIXELS;
            phd2ROIPct = 100;
            settlePixels = 1.5;
            settleTimeout = 40;
            autoRetryStartGuiding = false;
            autoRetryStartGuidingTimeoutSeconds = 300;
            maxY = 4;
            metaGuideUseIpAddressAny = false;
            metaGuidePort = 1277;
            metaGuideMinIntensity = 100;
            metaGuideLockWhenGuiding = false;
            skyGuardServerUrl = "localhost";
            skyGuardServerPort = 18700;
            skyGuardCallbackPort = 8000;
            skyGuardTimeLapsChecked = false;
            skyGuardValueMaxGuiding = 1;
            skyGuardTimeLapsGuiding = 60;
            skyGuardTimeLapsDitherChecked = false;
            skyGuardValueMaxDithering = 1;
            skyGuardTimeLapsDithering = 60;
            skyGuardTimeOutGuiding = 5;

            var defaultPHD2Path = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\PHDGuiding2\phd2.exe");

            phd2Path =
                File.Exists(defaultPHD2Path)
                ? defaultPHD2Path
                : string.Empty;
            guiderName = "PHD2";
            mgenFocalLength = 1000;
            mgenPixelMargin = 10;
            metaGuideDitherSettleSeconds = 30;

            var defaultSkyGuardPath = Environment.ExpandEnvironmentVariables(@"%PROGRAMFILES%\SkyGuard\SkyGuard.exe");
            skyGuardPath = File.Exists(defaultSkyGuardPath) ? defaultSkyGuardPath : string.Empty;

            guideChartRightAscensionColor = Colors.Blue;
            guideChartDeclinationColor = Colors.Red;
            guideChartShowCorrections = true;
        }

        private double ditherPixels;

        [DataMember]
        public double DitherPixels {
            get => ditherPixels;
            set {
                if (ditherPixels != value) {
                    ditherPixels = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool ditherRAOnly;

        [DataMember]
        public bool DitherRAOnly {
            get => ditherRAOnly;
            set {
                if (ditherRAOnly != value) {
                    ditherRAOnly = value;
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

        private int pHD2InstanceNumber;

        [DataMember]
        public int PHD2InstanceNumber {
            get => pHD2InstanceNumber;
            set {
                if (pHD2InstanceNumber != value) {
                    pHD2InstanceNumber = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string pHD2ServerUrl;

        [DataMember]
        public string PHD2ServerUrl {
            get => pHD2ServerUrl;
            set {
                if (pHD2ServerUrl != value) {
                    pHD2ServerUrl = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int pHD2ServerPort;

        [DataMember]
        public int PHD2ServerPort {
            get => pHD2ServerPort;
            set {
                if (pHD2ServerPort != value) {
                    pHD2ServerPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int pHD2LargeHistorySize;

        [DataMember]
        public int PHD2HistorySize {
            get => pHD2LargeHistorySize;
            set {
                if (pHD2LargeHistorySize != value) {
                    pHD2LargeHistorySize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string phd2Path;

        [DataMember]
        public string PHD2Path {
            get => phd2Path;
            set {
                if (phd2Path != value) {
                    phd2Path = value;
                    RaisePropertyChanged();
                }
            }
        }

        private GuiderScaleEnum pHD2GuiderScale;

        [DataMember]
        public GuiderScaleEnum PHD2GuiderScale {
            get => pHD2GuiderScale;
            set {
                if (pHD2GuiderScale != value) {
                    pHD2GuiderScale = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double settlePixels;

        [DataMember]
        public double SettlePixels {
            get => settlePixels;

            set {
                if (settlePixels != value) {
                    settlePixels = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int settleTimeout;

        [DataMember]
        public int SettleTimeout {
            get => settleTimeout;

            set {
                if (settleTimeout != value) {
                    settleTimeout = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string guiderName;

        [DataMember]
        public string GuiderName {
            get => guiderName;
            set {
                if (guiderName != value) {
                    guiderName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool autoRetryStartGuiding;

        [DataMember]
        public bool AutoRetryStartGuiding {
            get => autoRetryStartGuiding;
            set {
                if (autoRetryStartGuiding == value) return;
                autoRetryStartGuiding = value;
                RaisePropertyChanged();
            }
        }

        private int autoRetryStartGuidingTimeoutSeconds;

        [DataMember]
        public int AutoRetryStartGuidingTimeoutSeconds {
            get => autoRetryStartGuidingTimeoutSeconds;
            set {
                if (autoRetryStartGuidingTimeoutSeconds == value) return;
                autoRetryStartGuidingTimeoutSeconds = value;
                RaisePropertyChanged();
            }
        }

        private double maxY;

        [DataMember]
        public double MaxY {
            get => maxY;
            set {
                if (maxY != value) {
                    maxY = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool metaGuideUseIpAddressAny;

        [DataMember]
        public bool MetaGuideUseIpAddressAny {
            get => metaGuideUseIpAddressAny;
            set {
                if (metaGuideUseIpAddressAny != value) {
                    metaGuideUseIpAddressAny = value;
                    RaisePropertyChanged();
                }
            }
        
        }

        private int metaGuidePort;

        [DataMember]
        public int MetaGuidePort {
            get => metaGuidePort;
            set {
                if (metaGuidePort != value) {
                    metaGuidePort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int mgenFocalLength;

        [DataMember]
        public int MGENFocalLength {
            get => mgenFocalLength;
            set {
                if (mgenFocalLength != value) {
                    mgenFocalLength = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int mgenPixelMargin;

        [DataMember]
        public int MGENPixelMargin {
            get => mgenPixelMargin;
            set {
                if (mgenPixelMargin != value) {
                    mgenPixelMargin = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int metaGuideMinIntensity;

        [DataMember]
        public int MetaGuideMinIntensity {
            get => metaGuideMinIntensity;
            set {
                if (metaGuideMinIntensity != value) {
                    metaGuideMinIntensity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int metaGuideDitherSettleSeconds;

        [DataMember]
        public int MetaGuideDitherSettleSeconds {
            get => metaGuideDitherSettleSeconds;
            set {
                if (metaGuideDitherSettleSeconds != value) {
                    metaGuideDitherSettleSeconds = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool metaGuideLockWhenGuiding;

        [DataMember]
        public bool MetaGuideLockWhenGuiding {
            get => metaGuideLockWhenGuiding;
            set {
                if (metaGuideLockWhenGuiding != value) {
                    metaGuideLockWhenGuiding = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int phd2ROIPct;

        [DataMember]
        public int PHD2ROIPct {
            get => phd2ROIPct;
            set {
                if (phd2ROIPct != value) {
                    phd2ROIPct = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int? phd2ProfileId;

        [DataMember]
        public int? PHD2ProfileId {
            get => phd2ProfileId;
            set {
                if (phd2ProfileId != value) {
                    phd2ProfileId = value;
                    RaisePropertyChanged();
                }
            }
        }

        #region SkyGuard settings
        string skyGuardServerUrl;
        int skyGuardServerPort;
        string skyGuardPath;
        int skyGuardCallbackPort;
        bool skyGuardTimeLapsChecked;
        double skyGuardValueMaxGuiding;
        double skyGuardTimeLapsGuiding;
        bool skyGuardTimeLapsDitherChecked;
        double skyGuardValueMaxDithering;
        double skyGuardTimeLapsDithering;
        double skyGuardTimeOutGuiding;

        /// <summary>
        /// Property allowing to set the endpoint URL for SkyGuard software
        /// </summary>
        [DataMember]
        public string SkyGuardServerUrl
        {
            get => skyGuardServerUrl;
            set
            {
                if (skyGuardServerUrl != value)
                {
                    skyGuardServerUrl = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Property to set endpoint URL port for SkyGuard software
        /// </summary>
        [DataMember]
        public int SkyGuardServerPort
        {
            get => skyGuardServerPort;
            set
            {
                if (skyGuardServerPort != value)
                {
                    skyGuardServerPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Property allowing to set SkyGuard.exe file path
        /// </summary>
        [DataMember]
        public string SkyGuardPath
        {
            get => skyGuardPath;
            set
            {
                if (skyGuardPath != value)
                {
                    skyGuardPath = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Property to set callback port
        /// </summary>
        [DataMember]
        public int SkyGuardCallbackPort
        {
            get => skyGuardCallbackPort;
            set
            {
                if (skyGuardCallbackPort != value)
                {
                    skyGuardCallbackPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Property to set callback port
        /// </summary>
        [DataMember]
        public bool SkyGuardTimeLapsChecked
        {
            get => skyGuardTimeLapsChecked;
            set
            {
                if (skyGuardTimeLapsChecked != value)
                {
                    skyGuardTimeLapsChecked = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public double SkyGuardValueMaxGuiding
        {
            get => skyGuardValueMaxGuiding;
            set
            {
                if (skyGuardValueMaxGuiding != value)
                {
                    skyGuardValueMaxGuiding = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public double SkyGuardTimeLapsGuiding
        {
            get => skyGuardTimeLapsGuiding;
            set
            {
                if (skyGuardTimeLapsGuiding != value)
                {
                    skyGuardTimeLapsGuiding = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public bool SkyGuardTimeLapsDitherChecked {
            get => skyGuardTimeLapsDitherChecked;
            set {
                if (skyGuardTimeLapsDitherChecked != value) {
                    skyGuardTimeLapsDitherChecked = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public double SkyGuardValueMaxDithering {
            get => skyGuardValueMaxDithering;
            set {
                if (skyGuardValueMaxDithering != value) {
                    skyGuardValueMaxDithering = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public double SkyGuardTimeLapsDithering {
            get => skyGuardTimeLapsDithering;
            set {
                if (skyGuardTimeLapsDithering != value) {
                    skyGuardTimeLapsDithering = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public double SkyGuardTimeOutGuiding
        {
            get => skyGuardTimeOutGuiding;
            set
            {
                if (skyGuardTimeOutGuiding != value)
                {
                    skyGuardTimeOutGuiding = value;
                    RaisePropertyChanged();
                }
            }
        }
        #endregion


        private Color guideChartRightAscensionColor;        
        [DataMember]
        public Color GuideChartRightAscensionColor {
            get => guideChartRightAscensionColor;
            set {
                if (guideChartRightAscensionColor != value) {
                    guideChartRightAscensionColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color guideChartDeclinationColor;        
        [DataMember]
        public Color GuideChartDeclinationColor {
            get => guideChartDeclinationColor;
            set {
                if (guideChartDeclinationColor != value) {
                    guideChartDeclinationColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool guideChartShowCorrections;
        [DataMember]
        public bool GuideChartShowCorrections {
            get => guideChartShowCorrections;
            set {
                if (guideChartShowCorrections != value) {
                    guideChartShowCorrections = value;
                    RaisePropertyChanged();
                }
            }

        }
    }
}