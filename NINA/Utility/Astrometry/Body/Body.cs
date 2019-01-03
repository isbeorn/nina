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

    public abstract class Body {

        public Body(DateTime date, double latitude, double longitude) {
            this.Date = date;
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public DateTime Date { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Distance { get; protected set; }
        public double Altitude { get; protected set; }

        public abstract double Radius { get; }
        protected abstract string Name { get; }
        protected abstract NOVAS.Body BodyNumber { get; }

        public Task Calculate() {
            return Task.Run(() => {
                var jd = Astrometry.GetJulianDate(Date);
                var deltaT = Astrometry.DeltaT(Date);

                var location = new NOVAS.OnSurface() {
                    Latitude = Latitude,
                    Longitude = Longitude
                };

                var observer = new NOVAS.Observer() {
                    OnSurf = location,
                    Where = (short)NOVAS.ObserverLocation.EarthGeoCenter
                };

                var obj = new NOVAS.CelestialObject() {
                    Name = Name,
                    Number = (short)BodyNumber,
                    Star = new NOVAS.CatalogueEntry(),
                    Type = (short)NOVAS.ObjectType.MajorPlanetSunOrMoon
                };

                var objPosition = new NOVAS.SkyPosition();

                NOVAS.Place(jd + Astrometry.SecondsToDays(deltaT), obj, observer, deltaT, NOVAS.CoordinateSystem.EquinoxOfDate, NOVAS.Accuracy.Full, ref objPosition);
                this.Distance = Astrometry.AUToKilometer(objPosition.Dis);

                var siderealTime = Astrometry.GetLocalSiderealTime(Date, Longitude);
                var hourAngle = Astrometry.HoursToDegrees(Astrometry.GetHourAngle(siderealTime, objPosition.RA));
                this.Altitude = Astrometry.GetAltitude(hourAngle, Latitude, objPosition.Dec);
            });
        }
    }
}