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

    public class StartStarSearchCommand : AutoGuidingCommand<StarSearchResult> {
        public new uint Timeout { get; } = 15000;
        private byte gain;
        private ushort exposureTime;

        public StartStarSearchCommand(byte gain, ushort exposureTime) {
            if (gain < 2 || gain > 9) { throw new ArgumentException($"Gain needs to be in the range of 2-9, but was {gain}"); }
            if (exposureTime < 50 || exposureTime > 4000) { throw new ArgumentException($"Exposure Time needs to be in the range of 50-4000ms, but was {exposureTime}"); }
            this.gain = gain;
            this.exposureTime = exposureTime;
        }

        public override byte SubCommandCode { get; } = 0x30;

        protected override StarSearchResult ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var data = Read(device, 1);
            if (data[0] == 0x00) {
                var exposureTimeBytes = GetBytes(exposureTime);
                //For exposure time LSB has to be first
                var parameters = new byte[] { gain, exposureTimeBytes[0], exposureTimeBytes[1] };
                Write(device, parameters);

                var numStars = Read(device, 1);

                return new StarSearchResult(numStars[0]);
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

    public class StarSearchResult : MGENResult {

        public StarSearchResult(byte numberOfStars) : base(true) {
            NumberOfStars = numberOfStars;
        }

        public byte NumberOfStars { get; private set; }
    }
}