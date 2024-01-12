#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyTelescope;
using NINA.Astrometry;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using NINA.WPF.Base.Model.FramingAssistant;
using NINA.WPF.Base.SkySurvey;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface ISkyMapAnnotator {
        bool AnnotateConstellationBoundaries { get; set; }
        bool AnnotateConstellations { get; set; }
        bool AnnotateDSO { get; set; }
        bool AnnotateGrid { get; set; }
        bool UseCachedImages { get; set; }
        List<FramingConstellationBoundary> ConstellationBoundariesInViewPort { get; }
        List<FramingConstellation> ConstellationsInViewport { get; }
        ICommand DragCommand { get; }
        List<FramingDSO> DSOInViewport { get; }
        bool DynamicFoV { get; set; }
        FrameLineMatrix2 FrameLineMatrix { get; }
        bool Initialized { get; }
        BitmapSource SkyMapOverlay { get; set; }
        ViewportFoV ViewportFoV { get; }

        void CalculateConstellationBoundaries();

        void CalculateFrameLineMatrix();

        ViewportFoV ChangeFoV(double vFoVDegrees);

        void ClearFrameLineMatrix();
        void ClearImagesForViewport();
        void Dispose();

        Dictionary<string, DeepSkyObject> GetDeepSkyObjectsForViewport();

        Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CacheSkySurvey cache, CancellationToken ct);

        Coordinates ShiftViewport(Vector delta);

        void UpdateDeviceInfo(TelescopeInfo deviceInfo);

        void UpdateSkyMap();
    }
}