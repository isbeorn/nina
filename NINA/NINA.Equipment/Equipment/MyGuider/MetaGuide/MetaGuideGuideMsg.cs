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

    public class MetaGuideGuideMsg : MetaGuideBaseMsg {

        private MetaGuideGuideMsg() {
        }

        public static MetaGuideGuideMsg Create(string[] args) {
            if (args.Length < 10) {
                return null;
            }
            try {
                return new MetaGuideGuideMsg() {
                    SystemTimeInSeconds = double.Parse(args[5]),
                    SecondsSinceStart = double.Parse(args[6]),
                    WestPulse = int.Parse(args[7]),
                    NorthPulse = int.Parse(args[8]),
                    CalibrationState = (CalibrationState)int.Parse(args[9])
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        public double SystemTimeInSeconds { get; private set; }
        public double SecondsSinceStart { get; private set; }
        public int WestPulse { get; private set; }
        public int NorthPulse { get; private set; }
        public CalibrationState CalibrationState { get; private set; }
    }
}