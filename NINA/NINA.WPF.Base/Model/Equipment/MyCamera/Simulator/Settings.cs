#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Profile;
using System;

namespace NINA.WPF.Base.Model.Equipment.MyCamera.Simulator {

    public class Settings : BaseINPC {
        public Settings(PluginOptionsAccessor pluginOptionsAccessor) {
            this.pluginOptionsAccessor = pluginOptionsAccessor;
            RandomSettings = new RandomSettings(pluginOptionsAccessor);
            ImageSettings = new ImageSettings(pluginOptionsAccessor);
            SkySurveySettings = new SkySurveySettings(pluginOptionsAccessor);
            DirectorySettings = new DirectorySettings(pluginOptionsAccessor);
        }

        public CameraType Type {
            get => pluginOptionsAccessor.GetValueEnum(nameof(Type), CameraType.RANDOM);
            set {
                pluginOptionsAccessor.SetValueEnum(nameof(Type), value);
                RaisePropertyChanged();
            }
        }

        private PluginOptionsAccessor pluginOptionsAccessor;

        public RandomSettings RandomSettings { get; } 
        public ImageSettings ImageSettings { get;  }
        public SkySurveySettings SkySurveySettings { get;  }
        public DirectorySettings DirectorySettings { get; }
    }
}