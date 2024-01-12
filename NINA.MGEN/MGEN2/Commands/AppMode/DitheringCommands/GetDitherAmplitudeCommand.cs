#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTD2XX_NET;
using NINA.MGEN2.Commands;
using NINA.MGEN2.Commands.AppMode;

namespace NINA.MGEN2.Commands.AppMode {

    public class GetDitherAmplitudeCommand : DitheringCommand<DitherAmplitudeResult> {
        public override byte SubCommandCode { get; } = 0x02;

        protected override DitherAmplitudeResult ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var numBytes = Read(device, 1);
            if (numBytes[0] > 2) {
                var data = Read(device, numBytes[0]);
                var amplitude = data[1] / 100.0 + data[2];
                return new DitherAmplitudeResult(amplitude, true);
            }
            return new DitherAmplitudeResult(0.0, false);
        }
    }

    public class DitherAmplitudeResult : MGENResult {

        public DitherAmplitudeResult(double amplitude, bool success) : base(success) {
            this.Amplitude = amplitude;
        }

        public double Amplitude { get; }
    }
}