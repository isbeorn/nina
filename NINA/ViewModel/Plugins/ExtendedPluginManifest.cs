#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NINA.ViewModel.Plugins {

    public class ExtendedPluginManifest : PluginManifest, INotifyPropertyChanged {

        public ExtendedPluginManifest(IPluginManifest manifest) {
            this.Author = manifest.Author;
            this.Descriptions = manifest.Descriptions;
            this.Identifier = manifest.Identifier;
            this.Homepage = manifest.Homepage;
            this.ChangelogURL = manifest.ChangelogURL;
            this.Installer = manifest.Installer;
            this.License = manifest.License;
            this.LicenseURL = manifest.LicenseURL;
            this.MinimumApplicationVersion = manifest.MinimumApplicationVersion;
            this.Name = manifest.Name;
            this.Repository = manifest.Repository;
            this.Tags = manifest.Tags;
            this.Version = manifest.Version;
            this.State = PluginState.NotInstalled;
        }

        private PluginState state;

        public PluginState State {
            get => state;
            set {
                state = value;
                RaisePropertyChanged();
            }
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}