#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.WPF.Base.SkySurvey;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface ISkyMapAnnotator : IDisposable {
        event EventHandler ProjectionChanged;

        bool AnnotateConstellationBoundaries { get; set; }
        bool AnnotateConstellations { get; set; }
        bool AnnotateDSO { get; set; }
        bool AnnotateGrid { get; set; }
        DateTime? ObservationTime { get; set; }
        SkyMapProjectionMode ProjectionMode { get; set; }
        bool ShowHorizon { get; set; }
        bool UseCachedImages { get; set; }
        bool ShowAllCatalogues { get; set; }
        IList<ActiveCatalogue> ActiveCatalogues { get; set; }
        bool DynamicFoV { get; set; }
        bool Initialized { get; }
        ImageSource SkyMapOverlay { get; set; }
        SkyMapViewportProjection Projection { get; }
        ViewportFoV ViewportFoV { get; }

        ViewportFoV ChangeFoV(double vFoVDegrees);

        void ClearImagesForViewport();

        Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CacheSkySurvey cache, CancellationToken ct);

        Coordinates ShiftViewport(Vector delta);

        void UpdateDeviceInfo(TelescopeInfo deviceInfo);

        void UpdateSkyMap();
    }

    public partial class ActiveCatalogue : BaseINPC {
        [ObservableProperty]
        private string name;
        [ObservableProperty]
        private bool active;

        public ActiveCatalogue(string name, bool active) {
            Name = name;
            Active = active;
        }
    }
}
