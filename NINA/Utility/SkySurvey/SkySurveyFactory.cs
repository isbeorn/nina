using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Model;
using NINA.Utility.Astrometry;
using NINA.ViewModel;

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

        [Description("LblFile")]
        FILE,

        [Description("LblCache")]
        CACHE
    }
}