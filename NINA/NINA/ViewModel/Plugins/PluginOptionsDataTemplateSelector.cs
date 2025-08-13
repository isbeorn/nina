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
using NINA.Plugin.Interfaces;
using NINA.WPF.Base.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.ViewModel.Plugins {

    internal class PluginOptionsDataTemplateSelector : DataTemplateSelector {
        public DataTemplate Default { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            var plugin = item as IPluginManifest;
            if (plugin != null) {
                var  key = plugin.Name + DataTemplatePostfix.Options;
                if (Application.Current.Resources.Contains(key)) {
                    try {
                        return (DataTemplate)Application.Current.Resources[key];
                    } catch (Exception ex) {
                        Logger.Error($"Datatemplate {key} failed to load", ex);
                    }
                    
                }
            }
            return Default;
        }
    }
}