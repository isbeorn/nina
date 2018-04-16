using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(ColorSchemaSettings))]
    public class ColorSchemaSettings {
        public ColorSchemaSettings() {
            ColorSchemas = ColorSchemas.ReadColorSchemas();
            ColorSchemas.Items.Add(new ColorSchema {
                Name = "Custom",
                PrimaryColor = PrimaryColor,
                SecondaryColor = SecondaryColor,
                BorderColor = BorderColor,
                BackgroundColor = BackgroundColor,
                ButtonBackgroundColor = ButtonBackgroundColor,
                ButtonBackgroundSelectedColor = ButtonBackgroundSelectedColor,
                ButtonForegroundColor = ButtonForegroundColor,
                ButtonForegroundDisabledColor = ButtonForegroundDisabledColor,
                NotificationWarningColor = NotificationWarningColor,
                NotificationErrorColor = NotificationErrorColor
            });

            ColorSchemas.Items.Add(new ColorSchema {
                Name = "Alternative Custom",
                PrimaryColor = AltPrimaryColor,
                SecondaryColor = AltSecondaryColor,
                BorderColor = AltBorderColor,
                BackgroundColor = AltBackgroundColor,
                ButtonBackgroundColor = AltButtonBackgroundColor,
                ButtonBackgroundSelectedColor = AltButtonBackgroundSelectedColor,
                ButtonForegroundColor = AltButtonForegroundColor,
                ButtonForegroundDisabledColor = AltButtonForegroundDisabledColor,
                NotificationWarningColor = AltNotificationWarningColor,
                NotificationErrorColor = AltNotificationErrorColor
            });
        }

        [XmlIgnore]
        public ColorSchemas ColorSchemas { get; set; }

        private ColorSchema colorSchema;
        [XmlElement(nameof(ColorSchema))]
        public ColorSchema ColorSchema {
            get {
                if (colorSchema == null) {
                    colorSchema = ColorSchemas.Items.Where(x => x.Name == ColorSchemaName).FirstOrDefault();
                    if (colorSchema == null) {
                        colorSchema = ColorSchemas.CreateDefaultSchema();
                    }
                }
                return colorSchema;
            }
            set {
                colorSchema = value;
            }
        }

        private ColorSchema altColorSchema;
        [XmlElement(nameof(AltColorSchema))]
        public ColorSchema AltColorSchema {
            get {
                if (altColorSchema == null) {
                    altColorSchema = ColorSchemas.Items.Where(x => x.Name == AltColorSchemaName).FirstOrDefault();
                    if (altColorSchema == null) {
                        altColorSchema = ColorSchemas.CreateDefaultAltSchema();
                    }
                }
                return altColorSchema;
            }
            set {
                altColorSchema = value;
            }
        }

        private string colorSchemaName = "Light";
        [XmlElement(nameof(ColorSchemaName))]
        public string ColorSchemaName {
            get {
                return colorSchemaName;
            }
            set {
                colorSchemaName = value;
                ColorSchema = ColorSchemas.Items.Where(x => x.Name == value).FirstOrDefault();
            }
        }

        private string altColorSchemaName = "Dark";
        [XmlElement(nameof(AltColorSchemaName))]
        public string AltColorSchemaName {
            get {
                return altColorSchemaName;
            }
            set {
                altColorSchemaName = value;
                AltColorSchema = ColorSchemas.Items.Where(x => x.Name == value).FirstOrDefault();
            }
        }

        private Color primaryColor = (Color)ColorConverter.ConvertFromString("#FF000000");
        [XmlElement(nameof(PrimaryColor))]
        public Color PrimaryColor {
            get {
                return ColorSchema.PrimaryColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.PrimaryColor = value;
                    primaryColor = value;
                }
            }
        }

        private Color secondaryColor = (Color)ColorConverter.ConvertFromString("#FF54748C");
        [XmlElement(nameof(SecondaryColor))]
        public Color SecondaryColor {
            get {
                return ColorSchema.SecondaryColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.SecondaryColor = value;
                    secondaryColor = value;
                }
            }
        }

        private Color borderColor = (Color)ColorConverter.ConvertFromString("#AABCBCBC");
        [XmlElement(nameof(BorderColor))]
        public Color BorderColor {
            get {
                return ColorSchema.BorderColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.BorderColor = value;
                    borderColor = value;
                }
            }
        }

        private Color backgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
        [XmlElement(nameof(BackgroundColor))]
        public Color BackgroundColor {
            get {
                return ColorSchema.BackgroundColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.BackgroundColor = value;
                    backgroundColor = value;
                }

            }
        }

        private Color buttonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0B3C5D");
        [XmlElement(nameof(ButtonBackgroundColor))]
        public Color ButtonBackgroundColor {
            get {
                return ColorSchema.ButtonBackgroundColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.ButtonBackgroundColor = value;
                    buttonBackgroundColor = value;
                }

            }
        }

        private Color buttonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF2190DB");
        [XmlElement(nameof(ButtonBackgroundSelectedColor))]
        public Color ButtonBackgroundSelectedColor {
            get {
                return ColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.ButtonBackgroundSelectedColor = value;
                    buttonBackgroundSelectedColor = value;
                }

            }
        }

        private Color buttonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF1D2731");
        [XmlElement(nameof(ButtonForegroundDisabledColor))]
        public Color ButtonForegroundDisabledColor {
            get {
                return ColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.ButtonForegroundDisabledColor = value;
                    buttonForegroundDisabledColor = value;
                }

            }
        }

        private Color buttonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");
        [XmlElement(nameof(ButtonForegroundColor))]
        public Color ButtonForegroundColor {
            get {
                return ColorSchema.ButtonForegroundColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.ButtonForegroundColor = value;
                    buttonForegroundColor = value;
                }

            }
        }

        private Color altPrimaryColor = (Color)ColorConverter.ConvertFromString("#FF550C18");
        [XmlElement(nameof(AltPrimaryColor))]
        public Color AltPrimaryColor {
            get {
                return AltColorSchema.PrimaryColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.PrimaryColor = value;
                    altPrimaryColor = value;
                }
            }
        }

        private Color altSecondaryColor = (Color)ColorConverter.ConvertFromString("#FF1B2A41");
        [XmlElement(nameof(AltSecondaryColor))]
        public Color AltSecondaryColor {
            get {
                return AltColorSchema.SecondaryColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.SecondaryColor = value;
                    altSecondaryColor = value;
                }
            }
        }

        private Color altBorderColor = (Color)ColorConverter.ConvertFromString("#FF550C18");
        [XmlElement(nameof(AltBorderColor))]
        public Color AltBorderColor {
            get {
                return AltColorSchema.BorderColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.BorderColor = value;
                    altBorderColor = value;
                }
            }
        }

        private Color altBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A");
        [XmlElement(nameof(AltBackgroundColor))]
        public Color AltBackgroundColor {
            get {
                return AltColorSchema.BackgroundColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.BackgroundColor = value;
                    altBackgroundColor = value;
                }
            }
        }

        private Color altButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF550C18");
        [XmlElement(nameof(AltButtonBackgroundColor))]
        public Color AltButtonBackgroundColor {
            get {
                return AltColorSchema.ButtonBackgroundColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.ButtonBackgroundColor = value;
                    altButtonBackgroundColor = value;
                }
            }
        }

        private Color altButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF96031A");
        [XmlElement(nameof(AltButtonBackgroundSelectedColor))]
        public Color AltButtonBackgroundSelectedColor {
            get {
                return AltColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.ButtonBackgroundSelectedColor = value;
                    altButtonBackgroundSelectedColor = value;
                }
            }
        }

        private Color altButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF443730");
        [XmlElement(nameof(AltButtonForegroundDisabledColor))]
        public Color AltButtonForegroundDisabledColor {
            get {
                return AltColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.ButtonForegroundDisabledColor = value;
                    altButtonForegroundDisabledColor = value;
                }
            }
        }

        private Color altButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A");
        [XmlElement(nameof(AltButtonForegroundColor))]
        public Color AltButtonForegroundColor {
            get {
                return AltColorSchema.ButtonForegroundColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.ButtonForegroundColor = value;
                    altButtonForegroundColor = value;
                }
            }
        }

        private Color notificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");
        [XmlElement(nameof(NotificationWarningColor))]
        public Color NotificationWarningColor {
            get {
                return ColorSchema.NotificationWarningColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.NotificationWarningColor = value;
                    notificationWarningColor = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private Color notificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFDB0606");
        [XmlElement(nameof(NotificationErrorColor))]
        public Color NotificationErrorColor {
            get {
                return ColorSchema.NotificationErrorColor;
            }
            set {
                if (ColorSchemaName == "Custom") {
                    ColorSchema.NotificationErrorColor = value;
                    notificationErrorColor = value;
                }
            }
        }

        private Color altNotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");
        [XmlElement(nameof(AltNotificationWarningColor))]
        public Color AltNotificationWarningColor {
            get {
                return AltColorSchema.NotificationWarningColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.NotificationWarningColor = value;
                    altNotificationWarningColor = value;
                }
            }
        }

        private Color altNotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");
        [XmlElement(nameof(AltNotificationErrorColor))]
        public Color AltNotificationErrorColor {
            get {
                return AltColorSchema.NotificationErrorColor;
            }
            set {
                if (ColorSchemaName == "Alternative Custom") {
                    AltColorSchema.NotificationErrorColor = value;
                    altNotificationErrorColor = value;
                }
            }
        }
    }
}
