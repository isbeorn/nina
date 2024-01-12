#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Core.Utility;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Astrometry;
using NINA.WPF.Base.SkySurvey;
using System.Collections.Generic;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IFramingAssistantVM {
        double BoundHeight { get; set; }
        double BoundWidth { get; set; }
        int CameraHeight { get; set; }
        double CameraPixelSize { get; set; }
        AsyncObservableCollection<FramingRectangle> CameraRectangles { get; set; }
        int CameraWidth { get; set; }
        ICommand CancelLoadImageCommand { get; }
        ICommand CancelLoadImageFromFileCommand { get; }
        ICommand ClearCacheCommand { get; }
        ICommand CoordsFromScopeCommand { get; set; }
        ICommand CoordsFromPlanetariumCommand { get; set; }
        int DecDegrees { get; set; }
        int DecMinutes { get; set; }
        double DecSeconds { get; set; }
        IDeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; }
        int DownloadProgressValue { get; set; }
        ICommand DragMoveCommand { get; }
        ICommand DragStartCommand { get; }
        ICommand DragStopCommand { get; }
        DeepSkyObject DSO { get; set; }
        double FieldOfView { get; set; }
        double FocalLength { get; set; }
        int FontSize { get; set; }
        SkySurveySource FramingAssistantSource { get; set; }
        int HorizontalPanels { get; set; }
        XElement ImageCacheInfo { get; set; }
        SkySurveyImage ImageParameter { get; set; }
        IAsyncCommand LoadImageCommand { get; }
        ICommand MouseWheelCommand { get; }
        bool NegativeDec { get; set; }
        double Opacity { get; set; }
        double OverlapPercentage { get; set; }
        double OverlapPixels { get; set; }
        double OverlapValue { get; set; }
        List<string> OverlapUnits { get; set; }
        string SelectedOverlapUnit { get; set; }
        int RAHours { get; set; }
        int RAMinutes { get; set; }
        double RASeconds { get; set; }
        FramingRectangle Rectangle { get; set; }
        bool RectangleCalculated { get; }
        ICommand RefreshSkyMapAnnotationCommand { get; }
        ICommand ScrollViewerSizeChangedCommand { get; }
        XElement SelectedImageCacheInfo { get; set; }
        ICommand SetSequencerTargetCommand { get; }
        ISkyMapAnnotator SkyMapAnnotator { get; }
        ISkySurveyFactory SkySurveyFactory { get; set; }
        IAsyncCommand SlewToCoordinatesCommand { get; }
        ApplicationStatus Status { get; set; }
        int VerticalPanels { get; set; }
        CacheSkySurvey Cache { get; }

        void Dispose();

        Task<bool> SetCoordinates(DeepSkyObject dso);

        void UpdateDeviceInfo(CameraInfo cameraInfo);
    }
}