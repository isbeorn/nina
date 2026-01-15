#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.Body;
using System;

namespace NINA.Astrometry.RiseAndSet {

    public abstract class RiseAndSetEvent {

        [Obsolete("Use method with elevation parameter instead")]
        public RiseAndSetEvent(DateTime date, double latitude, double longitude) : this(date, latitude, longitude, elevation: 0) { }
        public RiseAndSetEvent(DateTime date, double latitude, double longitude, double elevation) {
            this.Date = date;
            this.Latitude = latitude;
            this.Longitude = longitude;
            Elevation = elevation;
        }

        public DateTime Date { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Elevation { get; private set; }
        public virtual DateTime? Rise { get; private set; }
        public virtual DateTime? Set { get; private set; }

        protected abstract double AdjustAltitude(BasicBody body);

        protected abstract BasicBody GetBody(DateTime date);

        /// <summary>
        /// Calculates rise and set time
        /// Caveat: does not consider more than one rise and one set event
        /// </summary>
        /// <returns></returns>
        public virtual bool Calculate() {
            // Check rise and set events in two hour periods
            var offset = 0;

            do {
                // Shift date by offset
                var offsetDate = Date.AddHours(offset);

                // Get three body locations for date, date + 1 hour and date + 2 hours
                var bodyAt0 = GetBody(offsetDate);
                var bodyAt1 = GetBody(offsetDate.AddHours(1));
                var bodyAt2 = GetBody(offsetDate.AddHours(2));

                bodyAt0.Calculate();
                bodyAt1.Calculate();
                bodyAt2.Calculate();

                // Adjust altitude for the three body parameters
                var altitude0 = AdjustAltitude(bodyAt0);
                var altitude1 = AdjustAltitude(bodyAt1);
                var altitude2 = AdjustAltitude(bodyAt2);

                // fit the three reference positions into a quadratic equation

                //P1 (offsetDate | altitude0) => (0 | altitude0)
                //P2 (offsetDate + 1 | altitude1) => (1 | altitude1)
                //P3 (offsetDate + 2 | altitude2) => (2 | altitude2)

                // ax^2 + bx + c

                // Solve for c
                // => altitude0 = 0 * x^2 + 0 * x + c => altitude0 = c

                // Solve for b using c
                // altitude1 = a * 1^2 + b * 1 + altitude0
                //    => altitude1 = a + b + altitude0
                //    => b = altitude1 - a - altitude0

                // Solve for a using b and c
                // altitude2 = a * 2^2 + b * 2 + altitude0
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
                // x = -b +- Sqrt(b^2 - 4ac) / 2a

                // Discriminant definition: b^2 - 4ac
                var discriminant = (b * b) - (4.0 * a * c);

                const double epsilon = 1e-5;
                const double discEps = 1e-10;

                if (discriminant >= -discEps) {
                    if (discriminant < 0) { 
                        discriminant = 0; 
                    }
                    double sqrtD = Math.Sqrt(discriminant);

                    double x1, x2;
                    if (Math.Abs(a) < epsilon) {
                        if (Math.Abs(b) < epsilon) {
                            continue; // no usable root in this window
                        }
                        // Linear fallback: bx + c = 0
                        x1 = x2 = -c / b;
                    } else {
                        x1 = (-b + sqrtD) / (2 * a);
                        x2 = (-b - sqrtD) / (2 * a);
                    }

                    bool x1Valid = !double.IsNaN(x1) && x1 >= -epsilon && x1 <= 2 + epsilon;
                    bool x2Valid = !double.IsNaN(x2) && x2 >= -epsilon && x2 <= 2 + epsilon && Math.Abs(x1 - x2) > epsilon;

                    if (x1Valid) x1 = Math.Clamp(x1, 0, 2);
                    if (x2Valid) x2 = Math.Clamp(x2, 0, 2);

                    if (x1Valid) { 
                        AssignEvent(x1, a, b, offsetDate); 
                    }
                    if (x2Valid) { 
                        AssignEvent(x2, a, b, offsetDate); 
                    }
                }
                offset += 2;
                //Repeat until rise and set events are found, or after a whole day
            } while (!((this.Rise != null && this.Set != null) || offset > 24));

            return Rise != null || Set != null;
        }

        private void AssignEvent(double x, double a, double b, DateTime offsetDate) {
            var slope = 2 * a * x + b;
            var eventTime = offsetDate.AddHours(x);

            if (slope > 0) {
                if (Rise == null || eventTime < Rise.Value) {
                    Rise = eventTime;
                }
            } else {
                if (Set == null || eventTime < Set.Value) {
                    Set = eventTime;
                }
            }
        }
    }
}