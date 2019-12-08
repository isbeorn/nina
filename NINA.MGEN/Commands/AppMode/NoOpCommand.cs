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

using FTD2XX_NET;
using NINA.MGEN.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.MGEN.Commands.AppMode {

    public class NoOpCommand : AppModeCommand<MGENResult> {
        public override byte CommandCode { get; } = 0xff;
        public override byte AcknowledgeCode { get; } = 0xff;
        public byte NotAcknowledgeCode { get; } = 0x00;

        public override MGENResult Execute(IFTDI device) {
            Write(device, CommandCode);
            var data = Read(device, 1);
            if (data[0] != AcknowledgeCode && data[0] != NotAcknowledgeCode) {
                return null;
            }
            return new MGENResult(data[0] == AcknowledgeCode);
        }
    }
}