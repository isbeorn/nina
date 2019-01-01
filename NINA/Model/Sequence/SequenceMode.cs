using NINA.Utility;
using System.ComponentModel;

namespace NINA.Model {
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SequenceMode {

        [Description("LblSequenceModeStandard")]
        STANDARD,

        [Description("LblSequenceModeRotate")]
        ROTATE
    }
}