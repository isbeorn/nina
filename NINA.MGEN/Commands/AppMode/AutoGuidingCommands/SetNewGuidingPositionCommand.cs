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
using System.Threading.Tasks;

namespace NINA.MGEN.Commands.AppMode {

    public class SetNewGuidingPositionCommand : AutoGuidingCommand<MGENResult> {
        private StarData starData;

        public SetNewGuidingPositionCommand(StarData starData) {
            this.starData = starData;
        }

        public override byte SubCommandCode { get; } = 0x3f;

        protected override MGENResult ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var data = Read(device, 1);
            if (data[0] == 0x00) {
                var posX = GetBytes(starData.PositionX);
                var posY = GetBytes(starData.PositionY);
                var parameters = new byte[] { posX[0], posX[1], posY[0], posY[1] };
                Write(device, parameters);

                return new MGENResult(true);
            } else if (data[0] == 0xf2) {
                throw new CameraIsOffException();
            } else if (data[0] == 0xf3) {
                throw new AutoGuidingActiveException();
            } else if (data[0] == 0xf1) {
                throw new AnotherCommandInProgressException();
            } else if (data[0] == 0xf0) {
                throw new UILockedException();
            } else {
                throw new UnexpectedReturnCodeException();
            }
        }
    }
}
