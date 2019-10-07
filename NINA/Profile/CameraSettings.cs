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
            readNoise = 0.0;
            bitDepth = 16;
            offset = 0.0;
            fullWellCapacity = 20000;
            downloadToDataRatio = 9;
            rawConverter = RawConverterEnum.FREEIMAGE;
            minFlatExposureTime = 0.2;
            maxFlatExposureTime = 20;
            fileCameraFolder = string.Empty;
            bitScaling = false;

            fliEnableFloodFlush = false;
            fliFloodDuration = 1;
            fliFlushCount = 2;
            fliEnableSnapshotFloodFlush = false;
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

        private double readNoise;

        [DataMember]
        public double ReadNoise {
            get => readNoise;
            set {
                if (readNoise != value) {
                    readNoise = value;
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

        private double offset;

        [DataMember]
        public double Offset {
            get => offset;
            set {
                if (offset != value) {
                    offset = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double fullWellCapacity;

        [DataMember]
        public double FullWellCapacity {
            get => fullWellCapacity;
            set {
                if (fullWellCapacity != value) {
                    fullWellCapacity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double downloadToDataRatio;

        [DataMember]
        public double DownloadToDataRatio {
            get => downloadToDataRatio;
            set {
                if (downloadToDataRatio != value) {
                    downloadToDataRatio = value;
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
    }
}