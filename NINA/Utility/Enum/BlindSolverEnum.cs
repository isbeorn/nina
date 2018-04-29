using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum BlindSolverEnum {

        [Description("LblAstrometryNet")]
        ASTROMETRY_NET,

        [Description("LblLocalPlatesolver")]
        LOCAL
    }
}