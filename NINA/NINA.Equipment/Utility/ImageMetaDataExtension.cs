#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using NINA.Core.Enum;
using System.Text;
using System.Threading.Tasks;
using NINA.Image.ImageData;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Utility {

    public static class ImageMetaDataExtension {

        public static void FromCamera(this ImageMetaData data, ICamera info) {
            if (info.Connected) {
                data.Camera.Id = info.Id;
                data.Camera.Name = info.Name;
                try { data.Camera.Temperature = info.Temperature; } catch { data.Camera.Temperature = double.NaN; }
                data.Camera.Gain = -1;
                if(info.CanGetGain) {
                    try { data.Camera.Gain = info.Gain; } catch { data.Camera.Gain = -1; }
                }
                try { data.Camera.Offset = info.Offset; } catch { data.Camera.Offset = -1; }
                try { data.Camera.SetPoint = info.TemperatureSetPoint; } catch { data.Camera.SetPoint = double.NaN; }
                data.Camera.BinX = info.BinX;
                data.Camera.BinY = info.BinY;
                data.Camera.ElectronsPerADU = info.ElectronsPerADU;
                data.Camera.PixelSize = info.PixelSizeX;
                data.Camera.USBLimit = info.USBLimit;

                if (info.ReadoutModes?.Count > 1) {
                    data.Camera.ReadoutModeIndex = info.ReadoutMode;
                    data.Camera.ReadoutModeName = info.ReadoutModes.ToArray()[info.ReadoutMode];
                }

                data.Camera.SensorType = info.SensorType;

                if (data.Camera.SensorType != SensorType.Monochrome) {
                    if (data.Camera.BayerPattern == BayerPatternEnum.None) { // Treat it like a monochrome camera (for mono-bin modes on OSCs)
                        data.Camera.SensorType = SensorType.Monochrome;
                    } else if (data.Camera.BayerPattern == BayerPatternEnum.Auto) { // Take the sensor type from the camera driver
                        data.Camera.SensorType = info.SensorType;
                        data.Camera.BayerOffsetX = info.BayerOffsetX;
                        data.Camera.BayerOffsetY = info.BayerOffsetY;
                    } else { // Take the sensor type from the user-configured bayer pattern
                        data.Camera.SensorType = (SensorType)data.Camera.BayerPattern;
                        data.Camera.BayerOffsetX = 0;
                        data.Camera.BayerOffsetY = 0;
                    }
                }
            }
        }

        public static void FromCameraInfo(this ImageMetaData data, CameraInfo info) {
            if (info.Connected) {
                data.Camera.Id = info.DeviceId;
                data.Camera.Name = info.Name;
                data.Camera.Temperature = info.Temperature;
                data.Camera.Gain = info.Gain;
                data.Camera.Offset = info.Offset;
                data.Camera.SetPoint = info.TemperatureSetPoint;
                data.Camera.BinX = info.BinX;
                data.Camera.BinY = info.BinY;
                data.Camera.ElectronsPerADU = info.ElectronsPerADU;
                data.Camera.PixelSize = info.PixelSize;
                data.Camera.USBLimit = info.USBLimit;

                if (info.ReadoutModes.Count() > 1) {
                    data.Camera.ReadoutModeIndex = info.ReadoutMode;
                    data.Camera.ReadoutModeName = info.ReadoutModes.ToArray()[info.ReadoutMode];
                }

                data.Camera.SensorType = info.SensorType;

                if (data.Camera.SensorType != SensorType.Monochrome) {
                    if (data.Camera.BayerPattern == BayerPatternEnum.None) { // Treat it like a monochrome camera (for mono-bin modes on OSCs)
                        data.Camera.SensorType = SensorType.Monochrome;
                    } else if (data.Camera.BayerPattern == BayerPatternEnum.Auto) { // Take the sensor type from the camera driver
                        data.Camera.SensorType = info.SensorType;
                        data.Camera.BayerOffsetX = info.BayerOffsetX;
                        data.Camera.BayerOffsetY = info.BayerOffsetY;
                    } else { // Take the sensor type from the user-configured bayer pattern
                        data.Camera.SensorType = (SensorType)data.Camera.BayerPattern;
                        data.Camera.BayerOffsetX = 0;
                        data.Camera.BayerOffsetY = 0;
                    }
                }
            }
        }

        public static void FromTelescopeInfo(this ImageMetaData data, TelescopeInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(data.Telescope.Name)) {
                    data.Telescope.Name = info.Name;
                }
                data.Telescope.Coordinates = info.Coordinates;
                data.Telescope.Altitude = info.Altitude;
                data.Telescope.Azimuth = info.Azimuth;
                data.Telescope.Airmass = Astrometry.AstroUtil.Airmass(info.Altitude);
                data.Telescope.SideOfPier = info.SideOfPier;
            }
        }

        public static void FromFilterWheelInfo(this ImageMetaData data, FilterWheelInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(data.FilterWheel.Name)) {
                    data.FilterWheel.Name = info.Name;
                }
                data.FilterWheel.Filter = info.SelectedFilter?.Name ?? string.Empty;
            }
        }

        public static void FromFocuserInfo(this ImageMetaData data, FocuserInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(data.Focuser.Name)) {
                    data.Focuser.Name = info.Name;
                }
                data.Focuser.Position = info.Position;
                data.Focuser.StepSize = info.StepSize;
                data.Focuser.Temperature = info.Temperature;
            }
        }

        public static void FromRotatorInfo(this ImageMetaData data, RotatorInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(data.Rotator.Name)) {
                    data.Rotator.Name = info.Name;
                }
                data.Rotator.MechanicalPosition = info.MechanicalPosition;
                data.Rotator.Position = info.Position;
                data.Rotator.StepSize = info.StepSize;
            }
        }

        public static void FromWeatherDataInfo(this ImageMetaData data, WeatherDataInfo info) {
            if (info.Connected) {
                data.WeatherData.CloudCover = info.CloudCover;
                data.WeatherData.DewPoint = info.DewPoint;
                data.WeatherData.Humidity = info.Humidity;
                data.WeatherData.Pressure = info.Pressure;
                data.WeatherData.SkyBrightness = info.SkyBrightness;
                data.WeatherData.SkyQuality = info.SkyQuality;
                data.WeatherData.SkyTemperature = info.SkyTemperature;
                data.WeatherData.StarFWHM = info.StarFWHM;
                data.WeatherData.Temperature = info.Temperature;
                data.WeatherData.WindDirection = info.WindDirection;
                data.WeatherData.WindGust = info.WindGust;
                data.WeatherData.WindSpeed = info.WindSpeed;
            }
        }
    }
}