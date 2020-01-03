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
using NINA.MGEN.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.MGEN.Commands.AppMode {

    public class FirmwareVersionCommand : AppModeCommand<FirmwareVersion> {
        public override byte CommandCode { get; } = 0x03;
        public override byte AcknowledgeCode { get; } = 0x03;

        public override FirmwareVersion Execute(IFTDI device) {
            Write(device, CommandCode);
            var data = Read(device, 3);

            if (data[0] == AcknowledgeCode) {
                var version = Convert.ToString(data[2], 16) + "." + Convert.ToString(data[1], 16);
                return new FirmwareVersion(true, version);
            } else {
                //throw generic error
                throw new UnexpectedReturnCodeException();
            }
        }
    }

    public class FirmwareVersion : MGENResult {

        public FirmwareVersion(bool success, string version) : base(success) {
            this.Version = version;
        }

        public string Version { get; private set; }
    }
}