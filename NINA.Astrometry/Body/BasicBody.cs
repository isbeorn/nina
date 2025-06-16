#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Threading.Tasks;

namespace NINA.Astrometry.Body {

    public abstract class BasicBody {

        public BasicBody(DateTime date, double latitude, double longitude, double elevation) {
            this.Date = date;
            this.Latitude = latitude;
            this.Longitude = longitude;
            Elevation = elevation;
        }

        public DateTime Date { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Elevation { get; }
        public double Distance { get; protected set; }
        public double Altitude { get; protected set; }

        public abstract double Radius { get; }
        protected abstract string Name { get; }
        protected abstract NOVAS.Body BodyNumber { get; }

        public Task Calculate() {
            return Task.Run(() => {
                var jd = AstroUtil.GetJulianDate(Date);
                var deltaT = AstroUtil.DeltaT(Date);

                var location = new NOVAS.OnSurface() {
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Height = Elevation
                };

                var observer = new NOVAS.Observer() {
                    OnSurf = location,
                    Where = (short)NOVAS.ObserverLocation.EarthSurface
                };

                var obj = new NOVAS.CelestialObject() {
                    Name = Name,
                    Number = (short)BodyNumber,
                    Star = new NOVAS.CatalogueEntry(),
                    Type = (short)NOVAS.ObjectType.MajorPlanetSunOrMoon
                };

                var objPosition = new NOVAS.SkyPosition();

                NOVAS.Place(jd, obj, observer, deltaT, NOVAS.CoordinateSystem.EquinoxOfDate, NOVAS.Accuracy.Full, ref objPosition);
                this.Distance = AstroUtil.AUToKilometer(objPosition.Dis);

                var siderealTime = AstroUtil.GetLocalSiderealTime(Date, Longitude);
                var hourAngle = AstroUtil.HoursToDegrees(AstroUtil.GetHourAngle(siderealTime, objPosition.RA));
                this.Altitude = AstroUtil.GetAltitude(hourAngle, Latitude, objPosition.Dec);
            });
        }
    }
}