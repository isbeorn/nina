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
using NINA.Profile.Interfaces;
using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.WPF.Base.ViewModel {

    public partial class DockableVM : BaseVM, IDockableVM {

        public DockableVM(IProfileService profileService) : base(profileService) {
            this.CanClose = true;
            this.IsClosed = false;
            this.HasSettings = false;
            SettingsVisible = false;

            // Default image when nothing is set
            if (System.Windows.Application.Current != null) {
                ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PuzzlePieceSVG"];
            }
            IsVisible = true;

            HideCommand = new RelayCommand(Hide);
            ToggleSettingsCommand = new RelayCommand(ToggleSettings);
            profileService.LocationChanged += (object sender, EventArgs e) => {
                RaisePropertyChanged(nameof(Title));
            };
        }

        public virtual bool IsTool { get; } = false;

        [ObservableProperty]
        private bool isClosed;

        [ObservableProperty]
        private bool canClose;


        public virtual string ContentId => this.GetType().Name;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        protected bool isVisible;

        [ObservableProperty]
        private GeometryGroup imageGeometry;

        [ObservableProperty]
        protected bool hasSettings;

        [ObservableProperty]
        protected bool settingsVisible;
        public ICommand HideCommand { get; private set; }
        public ICommand ToggleSettingsCommand { get; private set; }

        public virtual void ToggleSettings(object o) {
            SettingsVisible = !SettingsVisible;
        }

        public virtual void Hide(object o) {
            this.IsVisible = !IsVisible;
        }
    }
}