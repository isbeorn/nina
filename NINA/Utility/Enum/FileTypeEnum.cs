using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Enum
{
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
