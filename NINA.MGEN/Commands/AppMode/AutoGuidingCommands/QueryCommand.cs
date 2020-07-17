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

    public class QueryCommand : AutoGuidingCommand<GuidingResult> {

        [Flags]
        public enum QueryCommandFlag : byte {
            None = 0,
            AutoguidingState = 2,
            FrameInfo = 4,
            All = 6
        }

        private QueryCommandFlag flags;

        public QueryCommand(QueryCommandFlag flags) : base() {
            this.flags = flags;
        }

        public override byte SubCommandCode { get; } = 0x10;

        /// <summary>
        /// Frame index is an increasing number as new CCD frames are read.
        /// The lower 6 bits are provided. If the Camera can see and evaluate a star in it, bit 6 is set (otherwise it’szero).Bit7isalways zero.
        /// Raw CCD coordinates show where the last star has been measured. (If the ‘star present’ flag is zero, this value is the lastest one when that flag was one.)
        /// It’s a signed 16.8 bit fixed point number. (The lower 8 bits are the fractional part, the next 15 bits is the integer part and bit 23 is the sign.).
        /// (Note that the CCD pixels are not exactlysquare, 4.85 um horizontal (X) and 4.65 um vertical (Y)!
        /// For binningmodes, thesesizesaremultiplied.) Drift is the same value as the MGen uses for autoguiding and display.
        /// It’s only valid ifAG is active. It holds the latest measured value as is for the raw coordinates. The value is a signed 8.8 bit fixed point number.
        /// (Note that the drift values are transformed and corrected so that the CCD had 4.85 x 4.85 um square pixels (same forbinned).)
        /// </summary>
        /// <param name="device"></param>
        protected override GuidingResult ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var data = Read(device, 1);
            if (data[0] == 0x00) {
                Write(device, (byte)flags);
                var guiderActive = false;
                FrameInfo frameInfo = null;
                if (flags.HasFlag(QueryCommandFlag.AutoguidingState)) {
                    var agState = Read(device, 1);
                    if (agState[0] == 0x01) {
                        guiderActive = true;
                    }
                }
                if (flags.HasFlag(QueryCommandFlag.FrameInfo)) {
                    var frameInfoData = Read(device, 12);

                    var frameIndex = frameInfoData[0];

                    var posX = ThreeBytesToInt(frameInfoData[1], frameInfoData[2], frameInfoData[3]);
                    var posY = ThreeBytesToInt(frameInfoData[4], frameInfoData[5], frameInfoData[6]);

                    var d_RA = ToShort(frameInfoData[7], frameInfoData[8]);
                    var d_Dec = ToShort(frameInfoData[9], frameInfoData[10]);
                    var peak = frameInfoData[11];

                    frameInfo = new FrameInfo(frameIndex, posX, posY, d_RA, d_Dec);
                }
                return new GuidingResult(guiderActive, frameInfo);
            } else if (data[0] == 0xf0) {
                return new GuidingResult(false, null);
            } else {
                throw new UnexpectedReturnCodeException();
            }
        }
    }

    public class FrameInfo {

        public FrameInfo(byte frameIndex, int positionX, int positionY, short distanceRA, short distanceDec) {
            FrameIndex = frameIndex;
            PositionX = positionX;
            PositionY = positionY;
            DriftRA = distanceRA;
            DriftDec = distanceDec;
        }

        public byte FrameIndex { get; private set; }
        public int PositionX { get; private set; }
        public int PositionY { get; private set; }
        public short DriftRA { get; private set; }
        public short DriftDec { get; private set; }
    }

    public class GuidingResult : MGENResult {

        public GuidingResult(bool activeGuider, FrameInfo frameInfo) : base(true) {
            AutoGuiderActive = activeGuider;
            FrameInfo = frameInfo;
        }

        public bool AutoGuiderActive { get; private set; }
        public FrameInfo FrameInfo { get; private set; }
    }
}