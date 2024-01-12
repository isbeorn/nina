#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.MGEN {

    public class FrameInfo {

        public FrameInfo(uint frameIndex, float positionX, float positionY, float distanceRA, float distanceDec) {
            FrameIndex = frameIndex;
            PositionX = positionX;
            PositionY = positionY;
            DriftRA = distanceRA;
            DriftDec = distanceDec;
        }

        public uint FrameIndex { get; private set; }
        public float PositionX { get; private set; }
        public float PositionY { get; private set; }
        public float DriftRA { get; private set; }
        public float DriftDec { get; private set; }
    }
}