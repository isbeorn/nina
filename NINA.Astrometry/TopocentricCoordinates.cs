#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Database;
using NINA.Core.Enum;
using NINA.Core.Utility;
using System;
using System.Xml.Serialization;

namespace NINA.Astrometry {

    public class TopocentricCoordinates {
        private static ICustomDateTime SystemDateTime = new SystemDateTime();

        [XmlIgnore]
        public ICustomDateTime DateTime { get; }

        public Angle Azimuth { get; set; }
        public Angle Altitude { get; set; }
        public Angle Latitude { get; private set; }
        public Angle Longitude { get; private set; }
        public double Elevation { get; private set; }
        public AltitudeSite AltitudeSite => Azimuth.Degree >= 0 && Azimuth.Degree < 180 ? AltitudeSite.EAST : AltitudeSite.WEST;

        public TopocentricCoordinates(Angle azimuth, Angle altitude, Angle latitude, Angle longitude, double elevation, ICustomDateTime dateTime) {
            this.DateTime = dateTime;
            this.Azimuth = azimuth;
            this.Altitude = altitude;
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.Elevation = elevation;
        }

        public TopocentricCoordinates(Angle azimuth, Angle altitude, Angle latitude, Angle longitude, ICustomDateTime dateTime)
            : this(azimuth, altitude, latitude, longitude, 0.0d, dateTime) {
        }

        public TopocentricCoordinates(Angle azimuth, Angle altitude, Angle latitude, Angle longitude)
            : this(azimuth, altitude, latitude, longitude, SystemDateTime) {
        }

        public TopocentricCoordinates Clone() {
            return new TopocentricCoordinates(Azimuth.CopyByDegree(), Altitude.CopyByDegree(), Latitude.CopyByDegree(), Longitude.CopyByDegree());
        }

        public Coordinates Transform(Epoch epoch, DatabaseInteraction db = null) {
            return Transform(epoch, 0.0d, 0.0d, 0.0d, 0.0d, db);
        }

        /// <summary>
        /// Transforms observed coordinates to ICRS astrometric coordinates while applying refraction correction
        /// </summary>
        /// <param name="epoch">Epoch of resulting astrometric coordinates</param>
        /// <param name="pressurehPa">Pressure in hecto pascals (hPa) at the observer (not at sea level)</param>
        /// <param name="tempCelcius">Ambient temperature in Celcius</param>
        /// <param name="relativeHumidity">Relative humidity at the ambient temperature</param>
        /// <param name="wavelength">Wavelength of light in micrometers. 0.54 is approximately the center of a typical luminance bandpass and would be a reasonable default value to use</param>
        /// <param name="db">NINA database</param>
        /// <returns>Celestial coordinates</returns>
        public Coordinates Transform(Epoch epoch, double pressurehPa, double tempCelcius, double relativeHumidity, double wavelength, DatabaseInteraction db = null) {
            var now = DateTime.Now;
            var jdUTC = AstroUtil.GetJulianDate(now);

            var zenithDistance = AstroUtil.ToRadians(90d - Altitude.Degree);
            var deltaUT = AstroUtil.DeltaUT(now, db);

            var raRad = 0d;
            var decRad = 0d;
            SOFA.TopocentricToCelestial("A", Azimuth.Radians, zenithDistance, jdUTC, 0d, deltaUT, Longitude.Radians, Latitude.Radians, Elevation, 0d, 0d, pressurehPa, tempCelcius, relativeHumidity, wavelength, ref raRad, ref decRad);
            var ra = Angle.ByRadians(raRad);
            var dec = Angle.ByRadians(decRad);

            var coordinates = new Coordinates(ra, dec, Epoch.J2000, DateTime);
            return coordinates.Transform(epoch);
        }

        public override string ToString() {
            return $"Alt: {this.Altitude}; Az: {this.Azimuth}";
        }
    }
}