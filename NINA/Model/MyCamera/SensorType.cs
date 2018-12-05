using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    public enum SensorType {

        //
        //     monochrome - no bayer encoding
        Monochrome = 0,

        //     Color image without bayer encoding
        Color = 1,

        //     RGGB bayer encoding
        RGGB = 2,

        //     CMYG bayer encoding
        CMYG = 3,

        //     CMYG2 bayer encoding
        CMYG2 = 4,

        //     Camera produces Kodak TRUESENSE Bayer LRGB array images
        LRGB = 5
    }
}