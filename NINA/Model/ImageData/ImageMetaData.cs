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

using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Model.MyWeatherData;
using NINA.Profile;
using NINA.Utility.Astrometry;
using System;
using System.Linq;

namespace NINA.Model.ImageData {

    public class ImageMetaData {
        public ImageParameter Image { get; set; } = new ImageParameter();
        public CameraParameter Camera { get; set; } = new CameraParameter();
        public TelescopeParameter Telescope { get; set; } = new TelescopeParameter();
        public FocuserParameter Focuser { get; set; } = new FocuserParameter();
        public RotatorParameter Rotator { get; set; } = new RotatorParameter();
        public FilterWheelParameter FilterWheel { get; set; } = new FilterWheelParameter();
        public TargetParameter Target { get; set; } = new TargetParameter();
        public ObserverParameter Observer { get; set; } = new ObserverParameter();
        public WeatherDataParameter WeatherData { get; set; } = new WeatherDataParameter();

        /// <summary>
        /// Fill relevant info from a Profile
        /// </summary>
        /// <param name="profile"></param>
        public void FromProfile(IProfile profile) {
            Camera.PixelSize = profile.CameraSettings.PixelSize;

            Telescope.Name = profile.TelescopeSettings.Name;
            Telescope.FocalLength = profile.TelescopeSettings.FocalLength;
            Telescope.FocalRatio = profile.TelescopeSettings.FocalRatio;

            Observer.Latitude = profile.AstrometrySettings.Latitude;
            Observer.Longitude = profile.AstrometrySettings.Longitude;
        }

        public void FromCameraInfo(CameraInfo info) {
            if (info.Connected) {
                Camera.Name = info.Name;
                Camera.Temperature = info.Temperature;
                Camera.Gain = info.Gain;
                Camera.Offset = info.Offset;
                Camera.SetPoint = info.TemperatureSetPoint;
                Camera.BinX = info.BinX;
                Camera.BinY = info.BinY;
                Camera.ElectronsPerADU = info.ElectronsPerADU;
                Camera.PixelSize = info.PixelSize;

                if (info.ReadoutModes.Count() > 1) {
                    Camera.ReadoutModeName = info.ReadoutModes.ToArray()[info.ReadoutMode];
                }
            }
        }

        public void FromTelescopeInfo(TelescopeInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(Telescope.Name)) {
                    Telescope.Name = info.Name;
                }
                Observer.Elevation = info.SiteElevation;
                Telescope.Coordinates = info.Coordinates;
            }
        }

        public void FromFilterWheelInfo(FilterWheelInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(FilterWheel.Name)) {
                    FilterWheel.Name = info.Name;
                }
                FilterWheel.Filter = info.SelectedFilter?.Name ?? string.Empty;
            }
        }

        public void FromFocuserInfo(FocuserInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(Focuser.Name)) {
                    Focuser.Name = info.Name;
                }
                Focuser.Position = info.Position;
                Focuser.StepSize = info.StepSize;
                Focuser.Temperature = info.Temperature;
            }
        }

        public void FromRotatorInfo(RotatorInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(Rotator.Name)) {
                    Rotator.Name = info.Name;
                }
                Rotator.Position = info.Position;
                Rotator.StepSize = info.StepSize;
            }
        }

        public void FromWeatherDataInfo(WeatherDataInfo info) {
            if (info.Connected) {
                WeatherData.CloudCover = info.CloudCover;
                WeatherData.DewPoint = info.DewPoint;
                WeatherData.Humidity = info.Humidity;
                WeatherData.Pressure = info.Pressure;
                WeatherData.SkyBrightness = info.SkyBrightness;
                WeatherData.SkyQuality = info.SkyQuality;
                WeatherData.SkyTemperature = info.SkyTemperature;
                WeatherData.StarFWHM = info.StarFWHM;
                WeatherData.Temperature = info.Temperature;
                WeatherData.WindDirection = info.WindDirection;
                WeatherData.WindGust = info.WindGust;
                WeatherData.WindSpeed = info.WindSpeed;
            }
        }
    }

    public class ImageParameter {
        public DateTime ExposureStart { get; set; } = DateTime.MinValue;
        public int ExposureNumber { get; set; } = -1;
        public string ImageType { get; set; } = string.Empty;
        public string Binning { get; set; } = string.Empty;
        public double ExposureTime { get; set; } = double.NaN;
        public RMS RecordedRMS { get; set; } = null;
    }

    public class CameraParameter {
        public string Name { get; set; } = string.Empty;
        public string Binning { get => $"{BinX}x{BinY}"; }
        public int BinX { get; set; } = 1;
        public int BinY { get; set; } = 1;
        public double PixelSize { get; set; } = double.NaN;
        public double Temperature { get; set; } = double.NaN;
        public int Gain { get; set; } = -1;
        public int Offset { get; set; } = -1;
        public double ElectronsPerADU { get; set; } = double.NaN;
        public double SetPoint { get; set; } = double.NaN;
        public string ReadoutModeName { get; set; } = string.Empty;
    }

    public class TelescopeParameter {
        public string Name { get; set; } = string.Empty;
        public double FocalLength { get; set; } = double.NaN;
        public double FocalRatio { get; set; } = double.NaN;
        public Coordinates Coordinates { get; set; } = null;
    }

    public class FocuserParameter {
        public string Name { get; set; } = string.Empty;
        public double Position { get; set; } = double.NaN;
        public double StepSize { get; set; } = double.NaN;
        public double Temperature { get; set; } = double.NaN;
    }

    public class RotatorParameter {
        public string Name { get; set; } = string.Empty;
        public double Position { get; set; } = double.NaN;
        public double StepSize { get; set; } = double.NaN;
    }

    public class FilterWheelParameter {
        public string Name { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
    }

    public class TargetParameter {
        private string name = string.Empty;

        public string Name {
            get => name;
            set {
                name = value;
                name = name.Replace("\\", "-").Replace("/", "-");
            }
        }

        public Coordinates Coordinates { get; set; } = null;
    }

    public class ObserverParameter {
        public double Latitude { get; set; } = double.NaN;
        public double Longitude { get; set; } = double.NaN;
        public double Elevation { get; set; } = double.NaN;
    }

    public class WeatherDataParameter {
        public double CloudCover { get; set; } = double.NaN;
        public double DewPoint { get; set; } = double.NaN;
        public double Humidity { get; set; } = double.NaN;
        public double Pressure { get; set; } = double.NaN;
        public double SkyBrightness { get; set; } = double.NaN;
        public double SkyQuality { get; set; } = double.NaN;
        public double SkyTemperature { get; set; } = double.NaN;
        public double StarFWHM { get; set; } = double.NaN;
        public double Temperature { get; set; } = double.NaN;
        public double WindDirection { get; set; } = double.NaN;
        public double WindGust { get; set; } = double.NaN;
        public double WindSpeed { get; set; } = double.NaN;
    }
}