#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Model.MyCamera {

    internal class CameraDataToManaged {
        private int bitDepth;
        private IntPtr dataPtr;
        private int width;
        private int height;
        private int scaling = 0;

        private int Size {
            get {
                return width * height;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dataPtr">Pointer to the image data</param>
        /// <param name="width">Image dimension width</param>
        /// <param name="height">Image dimension height</param>
        /// <param name="bitDepth">Image data bit depth</param>
        /// <param name="bitScaling">True: Shift data bits left to scale to 16 bit. False: Pure raw data</param>
        public CameraDataToManaged(IntPtr dataPtr, int width, int height, int bitDepth, bool bitScaling) {
            this.dataPtr = dataPtr;
            this.width = width;
            this.height = height;
            this.bitDepth = bitDepth;
            if (bitScaling) {
                this.scaling = 16 - bitDepth;
            }
        }

        /// <summary>
        /// Copies the data from the data pointer to an actual ushort array
        /// </summary>
        /// <returns>image data array</returns>
        public ushort[] GetData() {
            ushort[] arr;
            if (bitDepth > 8) {
                arr = CopyToUShort(dataPtr, Size);
            } else {
                arr = Copy8BitToUShort(dataPtr, Size);
            }
            return arr;
        }

        private ushort[] Copy8BitToUShort(IntPtr source, int length) {
            var destination = new ushort[length];
            unsafe {
                var sourcePtr = (byte*)source;
                for (int i = 0; i < length; ++i) {
                    destination[i] = *sourcePtr++;
                }
            }
            return destination;
        }

        private ushort[] CopyToUShort(IntPtr source, int length) {
            var destination = new ushort[length];
            unsafe {
                var sourcePtr = (ushort*)source;
                for (int i = 0; i < length; ++i) {
                    destination[i] = (ushort)((*sourcePtr++) << scaling);
                }
            }
            return destination;
        }
    }
}