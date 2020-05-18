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

    public class GetStarDataCommand : AutoGuidingCommand<StarData> {
        private byte starIndex;

        public GetStarDataCommand(byte starIndex) {
            this.starIndex = starIndex;
        }

        public override byte SubCommandCode { get; } = 0x39;

        protected override StarData ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var data = Read(device, 1);
            if (data[0] == 0x00) {
                Write(device, starIndex);
                var mirror = Read(device, 1);
                if (mirror[0] == starIndex) {
                    var starDataArray = Read(device, 8);
                    var positionX = ToUShort(starDataArray[0], starDataArray[1]);
                    var positionY = ToUShort(starDataArray[2], starDataArray[3]);
                    var brightness = ToUShort(starDataArray[4], starDataArray[5]);
                    var pixels = starDataArray[6];
                    var peak = starDataArray[7];
                    return new StarData(positionX, positionY, brightness, pixels, peak);
                } else {
                    throw new Exception("Invalid Star Index");
                }
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

    public class StarData : MGENResult {

        public StarData(ushort positionX, ushort positionY, ushort brightness, byte pixels, byte peak) : base(true) {
            PositionX = positionX;
            PositionY = positionY;
            Brightness = brightness;
            Pixels = pixels;
            Peak = peak;
        }

        public ushort PositionX { get; private set; }
        public ushort PositionY { get; private set; }
        public ushort Brightness { get; private set; }
        public byte Pixels { get; private set; }
        public byte Peak { get; private set; }
    }
}
