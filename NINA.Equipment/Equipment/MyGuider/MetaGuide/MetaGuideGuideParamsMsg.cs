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

    public class MetaGuideGuideParamsMsg : MetaGuideBaseMsg {

        private MetaGuideGuideParamsMsg() {
        }

        public static MetaGuideGuideParamsMsg Create(string[] args) {
            if (args.Length < 14) {
                return null;
            }
            try {
                return new MetaGuideGuideParamsMsg() {
                    RARate = double.Parse(args[5]),
                    DECRate = double.Parse(args[6]),
                    RAAggressiveness = double.Parse(args[7]),
                    DECAggressiveness = double.Parse(args[8]),
                    MinMove = double.Parse(args[9]),
                    MaxMove = double.Parse(args[10]),
                    DecRev = double.Parse(args[11]),
                    NorthSouthRev = int.Parse(args[12]),
                    EastWestRev = int.Parse(args[13])
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        public double RARate { get; private set; }
        public double DECRate { get; private set; }
        public double RAAggressiveness { get; private set; }
        public double DECAggressiveness { get; private set; }
        public double MinMove { get; private set; }
        public double MaxMove { get; private set; }
        public double DecRev { get; private set; }
        public int NorthSouthRev { get; private set; }
        public int EastWestRev { get; private set; }
    }
}