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

using System.ComponentModel;

namespace NINA.Utility.SkySurvey {

    internal class SkySurveyFactory : ISkySurveyFactory {

        public ISkySurvey Create(SkySurveySource source) {
            switch (source) {
                case SkySurveySource.NASA:
                    return new NASASkySurvey();

                case SkySurveySource.SKYSERVER:
                    return new SkyServerSkySurvey();

                case SkySurveySource.STSCI:
                    return new StsciSkySurvey();

                case SkySurveySource.ESO:
                    return new ESOSkySurvey();

                case SkySurveySource.FILE:
                    return new FileSkySurvey();

                case SkySurveySource.SKYATLAS:
                    return new SkyAtlasSkySurvey();

                default:
                    return new NASASkySurvey();
            }
        }
    }

    internal interface ISkySurveyFactory {

        ISkySurvey Create(SkySurveySource source);
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SkySurveySource {

        [Description("LblNASASkySurvey")]
        NASA,

        [Description("LblSkyServerSkySurvey")]
        SKYSERVER,

        [Description("LblStsciSkySurvey")]
        STSCI,

        [Description("LblEsoSkySurvey")]
        ESO,

        [Description("LblSkyAtlasSkySurvey")]
        SKYATLAS,

        [Description("LblFile")]
        FILE,

        [Description("LblCache")]
        CACHE,
    }

    public static class SkySurveySourceExtension {

        public static string GetCacheSourceString(this SkySurveySource source) {
            switch (source) {
                case SkySurveySource.NASA:
                    return typeof(NASASkySurvey).Name;

                case SkySurveySource.SKYSERVER:
                    return typeof(SkyServerSkySurvey).Name;

                case SkySurveySource.STSCI:
                    return typeof(StsciSkySurvey).Name;

                case SkySurveySource.ESO:
                    return typeof(ESOSkySurvey).Name;

                case SkySurveySource.SKYATLAS:
                    return typeof(SkyAtlasSkySurvey).Name;

                case SkySurveySource.FILE:
                    return typeof(FileSkySurvey).Name;

                case SkySurveySource.CACHE:
                    return typeof(CacheSkySurvey).Name;

                default:
                    return string.Empty;
            }
        }
    }
}