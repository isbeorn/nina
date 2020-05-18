#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
