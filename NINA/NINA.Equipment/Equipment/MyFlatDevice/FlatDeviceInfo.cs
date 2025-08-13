#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyFlatDevice {

    public class FlatDeviceInfo : DeviceInfo {
        private CoverState _coverState;

        public CoverState CoverState {
            get => _coverState;
            set {
                _coverState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LocalizedCoverState));
            }
        }

        public string LocalizedCoverState => Loc.Instance[$"LblFlatDevice{_coverState}"];
        public string LocalizedLightOnState => LightOn ? Loc.Instance["LblOn"] : Loc.Instance["LblOff"];

        private bool _lightOn;

        public bool LightOn {
            get => _lightOn;
            set {
                _lightOn = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LocalizedLightOnState));
            }
        }

        private int _brightness;

        public int Brightness {
            get => _brightness;
            set { _brightness = value; RaisePropertyChanged(); }
        }

        private bool _supportsOpenClose;

        public bool SupportsOpenClose {
            get => _supportsOpenClose;
            set {
                _supportsOpenClose = value;
                RaisePropertyChanged();
            }
        }

        private int _minBrightness;

        public int MinBrightness {
            get => _minBrightness;
            set { _minBrightness = value; RaisePropertyChanged(); }
        }

        private int _maxBrightness;

        public int MaxBrightness {
            get => _maxBrightness;
            set { _maxBrightness = value; RaisePropertyChanged(); }
        }

        private bool _supportsOnOff;

        public bool SupportsOnOff {
            get => _supportsOnOff;
            set {
                _supportsOnOff = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> supportedActions;

        public IList<string> SupportedActions {
            get => supportedActions;
            set {
                supportedActions = value;
                RaisePropertyChanged();
            }
        }
    }
}