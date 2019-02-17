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

using System;

namespace NINA.Model.MyCamera {

    internal class CameraDataToManaged {
        private int bitDepth;
        private IntPtr dataPtr;
        private int width;
        private int height;

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
        public CameraDataToManaged(IntPtr dataPtr, int width, int height, int bitDepth) {
            this.dataPtr = dataPtr;
            this.width = width;
            this.height = height;
            this.bitDepth = bitDepth;
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
                    destination[i] = *sourcePtr++;
                }
            }
            return destination;
        }
    }
}