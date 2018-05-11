using NINA.Utility.Mediator;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class ColorSchemaSettings : IColorSchemaSettings {

        public ColorSchemaSettings() {
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
                NotificationErrorColor = NotificationErrorColor,
                NotificationWarningTextColor = NotificationWarningTextColor,
                NotificationErrorTextColor = NotificationErrorTextColor
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
                NotificationErrorColor = AltNotificationErrorColor,
                NotificationWarningTextColor = AltNotificationWarningTextColor,
                NotificationErrorTextColor = AltNotificationErrorTextColor
            });
        }

        private ColorSchemas colorSchemas;

        public ColorSchemas ColorSchemas {
            get {
                if (colorSchemas == null) {
                    colorSchemas = ColorSchemas.ReadColorSchemas();
                }
                return colorSchemas;
            }
            set {
                colorSchemas = value;
            }
        }

        private ColorSchema colorSchema;

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

        [DataMember]
        public string ColorSchemaName {
            get {
                return colorSchemaName;
            }
            set {
                colorSchemaName = value;
                ColorSchema = ColorSchemas.Items.Where(x => x.Name == value).FirstOrDefault();
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private string altColorSchemaName = "Dark";

        [DataMember]
        public string AltColorSchemaName {
            get {
                return altColorSchemaName;
            }
            set {
                altColorSchemaName = value;
                AltColorSchema = ColorSchemas.Items.Where(x => x.Name == value).FirstOrDefault();
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private Color primaryColor = (Color)ColorConverter.ConvertFromString("#FF000000");

        [DataMember]
        public Color PrimaryColor {
            get {
                return ColorSchema.PrimaryColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.PrimaryColor = value;
                    primaryColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color secondaryColor = (Color)ColorConverter.ConvertFromString("#FF54748C");

        [DataMember]
        public Color SecondaryColor {
            get {
                return ColorSchema.SecondaryColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.SecondaryColor = value;
                    secondaryColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color borderColor = (Color)ColorConverter.ConvertFromString("#AABCBCBC");

        [DataMember]
        public Color BorderColor {
            get {
                return ColorSchema.BorderColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.BorderColor = value;
                    borderColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color backgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        [DataMember]
        public Color BackgroundColor {
            get {
                return ColorSchema.BackgroundColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.BackgroundColor = value;
                    backgroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color buttonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0B3C5D");

        [DataMember]
        public Color ButtonBackgroundColor {
            get {
                return ColorSchema.ButtonBackgroundColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.ButtonBackgroundColor = value;
                    buttonBackgroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color buttonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF2190DB");

        [DataMember]
        public Color ButtonBackgroundSelectedColor {
            get {
                return ColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.ButtonBackgroundSelectedColor = value;
                    buttonBackgroundSelectedColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color buttonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF1D2731");

        [DataMember]
        public Color ButtonForegroundDisabledColor {
            get {
                return ColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.ButtonForegroundDisabledColor = value;
                    buttonForegroundDisabledColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color buttonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        [DataMember]
        public Color ButtonForegroundColor {
            get {
                return ColorSchema.ButtonForegroundColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.ButtonForegroundColor = value;
                    buttonForegroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color notificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");

        [DataMember]
        public Color NotificationWarningColor {
            get {
                return ColorSchema.NotificationWarningColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.NotificationWarningColor = value;
                    notificationWarningColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color notificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFDB0606");

        [DataMember]
        public Color NotificationErrorColor {
            get {
                return ColorSchema.NotificationErrorColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.NotificationErrorColor = value;
                    notificationErrorColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color notificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        [DataMember]
        public Color NotificationWarningTextColor {
            get {
                return ColorSchema.NotificationWarningTextColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.NotificationWarningTextColor = value;
                    notificationWarningTextColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color notificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        [DataMember]
        public Color NotificationErrorTextColor {
            get {
                return ColorSchema.NotificationErrorTextColor;
            }
            set {
                if (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom") {
                    ColorSchema.NotificationErrorTextColor = value;
                    notificationErrorTextColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altPrimaryColor = (Color)ColorConverter.ConvertFromString("#FF550C18");

        [DataMember]
        public Color AltPrimaryColor {
            get {
                return AltColorSchema.PrimaryColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.PrimaryColor = value;
                    altPrimaryColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altSecondaryColor = (Color)ColorConverter.ConvertFromString("#FF1B2A41");

        [DataMember]
        public Color AltSecondaryColor {
            get {
                return AltColorSchema.SecondaryColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.SecondaryColor = value;
                    altSecondaryColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altBorderColor = (Color)ColorConverter.ConvertFromString("#FF550C18");

        [DataMember]
        public Color AltBorderColor {
            get {
                return AltColorSchema.BorderColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.BorderColor = value;
                    altBorderColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A");

        [DataMember]
        public Color AltBackgroundColor {
            get {
                return AltColorSchema.BackgroundColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.BackgroundColor = value;
                    altBackgroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF550C18");

        [DataMember]
        public Color AltButtonBackgroundColor {
            get {
                return AltColorSchema.ButtonBackgroundColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.ButtonBackgroundColor = value;
                    altButtonBackgroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF96031A");

        [DataMember]
        public Color AltButtonBackgroundSelectedColor {
            get {
                return AltColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.ButtonBackgroundSelectedColor = value;
                    altButtonBackgroundSelectedColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF443730");

        [DataMember]
        public Color AltButtonForegroundDisabledColor {
            get {
                return AltColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.ButtonForegroundDisabledColor = value;
                    altButtonForegroundDisabledColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A");

        [DataMember]
        public Color AltButtonForegroundColor {
            get {
                return AltColorSchema.ButtonForegroundColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.ButtonForegroundColor = value;
                    altButtonForegroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altNotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");

        [DataMember]
        public Color AltNotificationWarningColor {
            get {
                return AltColorSchema.NotificationWarningColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.NotificationWarningColor = value;
                    altNotificationWarningColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altNotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");

        [DataMember]
        public Color AltNotificationErrorColor {
            get {
                return AltColorSchema.NotificationErrorColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.NotificationErrorColor = value;
                    altNotificationErrorColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altNotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A");

        [DataMember]
        public Color AltNotificationWarningTextColor {
            get {
                return AltColorSchema.NotificationWarningTextColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.NotificationWarningTextColor = value;
                    altNotificationWarningTextColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        private Color altNotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A");

        [DataMember]
        public Color AltNotificationErrorTextColor {
            get {
                return AltColorSchema.NotificationErrorTextColor;
            }
            set {
                if (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") {
                    AltColorSchema.NotificationErrorTextColor = value;
                    altNotificationErrorTextColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }
    }
}