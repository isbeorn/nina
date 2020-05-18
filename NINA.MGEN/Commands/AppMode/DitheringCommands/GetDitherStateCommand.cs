#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

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
using NINA.MGEN.Exceptions;

namespace NINA.MGEN.Commands.AppMode {

    public class GetDitherStateCommand : DitheringCommand<StartDitherResult> {
        public override byte SubCommandCode { get; } = 0x00;

        protected override StartDitherResult ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var data = Read(device, 1);
            var state = (DitherState)data[0];
            return new StartDitherResult(state.HasFlag(DitherState.RDActive), true);
        }

        [Flags]
        public enum DitherState : byte {
            RDActive = 0b_0001_0000,
            RDNextEnabled = 0b_0100_0000,
            RDLastEnabled = 0b_1000_0000
        }
    }

    public class StartDitherResult : MGENResult {

        public StartDitherResult(bool ditherActive, bool success) : base(success) {
            this.Dithering = ditherActive;
        }

        public bool Dithering { get; }
    }
}
