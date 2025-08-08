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
using NINA.Profile;

namespace NINA.WPF.Base.Model.Equipment.MyCamera.Simulator {

    public class RandomSettings : BaseINPC {

        public RandomSettings(Profile.PluginOptionsAccessor pluginOptionsAccessor) {
            this.pluginOptionsAccessor = pluginOptionsAccessor;
        }

        private object lockObj = new object();

        private PluginOptionsAccessor pluginOptionsAccessor;

        public int ImageWidth {
            get => pluginOptionsAccessor.GetValueInt32(nameof(ImageWidth), 640);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueInt32(nameof(ImageWidth), value);
                }
                RaisePropertyChanged();
            }
        }

        public int ImageHeight {
            get => pluginOptionsAccessor.GetValueInt32(nameof(ImageHeight), 480);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueInt32(nameof(ImageHeight), value);
                }
                RaisePropertyChanged();
            }
        }

        public int ImageMean {
            get => pluginOptionsAccessor.GetValueInt32(nameof(ImageMean), 5000);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueInt32(nameof(ImageMean), value);
                }
                RaisePropertyChanged();
            }
        }

        public int ImageStdDev {
            get => pluginOptionsAccessor.GetValueInt32(nameof(ImageStdDev), 100);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueInt32(nameof(ImageStdDev), value);
                }
                RaisePropertyChanged();
            }
        }
    }
}