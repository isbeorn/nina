#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Image.ImageData {

    public class ImageProperties {

        public ImageProperties(int width, int height, int bitDepth, bool isBayered, int gain, int offset) {
            this.Width = width;
            this.Height = height;
            this.IsBayered = isBayered;
            this.BitDepth = bitDepth;
            this.Gain = gain;
            this.Offset = offset;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BitDepth { get; private set; }
        public bool IsBayered { get; private set; }
        public int Gain { get; private set; }
        public int Offset { get; private set;}
    }
}