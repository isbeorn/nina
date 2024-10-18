#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;

namespace NINA.Profile.Interfaces {

    public interface IFramingAssistantSettings : ISettings {
        int CameraHeight { get; set; }
        int CameraWidth { get; set; }
        double FieldOfView { get; set; }
        double Opacity { get; set; }
        SkySurveySource LastSelectedImageSource { get; set; }
        double LastRotationAngle { get; set; }
        bool SaveImageInOfflineCache { get; set; }
        bool AnnotateConstellationBoundaries { get; set; }
        bool AnnotateConstellations { get; set; }
        bool AnnotateDSO { get; set; }
        bool AnnotateGrid { get; set; }
    }
}