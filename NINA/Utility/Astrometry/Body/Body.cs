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