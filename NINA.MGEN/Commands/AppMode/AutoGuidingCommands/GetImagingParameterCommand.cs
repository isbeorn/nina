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

    public class GetImagingParameterCommand : AutoGuidingCommand<ImagingParameter> {
        public override byte SubCommandCode { get; } = 0x92;

        protected override ImagingParameter ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var data = Read(device, 5);
            if (data[0] == 0x00) {
                var gain = data[1];
                var exposureTime = ToUShort(data[2], data[3]);
                var threshold = data[4];

                return new ImagingParameter(gain, exposureTime, threshold);
            } else if (data[0] == 0xf1) {
                throw new AnotherCommandInProgressException();
            } else if (data[0] == 0xf0) {
                throw new UILockedException();
            } else {
                throw new UnexpectedReturnCodeException();
            }
        }
    }

    public class ImagingParameter : MGENResult {

        public ImagingParameter(byte gain, ushort exposureTime, byte threshold) : base(true) {
            this.Gain = gain;
            this.ExposureTime = exposureTime;
            this.Threshold = threshold;
        }

        public byte Gain { get; private set; }
        public ushort ExposureTime { get; private set; }
        public byte Threshold { get; private set; }
    }
}
