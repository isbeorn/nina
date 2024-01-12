#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FTD2XX_NET;
using NINA.MGEN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.MGEN2.Commands.AppMode {

    public class GetLEDStatesCommand : IOCommand<LEDResult> {
        public override byte SubCommandCode { get; } = 0x0a;

        protected override LEDResult ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            Write(device, 1);
            var data = Read(device, 1);

            LEDS leds = (LEDS)data[0];
            return new LEDResult(leds);
        }
    }

    public class LEDResult : MGENResult {

        public LEDResult(LEDS LEDs) : base(true) {
            this.LEDs = LEDs;
        }

        public LEDS LEDs { get; private set; }
    }

    [Flags]
    public enum LEDS : byte {
        BLUE = 1, // Exposure Focus Line Active
        GREEN = 2, // Exposure Shutter Line Active
        UP_RED = 4, // DEC- correction active
        DOWN_RED = 8, //DEC+ correction active
        LEFT_RED = 16, // RA- correction active
        RIGHT_RED = 32 // RA+ correction active
    }
}