#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

namespace NINA.Utility.FileFormat.XISF {

    public static class XISFImageProperty {

        public static class Observer {
            public static readonly string Namespace = "Observer:";
            public static readonly string[] EmailAddress = { Namespace + nameof(EmailAddress), "String" };
            public static readonly string[] Name = { Namespace + nameof(Name), "String" };
            public static readonly string[] PostalAddress = { Namespace + nameof(PostalAddress), "String" };
            public static readonly string[] Website = { Namespace + nameof(Website), "String" };
        }

        public static class Organization {
            public static readonly string Namespace = "Organization:";
            public static readonly string[] EmailAddress = { Namespace + nameof(EmailAddress), "String" };
            public static readonly string[] Name = { Namespace + nameof(Name), "String" };
            public static readonly string[] PostalAddress = { Namespace + nameof(PostalAddress), "String" };
            public static readonly string[] Website = { Namespace + nameof(Website), "String" };
        }

        public static class Observation {
            public static readonly string Namespace = "Observation:";
            public static readonly string[] CelestialReferenceSystem = { Namespace + nameof(CelestialReferenceSystem), "String" };
            public static readonly string[] BibliographicReferences = { Namespace + nameof(BibliographicReferences), "String" };

            public static class Center {
                public static readonly string Namespace = Observation.Namespace + "Center:";
                public static readonly string[] Dec = { Namespace + nameof(Dec), "Float64", "DEC" };
                public static readonly string[] RA = { Namespace + nameof(RA), "Float64", "RA" };
                public static readonly string[] X = { Namespace + nameof(X), "Float64" };
                public static readonly string[] Y = { Namespace + nameof(Y), "Float64" };
            }

            public static readonly string[] Description = { Namespace + nameof(Description), "String" };
            public static readonly string[] Equinox = { Namespace + nameof(Equinox), "Float64" };
            public static readonly string[] GeodeticReferenceSystem = { Namespace + nameof(GeodeticReferenceSystem), "String" };

            public static class Location {
                public static readonly string Namespace = Observation.Namespace + "Location:";
                public static readonly string[] Elevation = { Namespace + nameof(Elevation), "Float64", "SITEELEV" };
                public static readonly string[] Latitude = { Namespace + nameof(Latitude), "Float64", "SITELAT" };
                public static readonly string[] Longitude = { Namespace + nameof(Longitude), "Float64", "SITELONG" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String" };
            }

            public static class Meteorology {
                public static readonly string Namespace = Observation.Namespace + "Meteorology:";
                public static readonly string[] AmbientTemperature = { Namespace + nameof(AmbientTemperature), "Float32", "AMBTEMP" };
                public static readonly string[] AtmosphericPressure = { Namespace + nameof(AtmosphericPressure), "Float32", "PRESSURE" };
                public static readonly string[] RelativeHumidity = { Namespace + nameof(RelativeHumidity), "Float32", "HUMIDITY" };
                public static readonly string[] WindDirection = { Namespace + nameof(WindDirection), "Float32", "WINDDIR" };
                public static readonly string[] WindGust = { Namespace + nameof(WindGust), "Float32", "WINDGUST" };
                public static readonly string[] WindSpeed = { Namespace + nameof(WindSpeed), "Float32", "WINDSPD" };
            }

            public static class Object {
                public static readonly string Namespace = Observation.Namespace + "Object:";
                public static readonly string[] Dec = { Namespace + nameof(Dec), "Float64", "OBJCTDEC" };
                public static readonly string[] RA = { Namespace + nameof(RA), "Float64", "OBJCTRA" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String", "OBJECT" };
            }

            public static class Time {
                public static readonly string Namespace = Observation.Namespace + "Time:";
                public static readonly string[] End = { Namespace + nameof(End), "TimePoint" };
                public static readonly string[] Start = { Namespace + nameof(Start), "TimePoint", "DATE-OBS" };
            }

            public static readonly string[] Title = { Namespace + nameof(Title), "String" };
        }

        public static class Instrument {
            public static readonly string Namespace = "Instrument:";
            public static readonly string[] ExposureTime = { Namespace + nameof(ExposureTime), "Float32", "EXPOSURE" };

            public static class Camera {
                public static readonly string Namespace = Instrument.Namespace + "Camera:";

                public static readonly string[] Gain = { Namespace + nameof(Gain), "Float32", "EGAIN" };
                public static readonly string[] ISOSpeed = { Namespace + nameof(ISOSpeed), "Int32" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String", "INSTRUME" };
                public static readonly string[] ReadoutNoise = { Namespace + nameof(ReadoutNoise), "Float32" };
                public static readonly string[] Rotation = { Namespace + nameof(Rotation), "Float32" };
                public static readonly string[] XBinning = { Namespace + nameof(XBinning), "Int32", "XBINNING" };
                public static readonly string[] YBinning = { Namespace + nameof(YBinning), "Int32", "YBINNING" };
            }

            public static class Filter {
                public static readonly string Namespace = Instrument.Namespace + "Filter:";
                public static readonly string[] Name = { Namespace + nameof(Name), "String", "FILTER" };
            }

            public static class Focuser {
                public static readonly string Namespace = Instrument.Namespace + "Focuser:";
                public static readonly string[] Position = { Namespace + nameof(Position), "Float32" };
            }

            public static class Sensor {
                public static readonly string Namespace = Instrument.Namespace + "Sensor:";
                public static readonly string[] TargetTemperature = { Namespace + nameof(TargetTemperature), "Float32" };
                public static readonly string[] Temperature = { Namespace + nameof(Temperature), "Float32", "CCD-TEMP" };
                public static readonly string[] XPixelSize = { Namespace + nameof(XPixelSize), "Float32", "XPIXSZ" };
                public static readonly string[] YPixelSize = { Namespace + nameof(YPixelSize), "Float32", "YPIXSZ" };
            }

            public static class Telescope {
                public static readonly string Namespace = Instrument.Namespace + "Telescope:";
                public static readonly string[] Aperture = { Namespace + nameof(Aperture), "Float32" };
                public static readonly string[] CollectingArea = { Namespace + nameof(CollectingArea), "Float32" };
                public static readonly string[] FocalLength = { Namespace + nameof(FocalLength), "Float32" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String", "TELESCOP" };
            }
        }

        public static class Image {
            public static readonly string Namespace = "Image:";
            public static readonly string[] FrameNumber = { Namespace + nameof(FrameNumber), "UInt32" };
            public static readonly string[] GroupId = { Namespace + nameof(GroupId), "String" };
            public static readonly string[] SubgroupId = { Namespace + nameof(SubgroupId), "String" };
        }
    }
}