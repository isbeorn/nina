using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Astrometry.RiseAndSet {
    internal class CivilTwilightRiseAndSet : SunCustomRiseAndSet {

        public CivilTwilightRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude, -6) {
        }
    }
}
