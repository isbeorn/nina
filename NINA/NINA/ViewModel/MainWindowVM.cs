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
using NINA.Utility;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.ViewModel {

    internal class MainWindowVM : IMainWindowVM {
        public IImagingVM ImagingVM { get; set; }
        public IApplicationVM AppVM { get; set; }
        public IEquipmentVM EquipmentVM { get; set; }
        public ISkyAtlasVM SkyAtlasVM { get; set; }
        public IFramingAssistantVM FramingAssistantVM { get; set; }
        public IFlatWizardVM FlatWizardVM { get; set; }
        public IDockManagerVM DockManagerVM { get; set; }
        public ISequenceNavigationVM SequenceNavigationVM { get; set; }
        public IOptionsVM OptionsVM { get; set; }
        public IVersionCheckVM VersionCheckVM { get; set; }
        public IApplicationStatusVM ApplicationStatusVM { get; set; }
        public IApplicationDeviceConnectionVM ApplicationDeviceConnectionVM { get; set; }
        public IImageSaveController ImageSaveController { get; set; }
        public IImageHistoryVM ImageHistoryVM { get; set; }
        public IPluginsVM PluginsVM { get; set; }
        public GlobalObjects GlobalObjects { get; set; }
    }
}