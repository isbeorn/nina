#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Locale;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel.FlatWizard {

    public interface IFlatWizardVM : ICameraConsumer, IFilterWheelConsumer, ITelescopeConsumer, IFlatDeviceConsumer {
        BinningMode BinningMode { get; set; }
        double CalculatedExposureTime { get; set; }
        double CalculatedHistogramMean { get; set; }
        bool CameraConnected { get; set; }
        RelayCommand CancelFlatExposureSequenceCommand { get; }
        ObservableCollection<FilterInfo> FilterInfos { get; }
        ObservableCollection<FlatWizardFilterSettingsWrapper> Filters { get; set; }
        int FlatCount { get; set; }
        IFlatWizardExposureTimeFinderService FlatWizardExposureTimeFinderService { get; set; }
        ILoc Locale { get; set; }
        IImagingVM ImagingVM { get; }
        int Gain { get; set; }
        BitmapSource Image { get; set; }
        bool IsPaused { get; set; }
        int Mode { get; set; }
        RelayCommand PauseFlatExposureSequenceCommand { get; }
        RelayCommand ResumeFlatExposureSequenceCommand { get; }
        FilterInfo SelectedFilter { get; set; }
        FlatWizardFilterSettingsWrapper SingleFlatWizardFilterSettings { get; set; }
        IAsyncCommand StartFlatSequenceCommand { get; }
        ApplicationStatus Status { get; set; }
        bool PauseBetweenFilters { get; set; }
    }
}