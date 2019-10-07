using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Model.MyWeatherData;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Astrometry;
using nom.tam.fits;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

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

        public void FromFITS(ImageHDU fitsImage) {
            // This method currently only prioritizes getting headers that would be useful for Framing
            // It also focuses on NINA-specific headers rather than handling other possible variants
            // TODO: Expand this for more completeness
            var header = fitsImage.Header;
            if (header.ContainsKey("XPIXSZ")) {
                Camera.PixelSize = header.GetDoubleValue("XPIXSZ");
            }
            if (header.ContainsKey("RA") && header.ContainsKey("DEC")) {
                var ra = header.GetDoubleValue("RA");
                var dec = header.GetDoubleValue("DEC");
                // Assume J2000 since that is the typical default. Regardless, this should be close enough
                // to help with solving
                Telescope.Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            }
            if (header.ContainsKey("OBJCTRA") && header.ContainsKey("OBJCTDEC")) {
                var ra = header.GetDoubleValue("OBJCTRA");
                var dec = header.GetDoubleValue("OBJCTDEC");
                Target.Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            }
            if (header.ContainsKey("FOCALLEN")) {
                Telescope.FocalLength = header.GetDoubleValue("FOCALLEN");
            }
            if (header.ContainsKey("FOCRATIO")) {
                Telescope.FocalRatio = header.GetDoubleValue("FOCRATIO");
            }
            if (header.ContainsKey("ROTATOR")) {
                Rotator.Position = header.GetDoubleValue("ROTATOR");
            }
        }

        public void FromXISF(XElement xisfImage) {
            IEnumerable<XElement> properties = xisfImage.Elements("Property");
            IEnumerable<XElement> fitsKeywords = xisfImage.Elements("FITSKeyword");
            var raDegrees = GetDoubleXisfProperty(properties, XISFImageProperty.Observation.Center.RA[0]);
            var decDegrees = GetDoubleXisfProperty(properties, XISFImageProperty.Observation.Center.Dec[0]);
            var objectRaDegrees = GetDoubleXisfProperty(properties, XISFImageProperty.Observation.Object.RA[0]);
            var objectDecDegrees = GetDoubleXisfProperty(properties, XISFImageProperty.Observation.Object.Dec[0]);
            var focalLength = GetDoubleXisfProperty(properties, XISFImageProperty.Instrument.Telescope.FocalLength[0]);
            var pixelSize = GetDoubleXisfProperty(properties, XISFImageProperty.Instrument.Sensor.XPixelSize[0]);
            var rotatorAng = GetDoubleXisfFITSKeyword(fitsKeywords, "ROTATOR");
            var focalRatio = GetDoubleXisfFITSKeyword(fitsKeywords, "FOCRATIO");
            if (raDegrees.HasValue && decDegrees.HasValue) {
                Telescope.Coordinates = new Coordinates(raDegrees.Value, decDegrees.Value, Epoch.J2000, Coordinates.RAType.Degrees);
            }
            if (objectRaDegrees.HasValue && objectDecDegrees.HasValue) {
                Target.Coordinates = new Coordinates(objectRaDegrees.Value, objectDecDegrees.Value, Epoch.J2000, Coordinates.RAType.Degrees);
            }
            if (focalLength.HasValue) {
                Telescope.FocalLength = focalLength.Value;
            }
            if (focalRatio.HasValue) {
                Telescope.FocalRatio = focalRatio.Value;
            }
            if (pixelSize.HasValue) {
                Camera.PixelSize = pixelSize.Value;
            }
            if (rotatorAng.HasValue) {
                Rotator.Position = rotatorAng.Value;
            }
        }

        private static double? GetDoubleXisfProperty(IEnumerable<XElement> properties, string id) {
            var propertyString = GetXisfProperty(properties, id);
            if (double.TryParse(propertyString, out double parsedProperty)) {
                return parsedProperty;
            }
            return null;
        }

        private static string GetXisfProperty(IEnumerable<XElement> properties, string id) {
            foreach (XElement property in properties) {
                var idProperty = property.Attribute("id");
                if (idProperty != null && id.Equals(idProperty.Value)) {
                    var valueProperty = property.Attribute("value");
                    if (valueProperty != null) {
                        return valueProperty.Value;
                    }
                    return property.Value;
                }
            }
            return null;
        }

        private static double? GetDoubleXisfFITSKeyword(IEnumerable<XElement> fitsKeywords, string name) {
            var propertyString = GetXisfFITSKeyword(fitsKeywords, name);
            if (double.TryParse(propertyString, out double parsedProperty)) {
                return parsedProperty;
            }
            return null;
        }

        private static string GetXisfFITSKeyword(IEnumerable<XElement> fitsKeywords, string name) {
            foreach (XElement keyword in fitsKeywords) {
                var nameProperty = keyword.Attribute("name");
                if (nameProperty != null && name.Equals(nameProperty.Value)) {
                    var valueProperty = keyword.Attribute("value");
                    if (valueProperty != null) {
                        return valueProperty.Value;
                    }
                }
            }
            return null;
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
        public double Gain { get; set; } = double.NaN;
        public double Offset { get; set; } = double.NaN;
        public double ElectronsPerADU { get; set; } = double.NaN;
        public double SetPoint { get; set; } = double.NaN;
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
        public string Name { get; set; } = string.Empty;
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