using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum LogLevelEnum {

        [Description("LblError")]
        ERROR,

        [Description("LblInfo")]
        INFO,

        [Description("LblWarning")]
        WARNING,

        [Description("LblDebug")]
        DEBUG,

        [Description("LblTrace")]
        TRACE
    }
}