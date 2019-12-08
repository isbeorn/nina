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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.MGEN.Commands.AppMode {

    public class SetImagingParameterCommand : AutoGuidingCommand<MGENResult> {
        private byte gain;
        private ushort exposureTime;
        private byte threshold;

        public SetImagingParameterCommand(byte gain, ushort exposureTime, byte threshold) {
            if (gain < 2 || gain > 9) { throw new ArgumentException($"Gain needs to be in the range of 2-9, but was {gain}"); }
            if (exposureTime < 50 || exposureTime > 4000) { throw new ArgumentException($"Exposure Time needs to be in the range of 50-4000ms, but was {exposureTime}"); }
            if (threshold < 1 || threshold > 99) { throw new ArgumentException($"Threshold needs to be in the range of 1-99, but was {threshold}"); }
            this.gain = gain;
            this.exposureTime = exposureTime;
            this.threshold = threshold;
        }

        public override byte SubCommandCode { get; } = 0x91;

        protected override MGENResult ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var data = Read(device, 5);
            if (data[0] == 0x00) {
                var exposureTimeBytes = GetBytes(exposureTime);
                //For exposure time LSB has to be first
                var parameters = new byte[] { gain, exposureTimeBytes[0], exposureTimeBytes[1], threshold };
                Write(device, parameters);

                return new MGENResult(true);
            } else if (data[0] == 0xf2) {
                throw new CameraIsOffException();
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