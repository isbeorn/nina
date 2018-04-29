using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum WeatherDataEnum {

        [Description("LblOpenWeatherMapOrg")]
        OPENWEATHERMAP
    }
}