#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment {

    internal class CameraTemplateSelector : DataTemplateSelector {
        public DataTemplate Default { get; set; }
        public DataTemplate Touptek { get; set; }
        public DataTemplate QhyCcd { get; set; }
        public DataTemplate Altair { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is AltairCamera) {
                return Altair;
            } else if (item is OmegonCamera || item is ToupTekCamera) {
                return Touptek;
            } else if (item is QHYCamera) {
                return QhyCcd;
            } else {
                return Default;
            }
        }
    }
}