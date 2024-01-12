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
using NINA.Astrometry;
using System;
using System.Linq;
using NINA.Core.Model;
using System.Collections.Generic;

namespace NINA.Image.ImageData {

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
        public WorldCoordinateSystem WorldCoordinateSystem = null;
        public SequenceParameter Sequence = new SequenceParameter();
        public List<IGenericMetaDataHeader> GenericHeaders { get; set; } = new List<IGenericMetaDataHeader>();

        /// <summary>
        /// Fill relevant info from a Profile
        /// </summary>
        /// <param name="profile"></param>
        public void FromProfile(IProfile profile) {
            Camera.PixelSize = profile.CameraSettings.PixelSize;

            if (Camera.BayerPattern != BayerPatternEnum.None) {
                Camera.BayerPattern = profile.CameraSettings.BayerPattern;
            }

            Telescope.Name = profile.TelescopeSettings.Name;
            Telescope.FocalLength = profile.TelescopeSettings.FocalLength;
            Telescope.FocalRatio = profile.TelescopeSettings.FocalRatio;

            Observer.Latitude = profile.AstrometrySettings.Latitude;
            Observer.Longitude = profile.AstrometrySettings.Longitude;
        }

        public SensorType StringToSensorType(string pattern) {
            return Enum.TryParse(pattern, out SensorType sensor) ? sensor : SensorType.Monochrome;
        }
    }

    public interface IGenericMetaDataHeader {
        string Key { get; }
        string Comment { get; }
    }

    public interface IGenericMetaDataHeader<T> : IGenericMetaDataHeader {
        T Value { get; }
    }

    public class StringMetaDataHeader : IGenericMetaDataHeader<string> {
        public string Key { get; }
        public string Value { get; }
        public string Comment { get; }

        public StringMetaDataHeader(string key, string value, string comment = "") {
            Key = key;
            Value = value;
            Comment = comment;
        }
    }

    public class IntMetaDataHeader : IGenericMetaDataHeader<int> {
        public string Key { get; }
        public int Value { get; }
        public string Comment { get; }

        public IntMetaDataHeader(string key, int value, string comment = "") {
            Key = key;
            Value = value;
            Comment = comment;
        }
    }

    public class DoubleMetaDataHeader : IGenericMetaDataHeader<double> {
        public string Key { get; }
        public double Value { get; }
        public string Comment { get; }

        public DoubleMetaDataHeader(string key, double value, string comment = "") {
            Key = key;
            Value = value;
            Comment = comment;
        }
    }
    public class BoolMetaDataHeader : IGenericMetaDataHeader<bool> {
        public string Key { get; }
        public bool Value { get; }
        public string Comment { get; }

        public BoolMetaDataHeader(string key, bool value, string comment = "") {
            Key = key;
            Value = value;
            Comment = comment;
        }
    }
    public class DateTimeMetaDataHeader : IGenericMetaDataHeader<DateTime> {
        public string Key { get; }
        public DateTime Value { get; }
        public string Comment { get; }

        public DateTimeMetaDataHeader(string key, DateTime value, string comment = "") {
            Key = key;
            Value = value;
            Comment = comment;
        }
    }

    public class ImageParameter {
        public int Id { get; set; } = -1;
        public DateTime ExposureStart { get; set; } = DateTime.MinValue;
        public DateTime ExposureMidPoint { get; set; } = DateTime.MinValue;
        public int ExposureNumber { get; set; } = -1;
        public string ImageType { get; set; } = string.Empty;
        public string Binning { get; set; } = string.Empty;
        public double ExposureTime { get; set; } = double.NaN;
        public RMS RecordedRMS { get; set; } = null;
    }

    public class CameraParameter {
        public string Name { get; set; } = string.Empty;
        public string Binning => $"{BinX}x{BinY}";
        public int BinX { get; set; } = 1;
        public int BinY { get; set; } = 1;
        public double PixelSize { get; set; } = double.NaN;
        public double Temperature { get; set; } = double.NaN;
        public int Gain { get; set; } = -1;
        public int Offset { get; set; } = -1;
        public double ElectronsPerADU { get; set; } = double.NaN;
        public double SetPoint { get; set; } = double.NaN;
        public short ReadoutModeIndex { get; set; } = 0;
        public string ReadoutModeName { get; set; } = string.Empty;
        public BayerPatternEnum BayerPattern = BayerPatternEnum.Auto;
        public SensorType SensorType { get; set; } = SensorType.Monochrome;
        public int BayerOffsetX { get; set; } = 0;
        public int BayerOffsetY { get; set; } = 0;
        public int USBLimit { get; set; } = -1;
    }

    public class TelescopeParameter {
        public string Name { get; set; } = string.Empty;
        public double FocalLength { get; set; } = double.NaN;
        public double FocalRatio { get; set; } = double.NaN;
        public double Altitude { get; set; } = double.NaN;
        public double Azimuth { get; set; } = double.NaN;
        public double Airmass { get; set; } = double.NaN;
        public PierSide SideOfPier { get; set; } = PierSide.pierUnknown;

        private Coordinates coordinates = null;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                if (value != null) {
                    value = value.Transform(Epoch.J2000);
                }
                coordinates = value;
            }
        }
    }

    public class FocuserParameter {
        public string Name { get; set; } = string.Empty;
        public int? Position { get; set; } = null;
        public double StepSize { get; set; } = double.NaN;
        public double Temperature { get; set; } = double.NaN;
    }

    public class RotatorParameter {
        public string Name { get; set; } = string.Empty;
        public double MechanicalPosition { get; set; } = double.NaN;
        public double Position { get; set; } = double.NaN;
        public double StepSize { get; set; } = double.NaN;
    }

    public class FilterWheelParameter {
        public string Name { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
    }

    public class TargetParameter {
        public string Name { get; set; } = string.Empty;
        [Obsolete("Use PositionAngle instead")]
        public double Rotation { get => AstroUtil.EuclidianModulus(360 - PositionAngle, 360); set => PositionAngle = AstroUtil.EuclidianModulus(360 - value, 360); }
        public double PositionAngle { get; set; } = double.NaN;
        private Coordinates coordinates = null;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                if (value != null) {
                    value = value.Transform(Epoch.J2000);
                }
                coordinates = value;
            }
        }
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

    public class SequenceParameter {
        public string Title { get; set; } = string.Empty;
    }
}