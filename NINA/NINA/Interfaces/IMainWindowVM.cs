#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Interfaces;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Sequencer;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.ViewModel.Interfaces {

    public interface IMainWindowVM {
        IImagingVM ImagingVM { get; }
        IApplicationVM AppVM { get; }
        IEquipmentVM EquipmentVM { get; }
        ISkyAtlasVM SkyAtlasVM { get; }
        IFramingAssistantVM FramingAssistantVM { get; }
        IFlatWizardVM FlatWizardVM { get; }
        IDockManagerVM DockManagerVM { get; }
        ISequenceNavigationVM SequenceNavigationVM { get; }
        IOptionsVM OptionsVM { get; }
        IVersionCheckVM VersionCheckVM { get; }
        IApplicationStatusVM ApplicationStatusVM { get; }
        IApplicationDeviceConnectionVM ApplicationDeviceConnectionVM { get; }
        IImageSaveController ImageSaveController { get; }
        IImageHistoryVM ImageHistoryVM { get; }
        IPluginsVM PluginsVM { get; }
    }
}