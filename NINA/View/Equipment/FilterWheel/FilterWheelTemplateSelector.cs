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
using NINA.Equipment.Equipment.MyFilterWheel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment {

    internal class FilterWheelTemplateSelector : DataTemplateSelector {
        public DataTemplate Default { get; set; }
        public DataTemplate Zwo { get; set; }
        public DataTemplate FailedToLoadTemplate { get; set; }

        public string Postfix { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is ASIFilterWheel) {
                return Zwo;
            } else {
                var templateKey = item?.GetType().FullName + Postfix;
                if (item != null && Application.Current.Resources.Contains(templateKey)) {
                    try {
                        return (DataTemplate)Application.Current.Resources[templateKey];
                    } catch (Exception ex) {
                        Logger.Error($"Datatemplate {templateKey} failed to load", ex);
                        return FailedToLoadTemplate;
                    }
                } else {
                    return Default;
                }
            }
        }
    }
}