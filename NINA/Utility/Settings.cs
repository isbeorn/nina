using NINA.Model;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace NINA.Utility {
    static class Settings  {
        
        static Settings() {
            ColorSchemas = ColorSchemas.ReadColorSchemas();            
        }

        public static FileTypeEnum FileType {
            get {
                return (FileTypeEnum)Properties.Settings.Default.FileType;
            } set {
                Properties.Settings.Default.FileType = (int)value;
                Properties.Settings.Default.Save();
            }

        }

        public static string AstrometryAPIKey {
            get {                
                return Properties.Settings.Default.AstrometryAPIKey;
            }
            set {
                Properties.Settings.Default.AstrometryAPIKey = value;
                Properties.Settings.Default.Save();
            }

        }

        public static string CygwinLocation {
            get {
                return Environment.ExpandEnvironmentVariables(Properties.Settings.Default.CygwinLocation);
            }
            set {
                Properties.Settings.Default.CygwinLocation = value;
                Properties.Settings.Default.Save();
            }

        }

        public static PlateSolverEnum PlateSolverType {
            get {
                return (PlateSolverEnum)Properties.Settings.Default.PlateSolverType;
            }
            set {
                Properties.Settings.Default.PlateSolverType = (int)value;
                Properties.Settings.Default.Save();
            }
        }

        public static WeatherDataEnum WeatherDataType {
            get {
                return (WeatherDataEnum)Properties.Settings.Default.WeatherDataType;
            }
            set {
                Properties.Settings.Default.WeatherDataType = (int)value;
                Properties.Settings.Default.Save();
            }
        }

        public static string OpenWeatherMapAPIKey {
            get {
                return Properties.Settings.Default.OpenWeatherMapAPIKey;
            }
            set {
                Properties.Settings.Default.OpenWeatherMapAPIKey = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string OpenWeatherMapUrl {
            get {
                return Properties.Settings.Default.OpenWeatherMapUrl;
            }
            set {
                Properties.Settings.Default.OpenWeatherMapUrl = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string OpenWeatherMapLocation {
            get {
                return Properties.Settings.Default.OpenWeatherMapLocation;
            }
            set {
                Properties.Settings.Default.OpenWeatherMapLocation = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Epoch EpochType {
            get {
                return (Epoch)Properties.Settings.Default.EpochType;
            }
            set {
                Properties.Settings.Default.EpochType = (int)value;
                Properties.Settings.Default.Save();
            }
        }

        public static Hemisphere HemisphereType {
            get {
                return (Hemisphere)Properties.Settings.Default.HemisphereType;
            }
            set {
                Properties.Settings.Default.HemisphereType = (int)value;
                Properties.Settings.Default.Save();
            }
        }

        public static int AnsvrFocalLength {
            get {
                return Properties.Settings.Default.AnsvrFocalLength;
            }
            set {
                Properties.Settings.Default.AnsvrFocalLength = value;
                Properties.Settings.Default.Save();
            }
        }

        public static double AnsvrPixelSize {
            get {
                return Properties.Settings.Default.AnsvrPixelSize;
            }
            set {
                Properties.Settings.Default.AnsvrPixelSize = value;
                Properties.Settings.Default.Save();
            }
        }

        public static double AnsvrSearchRadius {
            get {
                return Properties.Settings.Default.AnsvrSearchRadius;
            }
            set {
                Properties.Settings.Default.AnsvrSearchRadius = value;
                Properties.Settings.Default.Save();
            }
        }


        public static bool UseFullResolutionPlateSolve {
            get {
                return Properties.Settings.Default.UseFullResolutionPlateSolve;
            }
            set {
                Properties.Settings.Default.UseFullResolutionPlateSolve = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string CameraId {
            get {
                return Properties.Settings.Default.CameraId;
            }
            set {
                Properties.Settings.Default.CameraId = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string FocuserId {
            get {
                return Properties.Settings.Default.FocuserId;
            }
            set {
                Properties.Settings.Default.FocuserId = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string TelescopeId {
            get {
                return Properties.Settings.Default.TelescopeId;
            }
            set {
                Properties.Settings.Default.TelescopeId = value;
                Properties.Settings.Default.Save();
            }
        }

        public static int LogLevel {
            get {
                return Properties.Settings.Default.LogLevel;
            }
            set {
                Properties.Settings.Default.LogLevel = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string FilterWheelId {
            get {
                return Properties.Settings.Default.FilterWheelId;
            }
            set {
                Properties.Settings.Default.FilterWheelId = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string ImageFilePath {
            get {
                return Properties.Settings.Default.ImageFilePath;
            }
            set {
                Properties.Settings.Default.ImageFilePath = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string ImageFilePattern {
            get {
                return Properties.Settings.Default.ImageFilePattern;
            }
            set {
                Properties.Settings.Default.ImageFilePattern = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string PHD2ServerUrl {
            get {
                return Properties.Settings.Default.PHD2ServerUrl;
            }
            set {
                Properties.Settings.Default.PHD2ServerUrl = value;
                Properties.Settings.Default.Save();
            }
        }
        public static int PHD2ServerPort {
            get {
                return Properties.Settings.Default.PHD2ServerPort;
            }
            set {
                Properties.Settings.Default.PHD2ServerPort = value;
                Properties.Settings.Default.Save();
            }
        }

        public static double DitherPixels {
            get {
                return Properties.Settings.Default.DitherPixels;
            }
            set {
                Properties.Settings.Default.DitherPixels = value;
                Properties.Settings.Default.Save();
            }
        }

        public static bool DitherRAOnly {
            get {
                return Properties.Settings.Default.DitherRAOnly;
            }
            set {
                Properties.Settings.Default.DitherRAOnly = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string ColorSchemaName
        {
            get {
                return Properties.Settings.Default.ColorSchemaType;
            }
            set {                
                Properties.Settings.Default.ColorSchemaType = value;
                ColorSchema = ColorSchemas.Items.Where(x => x.Name == value).FirstOrDefault();
                Properties.Settings.Default.Save();
            }
        }

        public static string AltColorSchemaName {
            get {
                return Properties.Settings.Default.AlternativeColorSchemaType;
            }
            set {
                Properties.Settings.Default.AlternativeColorSchemaType = value;
                AlternativeColorSchema = ColorSchemas.Items.Where(x => x.Name == value).FirstOrDefault();                
                Properties.Settings.Default.Save();
            }
        }

        public static ColorSchemas ColorSchemas { get; set; }

        private static ColorSchema _colorSchema;
        public static ColorSchema ColorSchema {
            get {
                if(_colorSchema == null) {
                    _colorSchema = ColorSchemas.Items.Where(x => x.Name == ColorSchemaName).FirstOrDefault();
                    if(_colorSchema == null) {
                        _colorSchema = ColorSchemas.CreateDefaultSchema();
                    }
                }
                return _colorSchema;
            }
            set {
                _colorSchema = value;                
            }
        }

        private static ColorSchema _alternativeColorSchema;
        public static ColorSchema AlternativeColorSchema {
            get {

                if (_alternativeColorSchema == null) {
                    _alternativeColorSchema = ColorSchemas.Items.Where(x => x.Name == AltColorSchemaName).FirstOrDefault();
                    if (_alternativeColorSchema == null) {
                        _alternativeColorSchema = ColorSchemas.CreateDefaultAltSchema();
                    }
                }
                return _alternativeColorSchema;
            }
            set {
                _alternativeColorSchema = value;
            }
        }


        public static Color PrimaryColor {
            get {                
                return ColorSchema.PrimaryColor;
            }
            set {                
                if(ColorSchemaName == "Custom") {
                    ColorSchema.PrimaryColor = value;
                    Properties.Settings.Default.PrimaryColor = value;
                    Properties.Settings.Default.Save();
                }                
            }
        }

        public static Color SecondaryColor {
            get {
                return ColorSchema.SecondaryColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.SecondaryColor = value;
                    Properties.Settings.Default.SecondaryColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static Color BorderColor {
            get {
                return ColorSchema.BorderColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.BorderColor = value;
                    Properties.Settings.Default.BorderColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static Color BackgroundColor {
            get {
                return ColorSchema.BackgroundColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.BackgroundColor = value;
                    Properties.Settings.Default.BackgroundColor = value;
                    Properties.Settings.Default.Save();
                }

            }
        }

        public static Color ButtonBackgroundColor {
            get {
                return ColorSchema.ButtonBackgroundColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.ButtonBackgroundColor = value;
                    Properties.Settings.Default.ButtonBackgroundColor = value;
                    Properties.Settings.Default.Save();
                }

            }
        }

        public static Color ButtonBackgroundSelectedColor {
            get {
                return ColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.ButtonBackgroundSelectedColor = value;
                    Properties.Settings.Default.ButtonBackgroundSelectedColor = value;
                    Properties.Settings.Default.Save();
                }

            }
        }

        public static Color ButtonForegroundDisabledColor {
            get {
                return ColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.ButtonForegroundDisabledColor = value;
                    Properties.Settings.Default.ButtonForegroundDisabledColor = value;
                    Properties.Settings.Default.Save();
                }

            }
        }

        public static Color ButtonForegroundColor {
            get {
                return ColorSchema.ButtonForegroundColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.ButtonForegroundColor = value;
                    Properties.Settings.Default.ButtonForegroundColor = value;
                    Properties.Settings.Default.Save();
                }

            }
        }


        public static Color AltPrimaryColor {
            get {
                return AlternativeColorSchema.PrimaryColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AlternativeColorSchema.PrimaryColor = value;
                    Properties.Settings.Default.AltPrimaryColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static Color AltSecondaryColor {
            get {
                return AlternativeColorSchema.SecondaryColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AlternativeColorSchema.SecondaryColor = value;
                    Properties.Settings.Default.AltSecondaryColor = value;
                    Properties.Settings.Default.Save();
                }               
            }
        }

        public static Color AltBorderColor {
            get {
                return AlternativeColorSchema.BorderColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AlternativeColorSchema.BorderColor = value;
                    Properties.Settings.Default.AltBorderColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static Color AltBackgroundColor {
            get {
                return AlternativeColorSchema.BackgroundColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AlternativeColorSchema.BackgroundColor = value;
                    Properties.Settings.Default.AltBackgroundColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static Color AltButtonBackgroundColor {
            get {
                return AlternativeColorSchema.ButtonBackgroundColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AlternativeColorSchema.ButtonBackgroundColor = value;
                    Properties.Settings.Default.AltButtonBackgroundColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static Color AltButtonBackgroundSelectedColor {
            get {
                return AlternativeColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AlternativeColorSchema.ButtonBackgroundSelectedColor = value;
                    Properties.Settings.Default.AltButtonBackgroundSelectedColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static Color AltButtonForegroundDisabledColor {
            get {
                return AlternativeColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AlternativeColorSchema.ButtonForegroundDisabledColor = value;
                    Properties.Settings.Default.AltButtonForegroundDisabledColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static Color AltButtonForegroundColor {
            get {
                return AlternativeColorSchema.ButtonForegroundColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AlternativeColorSchema.ButtonForegroundColor = value;
                    Properties.Settings.Default.AltButtonForegroundColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static bool AutoMeridianFlip {
            get {
                return Properties.Settings.Default.AutoMeridianFlip;
            }
            set {
                Properties.Settings.Default.AutoMeridianFlip = value;
                Properties.Settings.Default.Save();
            }
        }

        public static double MinutesAfterMeridian {
            get {
                return Properties.Settings.Default.MinutesAfterMeridian;
            }
            set {
                Properties.Settings.Default.MinutesAfterMeridian = value;
                Properties.Settings.Default.Save();
            }
        }

        public static bool RecenterAfterFlip {
            get {
                return Properties.Settings.Default.RecenterAfterFlip;
            }
            set {
                Properties.Settings.Default.RecenterAfterFlip = value;
                Properties.Settings.Default.Save();
            }
        }


    }

}
