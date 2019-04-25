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

using NINA.Utility;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class ColorSchemaSettings : Settings, IColorSchemaSettings {

        public ColorSchemaSettings() : base() {
            Initialize();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        private void SetValuesOnDeserialized(StreamingContext context) {
            Initialize();
        }

        protected override void SetDefaultValues() {
            colorSchemaName = "Light";
            altColorSchemaName = "Dark";
            primaryColor = (Color)ColorConverter.ConvertFromString("#FF000000");
            secondaryColor = (Color)ColorConverter.ConvertFromString("#FF54748C");
            borderColor = (Color)ColorConverter.ConvertFromString("#AABCBCBC");
            backgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
            secondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0d3956");
            tertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0d3956");
            buttonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0B3C5D");
            buttonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF2190DB");
            buttonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FFc5d2db");
            buttonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
            notificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");
            notificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFDB0606");
            notificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
            notificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

            altPrimaryColor = (Color)ColorConverter.ConvertFromString("#FF550C18");
            altSecondaryColor = (Color)ColorConverter.ConvertFromString("#FF2d060d");
            altSecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
            altTertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
            altBorderColor = (Color)ColorConverter.ConvertFromString("#FF550C18");
            altBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A");
            altButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF550C18");
            altButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF96031A");
            altButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF443730");
            altButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A");
            altNotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");
            altNotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");
            altNotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A");
            altNotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A");
        }

        private void Initialize() {
            ColorSchemas = ColorSchemas.ReadColorSchemas();
            ColorSchemas.Items.Add(new ColorSchema {
                Name = (ColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") ? "Alternative Custom" : "Custom",
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                BorderColor = borderColor,
                BackgroundColor = backgroundColor,
                SecondaryBackgroundColor = secondaryBackgroundColor,
                TertiaryBackgroundColor = tertiaryBackgroundColor,
                ButtonBackgroundColor = buttonBackgroundColor,
                ButtonBackgroundSelectedColor = buttonBackgroundSelectedColor,
                ButtonForegroundColor = buttonForegroundColor,
                ButtonForegroundDisabledColor = buttonForegroundDisabledColor,
                NotificationWarningColor = notificationWarningColor,
                NotificationErrorColor = notificationErrorColor,
                NotificationWarningTextColor = notificationWarningTextColor,
                NotificationErrorTextColor = notificationErrorTextColor
            });

            ColorSchemas.Items.Add(new ColorSchema {
                Name = AltColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom" ? "Custom" : "Alternative Custom",
                PrimaryColor = altPrimaryColor,
                SecondaryColor = altSecondaryColor,
                BorderColor = altBorderColor,
                BackgroundColor = altBackgroundColor,
                SecondaryBackgroundColor = altSecondaryBackgroundColor,
                TertiaryBackgroundColor = altTertiaryBackgroundColor,
                ButtonBackgroundColor = altButtonBackgroundColor,
                ButtonBackgroundSelectedColor = altButtonBackgroundSelectedColor,
                ButtonForegroundColor = altButtonForegroundColor,
                ButtonForegroundDisabledColor = altButtonForegroundDisabledColor,
                NotificationWarningColor = altNotificationWarningColor,
                NotificationErrorColor = altNotificationErrorColor,
                NotificationWarningTextColor = altNotificationWarningTextColor,
                NotificationErrorTextColor = altNotificationErrorTextColor
            });
            ColorSchemas = ColorSchemas;

            ColorSchema = ColorSchemas.Items.Where(x => x.Name == ColorSchemaName).FirstOrDefault();
            if (ColorSchema == null) {
                ColorSchema = ColorSchemas.CreateDefaultSchema();
            }

            AltColorSchema = ColorSchemas.Items.Where(x => x.Name == AltColorSchemaName).FirstOrDefault();
            if (AltColorSchema == null) {
                AltColorSchema = ColorSchemas.CreateDefaultAltSchema();
            }
        }

        public ColorSchemas ColorSchemas { get; set; }

        [DataMember]
        public ColorSchema ColorSchema { get; set; }

        [DataMember]
        public ColorSchema AltColorSchema { get; set; }

        private string colorSchemaName;

        [DataMember]
        public string ColorSchemaName {
            get {
                return colorSchemaName;
            }
            set {
                if (colorSchemaName != value) {
                    colorSchemaName = value;
                    ColorSchema = ColorSchemas?.Items.Where(x => x.Name == value).FirstOrDefault();
                    RaisePropertyChanged();
                }
            }
        }

        private string altColorSchemaName;

        [DataMember]
        public string AltColorSchemaName {
            get {
                return altColorSchemaName;
            }
            set {
                if (altColorSchemaName != value) {
                    altColorSchemaName = value;
                    AltColorSchema = ColorSchemas?.Items.Where(x => x.Name == value).FirstOrDefault();
                    RaisePropertyChanged();
                }
            }
        }

        private Color primaryColor;

        [DataMember]
        public Color PrimaryColor {
            get {
                return ColorSchema.PrimaryColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.PrimaryColor = value;
                    primaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color secondaryColor;

        [DataMember]
        public Color SecondaryColor {
            get {
                return ColorSchema.SecondaryColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.SecondaryColor = value;
                    secondaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color borderColor;

        [DataMember]
        public Color BorderColor {
            get {
                return ColorSchema.BorderColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.BorderColor = value;
                    borderColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color backgroundColor;

        [DataMember]
        public Color BackgroundColor {
            get {
                return ColorSchema.BackgroundColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.BackgroundColor = value;
                    backgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color secondaryBackgroundColor;

        [DataMember]
        public Color SecondaryBackgroundColor {
            get {
                return ColorSchema.SecondaryBackgroundColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.SecondaryBackgroundColor = value;
                    secondaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color tertiaryBackgroundColor;

        [DataMember]
        public Color TertiaryBackgroundColor {
            get {
                return ColorSchema.TertiaryBackgroundColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.TertiaryBackgroundColor = value;
                    tertiaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color buttonBackgroundColor;

        [DataMember]
        public Color ButtonBackgroundColor {
            get {
                return ColorSchema.ButtonBackgroundColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.ButtonBackgroundColor = value;
                    buttonBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color buttonBackgroundSelectedColor;

        [DataMember]
        public Color ButtonBackgroundSelectedColor {
            get {
                return ColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.ButtonBackgroundSelectedColor = value;
                    buttonBackgroundSelectedColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color buttonForegroundDisabledColor;

        [DataMember]
        public Color ButtonForegroundDisabledColor {
            get {
                return ColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.ButtonForegroundDisabledColor = value;
                    buttonForegroundDisabledColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color buttonForegroundColor;

        [DataMember]
        public Color ButtonForegroundColor {
            get {
                return ColorSchema.ButtonForegroundColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.ButtonForegroundColor = value;
                    buttonForegroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color notificationWarningColor;

        [DataMember]
        public Color NotificationWarningColor {
            get {
                return ColorSchema.NotificationWarningColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.NotificationWarningColor = value;
                    notificationWarningColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color notificationErrorColor;

        [DataMember]
        public Color NotificationErrorColor {
            get {
                return ColorSchema.NotificationErrorColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.NotificationErrorColor = value;
                    notificationErrorColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color notificationWarningTextColor;

        [DataMember]
        public Color NotificationWarningTextColor {
            get {
                return ColorSchema.NotificationWarningTextColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.NotificationWarningTextColor = value;
                    notificationWarningTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color notificationErrorTextColor;

        [DataMember]
        public Color NotificationErrorTextColor {
            get {
                return ColorSchema.NotificationErrorTextColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.NotificationErrorTextColor = value;
                    notificationErrorTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altPrimaryColor;

        [DataMember]
        public Color AltPrimaryColor {
            get {
                return AltColorSchema.PrimaryColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.PrimaryColor = value;
                    altPrimaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altSecondaryColor;

        [DataMember]
        public Color AltSecondaryColor {
            get {
                return AltColorSchema.SecondaryColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.SecondaryColor = value;
                    altSecondaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altSecondaryBackgroundColor;

        [DataMember]
        public Color AltSecondaryBackgroundColor {
            get {
                return AltColorSchema.SecondaryBackgroundColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.SecondaryBackgroundColor = value;
                    altSecondaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altTertiaryBackgroundColor;

        [DataMember]
        public Color AltTertiaryBackgroundColor {
            get {
                return AltColorSchema.TertiaryBackgroundColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.TertiaryBackgroundColor = value;
                    altTertiaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altBorderColor;

        [DataMember]
        public Color AltBorderColor {
            get {
                return AltColorSchema.BorderColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.BorderColor = value;
                    altBorderColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altBackgroundColor;

        [DataMember]
        public Color AltBackgroundColor {
            get {
                return AltColorSchema.BackgroundColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.BackgroundColor = value;
                    altBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altButtonBackgroundColor;

        [DataMember]
        public Color AltButtonBackgroundColor {
            get {
                return AltColorSchema.ButtonBackgroundColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.ButtonBackgroundColor = value;
                    altButtonBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altButtonBackgroundSelectedColor;

        [DataMember]
        public Color AltButtonBackgroundSelectedColor {
            get {
                return AltColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.ButtonBackgroundSelectedColor = value;
                    altButtonBackgroundSelectedColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altButtonForegroundDisabledColor;

        [DataMember]
        public Color AltButtonForegroundDisabledColor {
            get {
                return AltColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.ButtonForegroundDisabledColor = value;
                    altButtonForegroundDisabledColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altButtonForegroundColor;

        [DataMember]
        public Color AltButtonForegroundColor {
            get {
                return AltColorSchema.ButtonForegroundColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.ButtonForegroundColor = value;
                    altButtonForegroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altNotificationWarningColor;

        [DataMember]
        public Color AltNotificationWarningColor {
            get {
                return AltColorSchema.NotificationWarningColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.NotificationWarningColor = value;
                    altNotificationWarningColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altNotificationErrorColor;

        [DataMember]
        public Color AltNotificationErrorColor {
            get {
                return AltColorSchema.NotificationErrorColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.NotificationErrorColor = value;
                    altNotificationErrorColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altNotificationWarningTextColor;

        [DataMember]
        public Color AltNotificationWarningTextColor {
            get {
                return AltColorSchema.NotificationWarningTextColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.NotificationWarningTextColor = value;
                    altNotificationWarningTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Color altNotificationErrorTextColor;

        [DataMember]
        public Color AltNotificationErrorTextColor {
            get {
                return AltColorSchema.NotificationErrorTextColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.NotificationErrorTextColor = value;
                    altNotificationErrorTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}