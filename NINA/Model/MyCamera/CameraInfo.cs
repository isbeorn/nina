#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using System.Collections.Generic;

namespace NINA.Model.MyCamera {

    public class CameraInfo : DeviceInfo {
        private bool canSetTemperature;

        public bool CanSetTemperature {
            get { return canSetTemperature; }
            set { canSetTemperature = value; RaisePropertyChanged(); }
        }

        private bool hasShutter;

        public bool HasShutter {
            get { return hasShutter; }
            set { hasShutter = value; RaisePropertyChanged(); }
        }

        private double temperature;

        public double Temperature {
            get { return temperature; }
            set { temperature = value; RaisePropertyChanged(); }
        }

        private int gain;

        public int Gain {
            get { return gain; }
            set { gain = value; RaisePropertyChanged(); }
        }

        private double electronsPerADU;

        public double ElectronsPerADU {
            get { return electronsPerADU; }
            set { electronsPerADU = value; RaisePropertyChanged(); }
        }

        private short binxX;

        public short BinX {
            get { return binxX; }
            set { binxX = value; RaisePropertyChanged(); }
        }

        private int bitDepth;

        public int BitDepth {
            get {
                return bitDepth;
            }
            set {
                bitDepth = value;
                RaisePropertyChanged();
            }
        }

        private short binY;

        public short BinY {
            get { return binY; }
            set { binY = value; RaisePropertyChanged(); }
        }

        private int offset;

        public int Offset {
            get { return offset; }
            set { offset = value; RaisePropertyChanged(); }
        }

        private bool isSubSampleEnabled;

        public bool IsSubSampleEnabled {
            get { return isSubSampleEnabled; }
            set { isSubSampleEnabled = value; RaisePropertyChanged(); }
        }

        private string cameraState;

        public string CameraState {
            get { return cameraState; }
            set { cameraState = value; RaisePropertyChanged(); }
        }

        private int xSize;

        public int XSize {
            get { return xSize; }
            set { xSize = value; RaisePropertyChanged(); }
        }

        private int ySize;

        public int YSize {
            get { return ySize; }
            set { ySize = value; RaisePropertyChanged(); }
        }

        private double pixelSize;

        public double PixelSize {
            get { return pixelSize; }
            set { pixelSize = value; RaisePropertyChanged(); }
        }

        private int battery;

        public int Battery {
            get { return battery; }
            set { battery = value; RaisePropertyChanged(); }
        }

        private bool coolerOn;

        public bool CoolerOn {
            get {
                return coolerOn;
            }
            set {
                coolerOn = value;
                RaisePropertyChanged();
            }
        }

        private double _coolerPower;

        public double CoolerPower {
            get {
                return _coolerPower;
            }
            set {
                _coolerPower = value;
                RaisePropertyChanged();
            }
        }

        private bool dewHeaterOn;

        public bool DewHeaterOn {
            get {
                return dewHeaterOn;
            }
            set {
                dewHeaterOn = value;
                RaisePropertyChanged();
            }
        }

        private bool canSubSample;

        public bool CanSubSample {
            get { return canSubSample; }
            set { canSubSample = value; RaisePropertyChanged(); }
        }

        private int subSampleX;

        public int SubSampleX {
            get { return subSampleX; }
            set { subSampleX = value; RaisePropertyChanged(); }
        }

        private int subSampleY;

        public int SubSampleY {
            get { return subSampleY; }
            set { subSampleY = value; RaisePropertyChanged(); }
        }

        private int subSampleWidth;

        public int SubSampleWidth {
            get { return subSampleWidth; }
            set { subSampleWidth = value; RaisePropertyChanged(); }
        }

        private int subSampleHeight;

        public int SubSampleHeight {
            get { return subSampleHeight; }
            set { subSampleHeight = value; RaisePropertyChanged(); }
        }

        private double temperatureSetPoint;

        public double TemperatureSetPoint {
            get { return temperatureSetPoint; }
            set { temperatureSetPoint = value; RaisePropertyChanged(); }
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
                readoutMode = value;
                RaisePropertyChanged();
            }
        }

        private short snapReadoutMode;

        public short ReadoutModeForSnapImages {
            get => snapReadoutMode;
            set {
                snapReadoutMode = value;
                RaisePropertyChanged();
            }
        }

        private short normalReadoutMode;

        public short ReadoutModeForNormalImages {
            get => normalReadoutMode;
            set {
                normalReadoutMode = value;
                RaisePropertyChanged();
            }
        }

        private bool isExposing;

        public bool IsExposing {
            get => isExposing;
            set {
                isExposing = value;
                RaisePropertyChanged();
            }
        }

        private DateTime exposureEndTime = DateTime.Now;

        public DateTime ExposureEndTime {
            get => exposureEndTime;
            set {
                exposureEndTime = value;
                RaisePropertyChanged();
            }
        }

        private double nextExposureLength = -1;

        public double NextExposureLength {
            get => nextExposureLength;
            set {
                nextExposureLength = value;
                RaisePropertyChanged();
            }
        }

        private double lastDownloadTime = -1;

        public double LastDownloadTime {
            get => lastDownloadTime;
            set {
                lastDownloadTime = value;
                RaisePropertyChanged();
            }
        }
    }
}