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

    public class SkySurveySettings : BaseINPC {
        private PluginOptionsAccessor pluginOptionsAccessor;

        public SkySurveySettings(PluginOptionsAccessor pluginOptionsAccessor) {
            this.pluginOptionsAccessor = pluginOptionsAccessor;
        }

        private object lockObj = new object();

        public int DecError {
            get => pluginOptionsAccessor.GetValueInt32(nameof(DecError), 0);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueInt32(nameof(DecError), value);
                }
                RaisePropertyChanged();
            }
        }

        public int RAError {
            get => pluginOptionsAccessor.GetValueInt32(nameof(RAError), 0);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueInt32(nameof(RAError), value);
                }
                RaisePropertyChanged();
            }
        }

        public int AzShift {
            get => pluginOptionsAccessor.GetValueInt32(nameof(AzShift), 0);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueInt32(nameof(AzShift), value);
                }
                RaisePropertyChanged();
            }
        }

        public int AltShift {
            get => pluginOptionsAccessor.GetValueInt32(nameof(AltShift), 0);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueInt32(nameof(AltShift), value);
                }
                RaisePropertyChanged();
            }
        }

        public double FieldOfView {
            get => pluginOptionsAccessor.GetValueDouble(nameof(FieldOfView), 1);
            set {
                lock (lockObj) {
                    pluginOptionsAccessor.SetValueDouble(nameof(FieldOfView), value);
                }
                RaisePropertyChanged();
            }
        }
    }
}