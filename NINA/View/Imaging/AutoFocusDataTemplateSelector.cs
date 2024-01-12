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
using System;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Imaging {
    public class AutoFocusDataTemplateSelector : DataTemplateSelector {
        private readonly ResourceDictionary resources;

        public AutoFocusDataTemplateSelector(ResourceDictionary resources) {
            this.resources = resources;
        }

        public AutoFocusDataTemplateSelector() : this(Application.Current.Resources) {
        }

        public DataTemplate BuiltIn { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item != null) {
                var key = item.GetType().FullName + "_Dockable";
                if (resources.Contains(key)) {
                    try {
                        return (DataTemplate)resources[key];
                    } catch (Exception ex) {
                        Logger.Error($"Datatemplate {key} failed to load", ex);
                    }

                }
            }
            return BuiltIn;
            
        }
    }
}
