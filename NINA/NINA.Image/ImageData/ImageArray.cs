#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Image.Interfaces;
using System;

namespace NINA.Image.ImageData {

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
        public int[] FlatArrayInt { get => null; }

        /// <summary>
        /// Contains RAW DSLR Data if available
        /// </summary>
        public byte[] RAWData { get; private set; }

        /// <summary>
        /// Contains the type of DSLR data (e.g. cr2)
        /// </summary>
        public string RAWType { get; private set; }
    }

    public class ImageArrayInt : IImageArray {

        private ImageArrayInt() {
        }

        public ImageArrayInt(int[] flatArray) : this(flatArray, null, null) {
        }

        public ImageArrayInt(int[] flatArray, byte[] rawData, string rawType) {
            this.FlatArrayInt = flatArray;
            this.RAWData = rawData;
            this.RAWType = rawType;
        }

        private ushort[] cachedFlatArray;
        public ushort[] FlatArray {
            get {
                if (cachedFlatArray != null)
                    return cachedFlatArray;

                if (FlatArrayInt == null)
                    return null;

                cachedFlatArray = new ushort[FlatArrayInt.Length];
                for (int i = 0; i < FlatArrayInt.Length; i++) {
                    // Ensure the conversion is safe
                    if (FlatArrayInt[i] < 0 || FlatArrayInt[i] > ushort.MaxValue) {
                        cachedFlatArray[i] = ushort.MaxValue;
                    } else {
                        cachedFlatArray[i] = (ushort)FlatArrayInt[i];
                    }
                }

                return cachedFlatArray;
            }
        }

        public int[] FlatArrayInt { get; private set; }

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