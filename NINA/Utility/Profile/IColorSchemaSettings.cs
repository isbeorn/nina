using System.Windows.Media;

namespace NINA.Utility.Profile {

    public interface IColorSchemaSettings : ISettings {
        Color AltBackgroundColor { get; set; }
        Color AltBorderColor { get; set; }
        Color AltButtonBackgroundColor { get; set; }
        Color AltButtonBackgroundSelectedColor { get; set; }
        Color AltButtonForegroundColor { get; set; }
        Color AltButtonForegroundDisabledColor { get; set; }
        ColorSchema AltColorSchema { get; set; }
        string AltColorSchemaName { get; set; }
        Color AltNotificationErrorColor { get; set; }
        Color AltNotificationErrorTextColor { get; set; }
        Color AltNotificationWarningColor { get; set; }
        Color AltNotificationWarningTextColor { get; set; }
        Color AltPrimaryColor { get; set; }
        Color AltSecondaryColor { get; set; }
        Color BackgroundColor { get; set; }
        Color BorderColor { get; set; }
        Color ButtonBackgroundColor { get; set; }
        Color ButtonBackgroundSelectedColor { get; set; }
        Color ButtonForegroundColor { get; set; }
        Color ButtonForegroundDisabledColor { get; set; }
        ColorSchema ColorSchema { get; set; }
        string ColorSchemaName { get; set; }
        ColorSchemas ColorSchemas { get; set; }
        Color NotificationErrorColor { get; set; }
        Color NotificationErrorTextColor { get; set; }
        Color NotificationWarningColor { get; set; }
        Color NotificationWarningTextColor { get; set; }
        Color PrimaryColor { get; set; }
        Color SecondaryColor { get; set; }
    }
}