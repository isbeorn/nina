using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum FileTypeEnum {

        [Description("LblTiff")]
        TIFF,

        [Description("LblFits")]
        FITS,

        [Description("LblXisf")]
        XISF,

        [Description("LblTiffZip")]
        TIFF_ZIP,

        [Description("LblTiffLzw")]
        TIFF_LZW
    }
}