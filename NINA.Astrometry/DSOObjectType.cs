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

namespace NINA.Astrometry {

    public class DSOObjectType : BaseINPC {

        public DSOObjectType(string s) {
            Name = s;
            Selected = false;
        }

        private bool _selected;

        public bool Selected {
            get => _selected;
            set {
                _selected = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name {
            get => _name;
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }
    }
}