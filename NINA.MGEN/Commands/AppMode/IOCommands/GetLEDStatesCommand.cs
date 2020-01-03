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

using FTD2XX_NET;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.MGEN.Commands.AppMode {

    public class GetLEDStatesCommand : IOCommand<LEDState> {
        public override byte SubCommandCode { get; } = 0x0a;

        protected override LEDState ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            Write(device, 1);
            var data = Read(device, 1);

            LEDS leds = (LEDS)data[0];
            return new LEDState(leds);
        }
    }

    public class LEDState : MGENResult {

        public LEDState(LEDS LEDs) : base(true) {
            this.LEDs = LEDs;
        }

        public LEDS LEDs { get; private set; }
    }
}