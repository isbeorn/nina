#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Model.ImageData {

    public class ImageArray : IImageArray {

        private ImageArray() {
        }

        public ImageArray(ushort[] flatArray) : this(flatArray, null, null) {
        }

        public ImageArray(ushort[] flatArray, byte[] rawData, string rawType) {
            this.FlatArray = flatArray;
            this.RAWData = rawData;
            this.RAWType = rawType;
        }

        public ushort[] FlatArray { get; private set; }

        /// <summary>
        /// Contains RAW DSLR Data if available
        /// </summary>
        public byte[] RAWData { get; private set; }

        /// <summary>
        /// Contains the type of DSLR data (e.g. cr2)
        /// </summary>
        public string RAWType { get; private set; }
    }
}