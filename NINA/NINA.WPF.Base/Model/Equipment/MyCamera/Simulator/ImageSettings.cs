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
using System.IO;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile;

namespace NINA.WPF.Base.Model.Equipment.MyCamera.Simulator {

    public class ImageSettings : BaseINPC {
        private PluginOptionsAccessor pluginOptionsAccessor;

        public ImageSettings(PluginOptionsAccessor pluginOptionsAccessor) {
            this.pluginOptionsAccessor = pluginOptionsAccessor;
        }

        public bool IsBayered {
            get => pluginOptionsAccessor.GetValueBoolean(nameof(IsBayered), false);
            set {
                pluginOptionsAccessor.SetValueBoolean(nameof(IsBayered), value);
                RaisePropertyChanged();
            }
        }

        public string ImagePath {
            get => pluginOptionsAccessor.GetValueString(nameof(ImagePath), string.Empty);
            set {
                pluginOptionsAccessor.SetValueString(nameof(ImagePath), value);
                RaisePropertyChanged();
            }
        }

    }
}