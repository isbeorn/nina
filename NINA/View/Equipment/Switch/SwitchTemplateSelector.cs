#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

namespace NINA.View.Equipment.Switch {

    internal class SwitchTemplateSelector : DataTemplateSelector {
        public DataTemplate Writable { get; set; }
        public DataTemplate WritableBoolean { get; set; }
        public DataTemplate ReadOnly { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is Model.MySwitch.IWritableSwitch) {
                var s = (Model.MySwitch.IWritableSwitch)item;
                if (s.Minimum == 0 && s.Maximum == 1) {
                    return WritableBoolean;
                } else {
                    return Writable;
                }
            } else {
                return ReadOnly;
            }
        }
    }
}