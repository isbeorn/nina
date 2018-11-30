using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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