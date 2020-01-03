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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.MGEN.Commands.CompatibilityMode {

    public class QueryDeviceCommand : CompatibilityModeCommand<QueryDevice> {
        public override byte CommandCode { get; } = 0xaa;

        public override byte AcknowledgeCode { get; } = 0x55;

        private byte[] BootModeAnswer = { 0x55, 0x03, 0x01, 0x80, 0x01 };
        private byte[] AppModeAnswer = { 0x55, 0x03, 0x01, 0x80, 0x02 };

        public override QueryDevice Execute(IFTDI device) {
            Write(device, new byte[] { CommandCode, 0x01, 0x01 });
            var data = Read(device, 5);
            var isBootMode = Enumerable.SequenceEqual(BootModeAnswer, data);
            var isAppMode = Enumerable.SequenceEqual(AppModeAnswer, data);
            if (!isBootMode && !isAppMode) {
                return null;
            }
            return new QueryDevice(isBootMode);
        }
    }

    public class QueryDevice : MGENResult {

        public QueryDevice(bool isBootMode) : base(true) {
            IsBootMode = isBootMode;
        }

        public bool IsBootMode { get; private set; }
    }
}