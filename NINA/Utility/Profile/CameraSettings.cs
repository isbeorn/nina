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
using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class CameraSettings : Settings, ICameraSettings {
        private string id = "No_Device";

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private double pixelSize = 3.8;

        [DataMember]
        public double PixelSize {
            get {
                return pixelSize;
            }
            set {
                pixelSize = value;
                RaisePropertyChanged();
            }
        }

        private CameraBulbModeEnum bulbMode = CameraBulbModeEnum.NATIVE;

        [DataMember]
        public CameraBulbModeEnum BulbMode {
            get {
                return bulbMode;
            }
            set {
                bulbMode = value;
                RaisePropertyChanged();
            }
        }

        private string serialPort = "COM1";

        [DataMember]
        public string SerialPort {
            get {
                return serialPort;
            }
            set {
                serialPort = value;
                RaisePropertyChanged();
            }
        }

        private double _readNoise = 0.0;

        [DataMember]
        public double ReadNoise {
            get {
                return _readNoise;
            }
            set {
                _readNoise = value;
                RaisePropertyChanged();
            }
        }

        private double _bitDepth = 16;

        [DataMember]
        public double BitDepth {
            get {
                return _bitDepth;
            }
            set {
                _bitDepth = value;
                RaisePropertyChanged();
            }
        }

        private double _offset = 0;

        [DataMember]
        public double Offset {
            get {
                return _offset;
            }
            set {
                _offset = value;
                RaisePropertyChanged();
            }
        }

        private double _fullWellCapacity = 20000;

        [DataMember]
        public double FullWellCapacity {
            get {
                return _fullWellCapacity;
            }
            set {
                _fullWellCapacity = value;
                RaisePropertyChanged();
            }
        }

        private double _downloadToDataRatio = 9;

        [DataMember]
        public double DownloadToDataRatio {
            get {
                return _downloadToDataRatio;
            }
            set {
                _downloadToDataRatio = value;
                RaisePropertyChanged();
            }
        }

        private RawConverterEnum _rawConverter = RawConverterEnum.DCRAW;

        [DataMember]
        public RawConverterEnum RawConverter {
            get {
                return _rawConverter;
            }
            set {
                _rawConverter = value;
                RaisePropertyChanged();
            }
        }
    }
}