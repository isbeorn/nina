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
