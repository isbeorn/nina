#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.ComponentModel;

namespace NINA.Core.Enum {

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

        [Description("LblOfflineSkyMap")]
        SKYATLAS,

        [Description("LblFile")]
        FILE,

        [Description("LblCache")]
        CACHE,
    }

    //[TypeConverter(typeof(EnumDescriptionTypeConverter))]
    //public enum HipsSkyMapSource {

    //    [Description("DSS2 Color Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/DSS2/color")]
    //    DSS2_COLOR,

    //    [Description("DSS2 Red Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/DSS2/red")]
    //    DSS2_RED,

    //    [Description("DSS2 Blue Coverage: 99.67%")]
    //    [HipsSkyMapValue("CDS/P/DSS2/blue")]
    //    DSS2_BLUE,

    //    [Description("DSS2 NIR Coverage: 99.55%")]
    //    [HipsSkyMapValue("CDS/P/DSS2/NIR")]
    //    DSS2_NIR,
        
    //    [Description("2MASS color J (1.23um), H (1.66um), K (2.16um) Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/2MASS/color")]
    //    TWO_MASS_COLOR,

    //    [Description("2MASS J (1.23um) Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/2MASS/J")]
    //    TWO_MASS_J,

    //    [Description("2MASS H (1.66um) Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/2MASS/H")]
    //    TWO_MASS_H,

    //    [Description("2MASS K (2.16um) Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/2MASS/K")]
    //    TWO_MASS_K,

    //    [Description("AKARI FIS Color WideL (140um), WideS (90um), N60 (65um) Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/AKARI/FIS/Color")]
    //    AKARI_FIS_COLOR,

    //    [Description("IRAS-IRIS HEALPix survey, color Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/IRIS/color")]
    //    IRAS_IRIS_COLOR,

    //    [Description("Mellinger color Coverage: 100%")]
    //    [HipsSkyMapValue("CDS/P/Mellinger/color")]
    //    MELLINGER_COLOR,

    //    [Description("CTA-FRAM Sky Survey, color Coverage: 100%")]
    //    [HipsSkyMapValue("fzu.cz/P/CTA-FRAM/survey/color")]
    //    CTA_FRAM_COLOR,

    //    [Description("PanSTARRS DR1 color (from bands z and g) Coverage: 78%")]
    //    [HipsSkyMapValue("CDS/P/PanSTARRS/DR1/color-z-zg-g")]
    //    PANSTARRS_DR1_COLOR,

    //    [Description("PanSTARRS DR1 color (i, r, g) Coverage: 76%")]
    //    [HipsSkyMapValue("CDS/P/PanSTARRS/DR1/color-i-r-g")]
    //    PANSTARRS_DR1_COLOR_IRG,

    //}

    //[AttributeUsage(AttributeTargets.Field)]
    //public class HipsSkyMapValue(string value) : Attribute {
    //    public string Value { get; } = value;
    //}

}