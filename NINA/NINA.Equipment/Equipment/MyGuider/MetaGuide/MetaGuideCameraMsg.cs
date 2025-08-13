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

    public class MetaGuideCameraMsg : MetaGuideBaseMsg {

        private MetaGuideCameraMsg() {
        }

        public static MetaGuideCameraMsg Create(string[] args) {
            if (args.Length < 11) {
                return null;
            }
            try {
                return new MetaGuideCameraMsg() {
                    Exposure = int.Parse(args[5]),
                    Gain = int.Parse(args[6]),
                    MinExposure = int.Parse(args[7]),
                    MaxExposure = int.Parse(args[8]),
                    MinGain = int.Parse(args[9]),
                    MaxGain = int.Parse(args[10])
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        public int Exposure { get; private set; }
        public int Gain { get; private set; }
        public int MinExposure { get; private set; }
        public int MaxExposure { get; private set; }
        public int MinGain { get; private set; }
        public int MaxGain { get; private set; }
    }
}