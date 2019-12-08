#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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