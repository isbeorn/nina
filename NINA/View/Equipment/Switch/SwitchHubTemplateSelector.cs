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
using NINA.Model.MySwitch;

namespace NINA.View.Equipment.Switch {

    internal class SwitchHubTemplateSelector : DataTemplateSelector {
        public DataTemplate Generic { get; set; }
        public DataTemplate Eagle { get; set; }
        public DataTemplate UltimatePowerBoxV2 { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            switch (item) {
                case Model.MySwitch.Eagle _:
                    return Eagle;

                case Model.MySwitch.UltimatePowerBoxV2 _:
                    return UltimatePowerBoxV2;

                default:
                    return Generic;
            }
        }
    }
}
