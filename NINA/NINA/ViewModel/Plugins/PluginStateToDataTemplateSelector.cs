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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.ViewModel.Plugins {

    public class PluginStateToDataTemplateSelector : DataTemplateSelector {
        public DataTemplate Installed { get; set; }
        public DataTemplate NotInstalled { get; set; }
        public DataTemplate UpdateAvailable { get; set; }
        public DataTemplate InstalledAndRequiresRestart { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is PluginState state) {
                switch (state) {
                    case PluginState.Installed:
                        return Installed;

                    case PluginState.UpdateAvailable:
                        return UpdateAvailable;

                    case PluginState.InstalledAndRequiresRestart:
                        return InstalledAndRequiresRestart;

                    default:
                        return NotInstalled;
                }
            }
            return NotInstalled;
        }
    }
}