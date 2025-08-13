#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Core.Utility;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Core.Utility.WindowService;
using Nito.AsyncEx;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Model.Equipment;
using NINA.WPF.Base.Model;
using NINA.Core.Enum;
using NINA.ViewModel.FlatWizard;

namespace NINA.ViewModel.Interfaces {

    public interface IFlatWizardVM : ICameraConsumer, IFilterWheelConsumer, ITelescopeConsumer, IFlatDeviceConsumer {
        double CalculatedExposureTime { get; set; }
        double CalculatedHistogramMean { get; set; }
        bool CameraConnected { get; set; }
        RelayCommand CancelFlatExposureSequenceCommand { get; }
        ObservableCollection<FilterInfo> FilterInfos { get; }
        ObservableCollection<FlatWizardFilterSettingsWrapper> Filters { get; set; }
        int FlatCount { get; set; }
        bool IsPaused { get; set; }
        int Mode { get; set; }
        RelayCommand PauseFlatExposureSequenceCommand { get; }
        RelayCommand ResumeFlatExposureSequenceCommand { get; }
        FilterInfo SelectedFilter { get; set; }
        FlatWizardFilterSettingsWrapper SingleFlatWizardFilterSettings { get; set; }
        IAsyncCommand StartFlatSequenceCommand { get; }
        bool PauseBetweenFilters { get; set; }
        FlatWizardMode FlatWizardMode { get; set; }
        IWindowService WindowService { get; set; }
        Task<bool> StartFlatMagic(IEnumerable<FlatWizardFilterSettingsWrapper> filterWrappers, PauseToken pt);
    }
}