#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using System;

namespace NINA.Profile {

    public class ProfileMeta : BaseINPC {
        public Guid Id { get; set; }
        public DateTime LastUsed { get; set; }
        private string name;

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        public string Location { get; set; }
        private bool isActive;

        public bool IsActive {
            get => isActive;
            set {
                isActive = value;
                RaisePropertyChanged();
            }
        }
    }
}
