using NINA.Utility.Mediator;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class ColorSchemaSettings : IColorSchemaSettings {

        public ColorSchemaSettings() {
            Initialize();
        }

        private void Initialize() {
            ColorSchemas = ColorSchemas.ReadColorSchemas();
            ColorSchemas.Items.Add(new ColorSchema {
                Name = (ColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom") ? "Alternative Custom" : "Custom",
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                BorderColor = borderColor,
                BackgroundColor = backgroundColor,
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

        [OnDeserialized]
        private void SetValuesOnDeserialized(StreamingContext context) {
            Initialize();
        }

        public ColorSchemas ColorSchemas { get; set; }

        [DataMember]
        public ColorSchema ColorSchema { get; set; }

        [DataMember]
        public ColorSchema AltColorSchema { get; set; }

        private string colorSchemaName = "Light";

        [DataMember]
        public string ColorSchemaName {
            get {
                return colorSchemaName;
            }
            set {
                colorSchemaName = value;
                ColorSchema = ColorSchemas?.Items.Where(x => x.Name == value).FirstOrDefault();
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
                AltColorSchema = ColorSchemas?.Items.Where(x => x.Name == value).FirstOrDefault();
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        [DataMember]
        private Color primaryColor = (Color)ColorConverter.ConvertFromString("#FF000000");

        [DataMember]
        public Color PrimaryColor {
            get {
                return ColorSchema.PrimaryColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.PrimaryColor = value;
                    primaryColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color secondaryColor = (Color)ColorConverter.ConvertFromString("#FF54748C");

        [DataMember]
        public Color SecondaryColor {
            get {
                return ColorSchema.SecondaryColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.SecondaryColor = value;
                    secondaryColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color borderColor = (Color)ColorConverter.ConvertFromString("#AABCBCBC");

        [DataMember]
        public Color BorderColor {
            get {
                return ColorSchema.BorderColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.BorderColor = value;
                    borderColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color backgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        [DataMember]
        public Color BackgroundColor {
            get {
                return ColorSchema.BackgroundColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.BackgroundColor = value;
                    backgroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color buttonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0B3C5D");

        [DataMember]
        public Color ButtonBackgroundColor {
            get {
                return ColorSchema.ButtonBackgroundColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.ButtonBackgroundColor = value;
                    buttonBackgroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color buttonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF2190DB");

        [DataMember]
        public Color ButtonBackgroundSelectedColor {
            get {
                return ColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.ButtonBackgroundSelectedColor = value;
                    buttonBackgroundSelectedColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color buttonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF1D2731");

        [DataMember]
        public Color ButtonForegroundDisabledColor {
            get {
                return ColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.ButtonForegroundDisabledColor = value;
                    buttonForegroundDisabledColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color buttonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        [DataMember]
        public Color ButtonForegroundColor {
            get {
                return ColorSchema.ButtonForegroundColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.ButtonForegroundColor = value;
                    buttonForegroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color notificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");

        [DataMember]
        public Color NotificationWarningColor {
            get {
                return ColorSchema.NotificationWarningColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.NotificationWarningColor = value;
                    notificationWarningColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color notificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFDB0606");

        [DataMember]
        public Color NotificationErrorColor {
            get {
                return ColorSchema.NotificationErrorColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.NotificationErrorColor = value;
                    notificationErrorColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color notificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        [DataMember]
        public Color NotificationWarningTextColor {
            get {
                return ColorSchema.NotificationWarningTextColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.NotificationWarningTextColor = value;
                    notificationWarningTextColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color notificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        [DataMember]
        public Color NotificationErrorTextColor {
            get {
                return ColorSchema.NotificationErrorTextColor;
            }
            set {
                if (ColorSchema != null && (ColorSchemaName == "Custom" || ColorSchemaName == "Alternative Custom")) {
                    ColorSchema.NotificationErrorTextColor = value;
                    notificationErrorTextColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altPrimaryColor = (Color)ColorConverter.ConvertFromString("#FF550C18");

        [DataMember]
        public Color AltPrimaryColor {
            get {
                return AltColorSchema.PrimaryColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.PrimaryColor = value;
                    altPrimaryColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altSecondaryColor = (Color)ColorConverter.ConvertFromString("#FF1B2A41");

        [DataMember]
        public Color AltSecondaryColor {
            get {
                return AltColorSchema.SecondaryColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.SecondaryColor = value;
                    altSecondaryColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altBorderColor = (Color)ColorConverter.ConvertFromString("#FF550C18");

        [DataMember]
        public Color AltBorderColor {
            get {
                return AltColorSchema.BorderColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.BorderColor = value;
                    altBorderColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A");

        [DataMember]
        public Color AltBackgroundColor {
            get {
                return AltColorSchema.BackgroundColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.BackgroundColor = value;
                    altBackgroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF550C18");

        [DataMember]
        public Color AltButtonBackgroundColor {
            get {
                return AltColorSchema.ButtonBackgroundColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.ButtonBackgroundColor = value;
                    altButtonBackgroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF96031A");

        [DataMember]
        public Color AltButtonBackgroundSelectedColor {
            get {
                return AltColorSchema.ButtonBackgroundSelectedColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.ButtonBackgroundSelectedColor = value;
                    altButtonBackgroundSelectedColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF443730");

        [DataMember]
        public Color AltButtonForegroundDisabledColor {
            get {
                return AltColorSchema.ButtonForegroundDisabledColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.ButtonForegroundDisabledColor = value;
                    altButtonForegroundDisabledColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A");

        [DataMember]
        public Color AltButtonForegroundColor {
            get {
                return AltColorSchema.ButtonForegroundColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.ButtonForegroundColor = value;
                    altButtonForegroundColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altNotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");

        [DataMember]
        public Color AltNotificationWarningColor {
            get {
                return AltColorSchema.NotificationWarningColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.NotificationWarningColor = value;
                    altNotificationWarningColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altNotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFF5A300");

        [DataMember]
        public Color AltNotificationErrorColor {
            get {
                return AltColorSchema.NotificationErrorColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.NotificationErrorColor = value;
                    altNotificationErrorColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altNotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A");

        [DataMember]
        public Color AltNotificationWarningTextColor {
            get {
                return AltColorSchema.NotificationWarningTextColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.NotificationWarningTextColor = value;
                    altNotificationWarningTextColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }

        [DataMember]
        private Color altNotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A");

        [DataMember]
        public Color AltNotificationErrorTextColor {
            get {
                return AltColorSchema.NotificationErrorTextColor;
            }
            set {
                if (AltColorSchema != null && (AltColorSchemaName == "Alternative Custom" || AltColorSchemaName == "Custom")) {
                    AltColorSchema.NotificationErrorTextColor = value;
                    altNotificationErrorTextColor = value;
                    Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                }
            }
        }
    }
}