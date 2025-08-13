#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Plugin.ManifestDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.ViewModel.Plugins {

    public class PluginInstallerDescriptionTemplateSelector : DataTemplateSelector {
        public DataTemplate DLL { get; set; }
        public DataTemplate Archive { get; set; }
        public DataTemplate Setup { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is InstallerType type) {
                switch (type) {
                    case InstallerType.DLL:
                        return DLL;

                    case InstallerType.ARCHIVE:
                        return Archive;

                    default:
                        return Setup;
                }
            }
            return Setup;
        }
    }
}