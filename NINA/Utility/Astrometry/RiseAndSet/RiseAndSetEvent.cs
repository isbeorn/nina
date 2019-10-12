#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Threading.Tasks;

namespace NINA.Utility.Astrometry {

    public abstract class RiseAndSetEvent {

        public RiseAndSetEvent(DateTime date, double latitude, double longitude) {
            this.Date = date;
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
        /// Calculates rise and set time
        /// Caveat: does not consider more than one rise and one set event
        /// </summary>
        /// <returns></returns>
        public Task<bool> Calculate() {
            return Task.Run(async () => {
                // Check rise and set events in two hour periods
                var offset = 0;

                do {
                    // Shift date by offset
                    var offsetDate = Date.AddHours(offset);

                    // Get three body locations for date, date + 1 hour and date + 2 hours
                    var bodyAt0 = GetBody(offsetDate);
                    var bodyAt1 = GetBody(offsetDate.AddHours(1));
                    var bodyAt2 = GetBody(offsetDate.AddHours(2));

                    await Task.WhenAll(bodyAt0.Calculate(), bodyAt1.Calculate(), bodyAt2.Calculate());

                    var location = new NOVAS.OnSurface() {
                        Latitude = Latitude,
                        Longitude = Longitude
                    };

                    // Adjust altitude for the three body parameters
                    var altitude0 = AdjustAltitude(bodyAt0);
                    var altitude1 = AdjustAltitude(bodyAt1);
                    var altitude2 = AdjustAltitude(bodyAt2);

                    // fit the three reference positions into a quadratic equation

                    //P1 (offsetDate | altitude0) => (0 | altitude0)
                    //P2 (offsetDate + 1 | altitude1) => (1 | altitude1)
                    //P3 (offsetDate + 2 | altitude2) => (2 | altitude2)

                    // ax² + bx + c

                    // Solve for c
                    // => altitude0 = 0 * x² + 0 * x + c => altitude0 = c

                    // Solve for b using c
                    // altitude1 = a * 1² + b * 1 + altitude0
                    //    => altitude1 = a + b + altitude0
                    //    => b = altitude1 - a - altitude0

                    // Solve for a using b and c
                    // altitude2 = a * 2² + b * 2 + altitude0
                    //   => altitude2 = 4a + 2(altitude1 - a - altitude0) + altitude0
                    //   => altitude2 = 4a + 2*altitude1 - 2a - 2*altitude0 + altitude0
                    //   => altitude2 = 2a + 2*altitude1 - altitude0
                    //   => 2a = altitude2 - 2*altitude1 + altitude0
                    //   => a = 0.5 * altitude2  - altitude1 + 0.5 * altitude0
                    //   => a = 0.5 * (altitude2 + altitude0) - altitude1

                    // Solve for b using a and c
                    //   => b = altitude1 - (0.5 * (altitude2 + altitude0) - altitude1) - altitude0
                    //   => b = altitude1 - 0.5 * altitude2 - 0.5 * altitude0 + altitude1 - altitude0
                    //   => b = 2 * altitude1 - 0.5 * altitude2 - 1.5 * altitude0

                    var a = 0.5 * (altitude2 + altitude0) - altitude1;
                    var b = 2 * altitude1 - 0.5 * altitude2 - 1.5 * altitude0;
                    var c = altitude0;

                    // a-b-c formula
                    // x = -b +- Sqrt(b² - 4ac) / 2a

                    // Discriminant definition: b² - 4ac
                    var discriminant = (Math.Pow(b, 2)) - (4.0 * a * c);

                    var zeroPoint1 = double.NaN;
                    var zeroPoint2 = double.NaN;
                    var events = 0;

                    if (discriminant == 1) {
                        zeroPoint1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
                        if (zeroPoint1 >= 0 && zeroPoint1 <= 2) {
                            events++;
                        }
                    } else if (discriminant > 1) {
                        zeroPoint1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
                        zeroPoint2 = (-b - Math.Sqrt(discriminant)) / (2 * a);

                        // Check if zero point is inside the span of 0 to 2 (to be inside the checked timeframe)
                        if (zeroPoint1 >= 0 && zeroPoint1 <= 2) {
                            events++;
                        }
                        if (zeroPoint2 >= 0 && zeroPoint2 <= 2) {
                            events++;
                        }
                        if (zeroPoint1 < 0 || zeroPoint1 > 2) {
                            zeroPoint1 = zeroPoint2;
                        }
                    }

                    //find the gradient at zeroPoint1. positive => rise event, negative => set event
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
                    //Repeat until rise and set events are found, or after a whole day
                } while (!((this.Rise != null && this.Set != null) || offset > 24));

                return true;
            });
        }
    }
}