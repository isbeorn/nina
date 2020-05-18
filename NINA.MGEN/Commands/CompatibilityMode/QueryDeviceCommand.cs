#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
