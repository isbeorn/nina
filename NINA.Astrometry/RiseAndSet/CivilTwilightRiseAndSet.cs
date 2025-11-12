using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Astrometry.RiseAndSet {
    internal class CivilTwilightRiseAndSet : SunCustomRiseAndSet {
        [Obsolete("Use method with elevation parameter instead")]
        public CivilTwilightRiseAndSet(DateTime date, double latitude, double longitude) : this(date, latitude, longitude, elevation: 0) { }

        public CivilTwilightRiseAndSet(DateTime date, double latitude, double longitude, double elevation) : base(date, latitude, longitude, elevation, -6) {
        }
    }
}
