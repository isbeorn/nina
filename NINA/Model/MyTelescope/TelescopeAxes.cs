using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyTelescope {

    public enum TelescopeAxes {

        // RightAscension or Azimuth.
        Primary = 0,

        // Declination or Altitude
        Secondary = 1,

        // Derotator
        Tertiary = 2
    }
}