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
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyCamera {

    public class CameraInfo : DeviceInfo {
        private bool canSetTemperature;

        public bool CanSetTemperature {
            get => canSetTemperature;
            set { if (canSetTemperature != value) { canSetTemperature = value; RaisePropertyChanged(); } }
        }

        private bool hasShutter;

        public bool HasShutter {
            get => hasShutter;
            set { if (hasShutter != value) { hasShutter = value; RaisePropertyChanged(); } }
        }

        private double temperature;

        public double Temperature {
            get => temperature;
            set { if (temperature != value) { temperature = value; RaisePropertyChanged(); } }
        }

        private int gain = -1;

        public int Gain {
            get => gain;
            set { if (gain != value) { gain = value; RaisePropertyChanged(); } }
        }

        private int defaultGain = -1;

        public int DefaultGain {
            get => defaultGain;
            set {
                if (value != defaultGain) {
                    defaultGain = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double electronsPerADU;

        public double ElectronsPerADU {
            get => electronsPerADU;
            set { if (electronsPerADU != value) { electronsPerADU = value; RaisePropertyChanged(); } }
        }

        private short binxX;

        public short BinX {
            get => binxX;
            set { if (binxX != value) { binxX = value; RaisePropertyChanged(); } }
        }

        private int bitDepth;

        public int BitDepth {
            get => bitDepth;
            set {
                if (bitDepth != value) {
                    bitDepth = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short binY;

        public short BinY {
            get => binY;
            set { if (binY != value) { binY = value; RaisePropertyChanged(); } }
        }

        private bool canSetOffset;

        public bool CanSetOffset {
            get => canSetOffset;
            set { if (canSetOffset != value) { canSetOffset = value; RaisePropertyChanged(); } }
        }

        private bool canGetGain;

        public bool CanGetGain {
            get => canGetGain;
            set { if (canGetGain != value) { canGetGain = value; RaisePropertyChanged(); } }
        }

        public int offsetMin;

        public int OffsetMin {
            get => offsetMin;
            set { if (offsetMin != value) { offsetMin = value; RaisePropertyChanged(); } }
        }

        public int offsetMax;

        public int OffsetMax {
            get => offsetMax;
            set { if (offsetMax != value) { offsetMax = value; RaisePropertyChanged(); } }
        }

        private int offset;

        public int Offset {
            get => offset;
            set { if (offset != value) { offset = value; RaisePropertyChanged(); } }
        }

        private int defaultOffset = -1;

        public int DefaultOffset {
            get => defaultOffset;
            set {
                if (value != defaultOffset) {
                    defaultOffset = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int usbLimit;

        public int USBLimit {
            get => usbLimit;
            set { if (usbLimit != value) { usbLimit = value; RaisePropertyChanged(); } }
        }

        private bool isSubSampleEnabled;

        public bool IsSubSampleEnabled {
            get => isSubSampleEnabled;
            set { if (isSubSampleEnabled != value) { isSubSampleEnabled = value; RaisePropertyChanged(); } }
        }

        private CameraStates cameraState = CameraStates.NoState;

        public CameraStates CameraState {
            get => cameraState;
            set { if (cameraState != value) { cameraState = value; RaisePropertyChanged(); } }
        }

        private int xSize;

        public int XSize {
            get => xSize;
            set { if (xSize != value) { xSize = value; RaisePropertyChanged(); } }
        }

        private int ySize;

        public int YSize {
            get => ySize;
            set { if (ySize != value) { ySize = value; RaisePropertyChanged(); } }
        }

        private double pixelSize;

        public double PixelSize {
            get => pixelSize;
            set { if (pixelSize != value) { pixelSize = value; RaisePropertyChanged(); } }
        }

        private bool hasBattery;

        public bool HasBattery {
            get => hasBattery;
            set { if (hasBattery != value) { hasBattery = value; RaisePropertyChanged(); } }
        }

        private int battery;

        public int Battery {
            get => battery;
            set { if (battery != value) { battery = value; RaisePropertyChanged(); } }
        }

        private int gainMin;

        public int GainMin {
            get => gainMin;
            set {
                if (gainMin != value) {
                    gainMin = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int gainMax;

        public int GainMax {
            get => gainMax;
            set {
                if (gainMax != value) {
                    gainMax = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canSetGain;

        public bool CanSetGain {
            get => canSetGain;
            set {
                if (canSetGain != value) {
                    canSetGain = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IList<int> gains = new List<int>();

        public IList<int> Gains {
            get => gains;
            set {
                gains = value;
                RaisePropertyChanged();
            }
        }

        private bool coolerOn;

        public bool CoolerOn {
            get => coolerOn;
            set {
                if (coolerOn != value) {
                    coolerOn = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _coolerPower;

        public double CoolerPower {
            get => _coolerPower;
            set {
                _coolerPower = value;
                RaisePropertyChanged();
            }
        }

        private bool hasDewHeater;

        public bool HasDewHeater {
            get => hasDewHeater;
            set {
                hasDewHeater = value;
                RaisePropertyChanged();
            }
        }

        private bool dewHeaterOn;

        public bool DewHeaterOn {
            get => dewHeaterOn;
            set {
                dewHeaterOn = value;
                RaisePropertyChanged();
            }
        }

        private bool canSubSample;

        public bool CanSubSample {
            get => canSubSample;
            set { if (canSubSample != value) { canSubSample = value; RaisePropertyChanged(); } }
        }

        private int subSampleX;

        public int SubSampleX {
            get => subSampleX;
            set { if (subSampleX != value) { subSampleX = value; RaisePropertyChanged(); } }
        }

        private int subSampleY;

        public int SubSampleY {
            get => subSampleY;
            set { if (subSampleY != value) { subSampleY = value; RaisePropertyChanged(); } }
        }

        private int subSampleWidth;

        public int SubSampleWidth {
            get => subSampleWidth;
            set { if (subSampleWidth != value) { subSampleWidth = value; RaisePropertyChanged(); } }
        }

        private int subSampleHeight;

        public int SubSampleHeight {
            get => subSampleHeight;
            set { if (subSampleHeight != value) { subSampleHeight = value; RaisePropertyChanged(); } }
        }

        private double temperatureSetPoint;

        public double TemperatureSetPoint {
            get => temperatureSetPoint;
            set { if (temperatureSetPoint != value) { temperatureSetPoint = value; RaisePropertyChanged(); } }
        }

        private IEnumerable<string> readoutModes;

        public IEnumerable<string> ReadoutModes {
            get => readoutModes;
            set {
                readoutModes = value;
                RaisePropertyChanged();
            }
        }

        private short readoutMode;

        public short ReadoutMode {
            get => readoutMode;
            set {
                if (readoutMode != value) {
                    readoutMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short snapReadoutMode;

        public short ReadoutModeForSnapImages {
            get => snapReadoutMode;
            set {
                if (snapReadoutMode != value) {
                    snapReadoutMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short normalReadoutMode;

        public short ReadoutModeForNormalImages {
            get => normalReadoutMode;
            set {
                if (normalReadoutMode != value) {
                    normalReadoutMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isExposing;

        public bool IsExposing {
            get => isExposing;
            set {
                if (isExposing != value) {
                    isExposing = value;
                    RaisePropertyChanged();
                }
            }
        }

        private DateTime exposureEndTime = DateTime.Now;

        public DateTime ExposureEndTime {
            get => exposureEndTime;
            set {
                if (exposureEndTime != value) {
                    exposureEndTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double lastDownloadTime = -1;

        public double LastDownloadTime {
            get => lastDownloadTime;
            set {
                if (lastDownloadTime != value) {
                    lastDownloadTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private SensorType sensorType = SensorType.Monochrome;

        public SensorType SensorType {
            get => sensorType;
            set {
                if (sensorType != value) {
                    sensorType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short bayerOffsetX = 0;

        public short BayerOffsetX {
            get => bayerOffsetX;
            set {
                if (bayerOffsetX != value) {
                    bayerOffsetX = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short bayerOffsetY = 0;

        public short BayerOffsetY {
            get => bayerOffsetY;
            set {
                if (bayerOffsetY != value) {
                    bayerOffsetY = value;
                    RaisePropertyChanged();
                }
            }
        }

        private AsyncObservableCollection<BinningMode> binningModes = new AsyncObservableCollection<BinningMode>();

        public AsyncObservableCollection<BinningMode> BinningModes {
            get => binningModes;
            set {
                binningModes = value;
                RaisePropertyChanged();
            }
        }

        private double exposureMax = 0;

        public double ExposureMax {
            get => exposureMax;
            set {
                if (exposureMax != value) {
                    exposureMax = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double exposureMin = 0;

        public double ExposureMin {
            get => exposureMin;
            set {
                if (exposureMin != value) {
                    exposureMin = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool liveViewEnabled = false;

        public bool LiveViewEnabled {
            get => liveViewEnabled;
            set {
                if (liveViewEnabled != value) {
                    liveViewEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canShowLiveView = false;

        public bool CanShowLiveView {
            get => canShowLiveView;
            set {
                if (canShowLiveView != value) {
                    canShowLiveView = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IList<string> supportedActions;

        public IList<string> SupportedActions {
            get => supportedActions;
            set {
                supportedActions = value;
                RaisePropertyChanged();
            }
        }
        private bool canSetUSBLimit;

        public bool CanSetUSBLimit {
            get => canSetUSBLimit;
            set { if (canSetUSBLimit != value) { canSetUSBLimit = value; RaisePropertyChanged(); } }
        }

        private int uSBLimitMin;
        public int USBLimitMin {
            get => uSBLimitMin;
            set { if (uSBLimitMin != value) { uSBLimitMin = value; RaisePropertyChanged(); } }
        }

        private int uSBLimitMax;
        public int USBLimitMax {
            get => uSBLimitMax;
            set { if (uSBLimitMax != value) { uSBLimitMax = value; RaisePropertyChanged(); } }
        }
    }
}