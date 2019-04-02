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

using NINA.Utility.Enum;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class CameraSettings : Settings, ICameraSettings {
        private string id;

        public CameraSettings() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDeseralization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            id = "No_Device";
            pixelSize = 3.8;
            bulbMode = CameraBulbModeEnum.NATIVE;
            serialPort = "COM1";
            readNoise = 0.0;
            bitDepth = 16;
            offset = 0.0;
            fullWellCapacity = 20000;
            downloadToDataRatio = 9;
            rawConverter = RawConverterEnum.DCRAW;
            minFlatExposureTime = 0.2;
            maxFlatExposureTime = 20;
        }

        [DataMember]
        public string Id {
            get => id;
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private double pixelSize;

        [DataMember]
        public double PixelSize {
            get => pixelSize;
            set {
                pixelSize = value;
                RaisePropertyChanged();
            }
        }

        private CameraBulbModeEnum bulbMode;

        [DataMember]
        public CameraBulbModeEnum BulbMode {
            get => bulbMode;
            set {
                bulbMode = value;
                RaisePropertyChanged();
            }
        }

        private string serialPort;

        [DataMember]
        public string SerialPort {
            get => serialPort;
            set {
                serialPort = value;
                RaisePropertyChanged();
            }
        }

        private double readNoise;

        [DataMember]
        public double ReadNoise {
            get => readNoise;
            set {
                readNoise = value;
                RaisePropertyChanged();
            }
        }

        private double bitDepth;

        [DataMember]
        public double BitDepth {
            get => bitDepth;
            set {
                bitDepth = value;
                RaisePropertyChanged();
            }
        }

        private double offset;

        [DataMember]
        public double Offset {
            get => offset;
            set {
                offset = value;
                RaisePropertyChanged();
            }
        }

        private double fullWellCapacity;

        [DataMember]
        public double FullWellCapacity {
            get => fullWellCapacity;
            set {
                fullWellCapacity = value;
                RaisePropertyChanged();
            }
        }

        private double downloadToDataRatio;

        [DataMember]
        public double DownloadToDataRatio {
            get => downloadToDataRatio;
            set {
                downloadToDataRatio = value;
                RaisePropertyChanged();
            }
        }

        private RawConverterEnum rawConverter;

        [DataMember]
        public RawConverterEnum RawConverter {
            get => rawConverter;
            set {
                rawConverter = value;
                RaisePropertyChanged();
            }
        }

        private double minFlatExposureTime;

        [DataMember]
        public double MinFlatExposureTime {
            get => minFlatExposureTime;
            set {
                minFlatExposureTime = value;
                if (MaxFlatExposureTime < minFlatExposureTime) {
                    MaxFlatExposureTime = minFlatExposureTime;
                }

                RaisePropertyChanged();
            }
        }

        private double maxFlatExposureTime;

        [DataMember]
        public double MaxFlatExposureTime {
            get => maxFlatExposureTime;
            set {
                maxFlatExposureTime = value;
                RaisePropertyChanged();
            }
        }
    }
}