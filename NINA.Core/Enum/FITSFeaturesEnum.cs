using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Enum {
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum FITSRowOrder {
        [Description("LblTopDown")]
        TOP_DOWN,
        [Description("LblBottomUp")]
        BOTTOM_UP
    }
}
