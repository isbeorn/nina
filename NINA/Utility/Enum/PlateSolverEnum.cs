using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum PlateSolverEnum {

        [Description("LblAstrometryNet")]
        ASTROMETRY_NET,

        [Description("LblLocalPlatesolver")]
        LOCAL,

        [Description("LblPlatesolve2")]
        PLATESOLVE2
    }
}