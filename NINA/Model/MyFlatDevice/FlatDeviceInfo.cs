#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

namespace NINA.Model.MyFlatDevice {

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

        public string LocalizedCoverState => Locale.Loc.Instance[$"LblFlatDevice{_coverState}"];
        public string LocalizedLightOnState => LightOn ? Locale.Loc.Instance["LblOn"] : Locale.Loc.Instance["LblOff"];

        private bool _lightOn;

        public bool LightOn {
            get => _lightOn;
            set {
                _lightOn = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LocalizedLightOnState));
            }
        }

        private double _brightness;

        public double Brightness {
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
    }
}