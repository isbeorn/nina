#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Utility.Enum;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class CameraSettings : Settings, ICameraSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            id = "No_Device";
            pixelSize = 3.8;
            bulbMode = CameraBulbModeEnum.NATIVE;
            serialPort = "COM1";
            bitDepth = 16;
            bayerPattern = BayerPatternEnum.Auto;
            rawConverter = RawConverterEnum.FREEIMAGE;
            minFlatExposureTime = 0.2;
            maxFlatExposureTime = 20;
            fileCameraFolder = string.Empty;
            bitScaling = false;

            fliEnableFloodFlush = false;
            fliFloodDuration = 1;
            fliFlushCount = 2;
            fliEnableSnapshotFloodFlush = false;

            qhyIncludeOverscan = false;
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

        private double pixelSize;

        [DataMember]
        public double PixelSize {
            get => pixelSize;
            set {
                if (pixelSize != value) {
                    pixelSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private BayerPatternEnum bayerPattern;

        [DataMember]
        public BayerPatternEnum BayerPattern {
            get => bayerPattern;
            set {
                if (bayerPattern != value) {
                    bayerPattern = value;
                    RaisePropertyChanged();
                }
            }
        }

        private CameraBulbModeEnum bulbMode;

        [DataMember]
        public CameraBulbModeEnum BulbMode {
            get => bulbMode;
            set {
                if (bulbMode != value) {
                    bulbMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string serialPort;

        [DataMember]
        public string SerialPort {
            get => serialPort;
            set {
                if (serialPort != value) {
                    serialPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double bitDepth;

        [DataMember]
        public double BitDepth {
            get => bitDepth;
            set {
                if (bitDepth != value) {
                    bitDepth = value;
                    RaisePropertyChanged();
                }
            }
        }

        private RawConverterEnum rawConverter;

        [DataMember]
        public RawConverterEnum RawConverter {
            get => rawConverter;
            set {
                if (rawConverter != value) {
                    rawConverter = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double minFlatExposureTime;

        [DataMember]
        public double MinFlatExposureTime {
            get => minFlatExposureTime;
            set {
                if (minFlatExposureTime != value) {
                    minFlatExposureTime = value;
                    if (MaxFlatExposureTime < minFlatExposureTime) {
                        MaxFlatExposureTime = minFlatExposureTime;
                    }

                    RaisePropertyChanged();
                }
            }
        }

        private double maxFlatExposureTime;

        [DataMember]
        public double MaxFlatExposureTime {
            get => maxFlatExposureTime;
            set {
                if (maxFlatExposureTime != value) {
                    maxFlatExposureTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string fileCameraFolder;

        [DataMember]
        public string FileCameraFolder {
            get => fileCameraFolder;
            set {
                if (fileCameraFolder != value) {
                    fileCameraFolder = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string fileCameraExtension;

        [DataMember]
        public string FileCameraExtension {
            get => fileCameraExtension;
            set {
                if (fileCameraExtension != value) {
                    fileCameraExtension = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool fileCameraUseBulbMode;

        [DataMember]
        public bool FileCameraUseBulbMode {
            get => fileCameraUseBulbMode;
            set {
                if (fileCameraUseBulbMode != value) {
                    fileCameraUseBulbMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool fileCameraIsBayered;

        [DataMember]
        public bool FileCameraIsBayered {
            get => fileCameraIsBayered;
            set {
                if (fileCameraIsBayered != value) {
                    fileCameraIsBayered = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool fliEnableFloodFlush;

        [DataMember]
        public bool FLIEnableFloodFlush {
            get => fliEnableFloodFlush;
            set {
                if (fliEnableFloodFlush != value) {
                    fliEnableFloodFlush = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double fliFloodDuration;

        [DataMember]
        public double FLIFloodDuration {
            get => fliFloodDuration;
            set {
                if (fliFloodDuration != value) {
                    fliFloodDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private uint fliFlushCount;

        [DataMember]
        public uint FLIFlushCount {
            get => fliFlushCount;
            set {
                if (fliFlushCount != value) {
                    fliFlushCount = value;
                    RaisePropertyChanged();
                }
            }
        }

        private BinningMode fliFloodBin;

        [DataMember]
        public BinningMode FLIFloodBin {
            get => fliFloodBin;
            set {
                if (fliFloodBin != value) {
                    fliFloodBin = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool fliEnableSnapshotFloodFlush;

        [DataMember]
        public bool FLIEnableSnapshotFloodFlush {
            get => fliEnableSnapshotFloodFlush;
            set {
                if (fliEnableSnapshotFloodFlush != value) {
                    fliEnableSnapshotFloodFlush = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool bitScaling;

        [DataMember]
        public bool BitScaling {
            get => bitScaling;
            set {
                if (bitScaling != value) {
                    bitScaling = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double coolingDuration;

        [DataMember]
        public double CoolingDuration {
            get => coolingDuration;
            set {
                if (coolingDuration != value) {
                    coolingDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double warmingDuration;

        [DataMember]
        public double WarmingDuration {
            get => warmingDuration;
            set {
                if (warmingDuration != value) {
                    warmingDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double? temperature;

        [DataMember]
        public double? Temperature {
            get => temperature;
            set {
                if (temperature != value) {
                    temperature = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short? binningX;

        [DataMember]
        public short? BinningX {
            get => binningX;
            set {
                if (binningX != value) {
                    binningX = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short? binningY;

        [DataMember]
        public short? BinningY {
            get => binningY;
            set {
                if (binningY != value) {
                    binningY = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int? gain;

        [DataMember]
        public int? Gain {
            get => gain;
            set {
                if (gain != value) {
                    gain = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int? offset;

        [DataMember]
        public int? Offset {
            get => offset;
            set {
                if (offset != value) {
                    offset = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int? usbLimit;

        [DataMember]
        public int? USBLimit {
            get => usbLimit;
            set {
                if (usbLimit != value) {
                    usbLimit = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short? readoutMode;

        [DataMember]
        public short? ReadoutMode {
            get => readoutMode;
            set {
                if (readoutMode != value) {
                    readoutMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short? readoutModeForSnapImages;

        [DataMember]
        public short? ReadoutModeForSnapImages {
            get => readoutModeForSnapImages;
            set {
                if (readoutModeForSnapImages != value) {
                    readoutModeForSnapImages = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short? readoutModeForNormalImages;

        [DataMember]
        public short? ReadoutModeForNormalImages {
            get => readoutModeForNormalImages;
            set {
                if (readoutModeForNormalImages != value) {
                    readoutModeForNormalImages = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool qhyIncludeOverscan;

        [DataMember]
        public bool QhyIncludeOverscan {
            get => qhyIncludeOverscan;
            set {
                if (qhyIncludeOverscan != value) {
                    qhyIncludeOverscan = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}