using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum GuiderScaleEnum {

        [Description("LblPixels")]
        PIXELS,

        [Description("LblArcsec")]
        ARCSECONDS
    }
}