#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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

                case SkySurveySource.HIPS2FITS:
                    return new Hips2FitsSurvey();

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

        [Description("LblHips2FitsSurvey")]
        HIPS2FITS,

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

                case SkySurveySource.HIPS2FITS:
                    return typeof(Hips2FitsSurvey).Name;

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