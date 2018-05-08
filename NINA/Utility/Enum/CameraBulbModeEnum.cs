using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum CameraBulbModeEnum {

        [Description("LblNative")]
        NATIVE,

        [Description("LblSerialPort")]
        SERIALPORT,

        [Description("LblTelescopeSnapPort")]
        TELESCOPESNAPPORT
    }
}