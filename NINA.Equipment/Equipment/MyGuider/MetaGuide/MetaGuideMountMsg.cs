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
using System;

namespace NINA.Equipment.Equipment.MyGuider.MetaGuide {

    public class MetaGuideMountMsg : MetaGuideBaseMsg {

        private MetaGuideMountMsg() {
        }

        public static MetaGuideMountMsg Create(string[] args) {
            if (args.Length < 6) {
                return null;
            }
            try {
                return new MetaGuideMountMsg() {
                    MountName = args[5]
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        public string MountName { get; private set; }
    }
}