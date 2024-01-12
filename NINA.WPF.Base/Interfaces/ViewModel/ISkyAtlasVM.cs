#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Windows.Input;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Core.Model;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface ISkyAtlasVM {
        AsyncObservableCollection<string> BrightnessFrom { get; set; }
        AsyncObservableCollection<string> BrightnessThrough { get; set; }
        ICommand CancelSearchCommand { get; }
        AsyncObservableCollection<string> Constellations { get; set; }
        AsyncObservableCollection<KeyValuePair<double?, string>> DecFrom { get; set; }
        AsyncObservableCollection<KeyValuePair<double?, string>> DecThrough { get; set; }
        AsyncObservableCollection<string> MagnitudesFrom { get; set; }
        AsyncObservableCollection<string> MagnitudesThrough { get; set; }
        AsyncObservableCollection<KeyValuePair<double, string>> MinimumAltitudeDegrees { get; set; }
        AsyncObservableCollection<DSOObjectType> ObjectTypes { get; set; }
        SkyAtlasOrderByDirectionEnum OrderByDirection { get; set; }
        SkyAtlasOrderByFieldsEnum OrderByField { get; set; }
        int PageSize { get; set; }
        AsyncObservableCollection<KeyValuePair<double?, string>> RAFrom { get; set; }
        AsyncObservableCollection<KeyValuePair<double?, string>> RAThrough { get; set; }
        ICommand ResetFiltersCommand { get; }
        ICommand SearchCommand { get; }
        string SearchObjectName { get; set; }
        PagedList<DeepSkyObject> SearchResult { get; set; }
        DateTime SelectedAltitudeTimeFrom { get; set; }
        DateTime SelectedAltitudeTimeThrough { get; set; }
        double? SelectedBrightnessFrom { get; set; }
        double? SelectedBrightnessThrough { get; set; }
        string SelectedConstellation { get; set; }
        double? SelectedDecFrom { get; set; }
        double? SelectedDecThrough { get; set; }
        double? SelectedMagnitudeFrom { get; set; }
        double? SelectedMagnitudeThrough { get; set; }
        double SelectedMinimumAltitudeDegrees { get; set; }
        double? SelectedRAFrom { get; set; }
        double? SelectedRAThrough { get; set; }
        double? SelectedSizeFrom { get; set; }
        double? SelectedSizeThrough { get; set; }
        ICommand SetFramingAssistantCoordinatesCommand { get; }
        ICommand SetSequencerTargetCommand { get; }
        AsyncObservableCollection<KeyValuePair<string, string>> SizesFrom { get; set; }
        AsyncObservableCollection<KeyValuePair<string, string>> SizesThrough { get; set; }
        IAsyncCommand SlewToCoordinatesCommand { get; }
        NighttimeData NighttimeData { get; set; }
    }
}