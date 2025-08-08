using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Enum {
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum FITSCompressionTypeEnum {

        [Description("LblNone")]
        NONE = 0,

        [Description("LblCompressionRICE")]
        RICE,

        [Description("LblCompressionGZIP1")]
        GZIP1,

        [Description("LblCompressionGZIP2")]
        GZIP2,

        [Description("LblCompressionPLIO")]
        PLIO,

        [Description("LblCompressionHCOMPRESS")]
        HCOMPRESS
    }
}
