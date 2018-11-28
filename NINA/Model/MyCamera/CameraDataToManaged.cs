using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    internal class CameraDataToManaged {
        private int bitDepth;
        private int width;
        private int height;

        public int Size {
            get {
                return width * height * 2;
            }
        }

        public CameraDataToManaged(int width, int height, int bitDepth) {
            this.width = width;
            this.height = height;
            this.bitDepth = bitDepth;
        }

        public ushort[] GetData(Action<IntPtr> getDataFromCamera) {
            var pointer = Marshal.AllocHGlobal(Size);

            getDataFromCamera(pointer);

            ushort[] arr;
            if (bitDepth > 8) {
                arr = CopyToUShort(pointer, Size / 2);
            } else {
                arr = Copy8BitToUShort(pointer, Size / 2);
            }
            Marshal.FreeHGlobal(pointer);

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