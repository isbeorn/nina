using System;
using System.Threading.Tasks;

namespace NINA.Utility.Astrometry {

    public abstract class RiseAndSetEvent {

        public RiseAndSetEvent(DateTime date, double latitude, double longitude) {
            this.Date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public DateTime Date { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public DateTime? Rise { get; private set; }
        public DateTime? Set { get; private set; }

        protected abstract double AdjustAltitude(Body body);

        protected abstract Body GetBody(DateTime date);

        /// <summary>
        /// Calculates rise and set time for the sun
        /// Caveat: does not consider more than two rises
        /// </summary>
        /// <returns></returns>
        public Task<bool> Calculate() {
            return Task.Run(async () => {
                // Check rise and set events in two hour periods
                var offset = 0;

                do {
                    // Shift date by offset
                    var offsetDate = Date.AddHours(offset);

                    // Get three sun locations for date, date + 1 hour and date + 2 hours
                    var bodyAt0 = GetBody(offsetDate);
                    var bodyAt1 = GetBody(offsetDate.AddHours(1));
                    var bodyAt2 = GetBody(offsetDate.AddHours(2));

                    await Task.WhenAll(bodyAt0.Calculate(), bodyAt1.Calculate(), bodyAt2.Calculate());

                    var location = new NOVAS.OnSurface() {
                        Latitude = Latitude,
                        Longitude = Longitude
                    };

                    // Adjust altitude for the three sunrise event
                    var altitude0 = AdjustAltitude(bodyAt0);
                    var altitude1 = AdjustAltitude(bodyAt1);
                    var altitude2 = AdjustAltitude(bodyAt2);

                    // Normalized quadratic equation parameters
                    var a = 0.5 * (altitude2 + altitude0) - altitude1;
                    var b = 0.5 * (altitude2 - altitude0);
                    var c = altitude0;

                    // x = -b +- Sqrt(b² - 4ac) / 2a   --- https://de.khanacademy.org/math/algebra/quadratics/solving-quadratics-using-the-quadratic-formula/a/discriminant-review

                    var xAxisSymmetry = -b / (2.0 * a);
                    var discriminant = (Math.Pow(b, 2)) - (4.0 * a * c);

                    var zeroPoint1 = double.NaN;
                    var zeroPoint2 = double.NaN;
                    var events = 0;

                    if (discriminant > 0) {
                        // Zero points detected
                        var delta = 0.5 * Math.Sqrt(discriminant) / Math.Abs(a);
                        zeroPoint1 = xAxisSymmetry - delta;
                        zeroPoint2 = xAxisSymmetry + delta;

                        if (Math.Abs(zeroPoint1) <= 1) {
                            events++;
                        }
                        if (Math.Abs(zeroPoint2) <= 1) {
                            events++;
                        }
                        if (zeroPoint1 < -1.0) {
                            zeroPoint1 = zeroPoint2;
                        }
                    }

                    var gradient = 2 * a * zeroPoint1 + b;

                    if (events == 1) {
                        if (gradient > 0) {
                            // rise
                            this.Rise = offsetDate.AddHours(zeroPoint1);
                        } else {
                            // set
                            this.Set = offsetDate.AddHours(zeroPoint1);
                        }
                    } else if (events == 2) {
                        if (gradient > 0) {
                            // rise and set
                            this.Rise = offsetDate.AddHours(zeroPoint1);
                            this.Set = offsetDate.AddHours(zeroPoint2);
                        } else {
                            // set and rise
                            this.Rise = offsetDate.AddHours(zeroPoint2);
                            this.Set = offsetDate.AddHours(zeroPoint1);
                        }
                    }
                    offset += 2;
                } while (!((this.Rise != null && this.Set != null) || offset > 24));

                return true;
            });
        }
    }
}