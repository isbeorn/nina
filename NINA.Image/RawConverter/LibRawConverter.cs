#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Image.RawConverter {

    internal class LibRawConverter : IRawConverter {
        private const string LibRawDllName = "libraw_0_22_1.dll";

        // Offsets are from LibRaw 0.22.1's public libraw_types.h and must be updated with the versioned DLL.
        private const int ImageSizesOffset = 8;
        private const int RawDataOffset = 193768;
        private const int RawImageOffset = 8;

        private static readonly object loadLock = new object();
        private static bool dllLoaded;

        private readonly IImageDataFactory imageDataFactory;

        public LibRawConverter(IImageDataFactory imageDataFactory) {
            this.imageDataFactory = imageDataFactory;
            EnsureDllLoaded();
        }

        public Task<IImageData> Convert(
            MemoryStream s,
            int bitDepth,
            string rawType,
            ImageMetaData metaData,
            CancellationToken token = default) {
            return Task.Run(() => {
                using (MyStopWatch.Measure()) {
                    token.ThrowIfCancellationRequested();

                    var rawBytes = s.ToArray();
                    var handle = GCHandle.Alloc(rawBytes, GCHandleType.Pinned);
                    var processor = IntPtr.Zero;
                    try {
                        processor = LibRawNative.Init(0);
                        if (processor == IntPtr.Zero) {
                            throw new InvalidOperationException("LibRaw initialization failed.");
                        }

                        ThrowIfError(
                            LibRawNative.OpenBuffer(processor, handle.AddrOfPinnedObject(), (UIntPtr)rawBytes.Length),
                            "LibRaw open buffer");

                        token.ThrowIfCancellationRequested();

                        ThrowIfError(LibRawNative.Unpack(processor), "LibRaw unpack");

                        token.ThrowIfCancellationRequested();

                        return CreateImageData(processor, rawBytes, rawType, bitDepth, metaData);
                    } finally {
                        if (processor != IntPtr.Zero) {
                            LibRawNative.Close(processor);
                        }

                        if (handle.IsAllocated) {
                            handle.Free();
                        }
                    }
                }
            }, token);
        }

        private static void EnsureDllLoaded() {
            lock (loadLock) {
                if (dllLoaded) {
                    return;
                }

                DllLoader.LoadDll(Path.Combine("Libraw", LibRawDllName));
                dllLoaded = true;
            }
        }

        private IImageData CreateImageData(IntPtr processor, byte[] rawBytes, string rawType, int bitDepth, ImageMetaData metaData) {
            var sizes = Marshal.PtrToStructure<LibRawImageSizes>(IntPtr.Add(processor, ImageSizesOffset));
            var frame = GetActiveFrame(sizes);

            var rawImage = ReadRawDataPointer(processor, RawImageOffset);
            if (rawImage != IntPtr.Zero) {
                var pixels = CopyUshortFrame(rawImage, frame);
                return CreateImageData(pixels, rawBytes, rawType, frame.Width, frame.Height, bitDepth, metaData);
            }

            throw new NotSupportedException("LibRaw did not return an unpacked CFA RAW image buffer.");
        }

        private IImageData CreateImageData(ushort[] pixels, byte[] rawBytes, string rawType, int width, int height, int bitDepth, ImageMetaData metaData) {
            var imageArray = new ImageArray(flatArray: pixels, rawData: rawBytes, rawType: rawType);
            return imageDataFactory.CreateBaseImageData(
                imageArray: imageArray,
                width: width,
                height: height,
                bitDepth: bitDepth,
                isBayered: true,
                metaData: metaData);
        }

        private static ActiveFrame GetActiveFrame(LibRawImageSizes sizes) {
            var sourceWidth = sizes.RawWidth;
            var sourceHeight = sizes.RawHeight;
            var rowStride = sizes.RawPitch > 0 ? Math.Max(sourceWidth, (int)sizes.RawPitch / sizeof(ushort)) : sourceWidth;
            var width = FirstPositive(sizes.Width, sizes.IWidth, sourceWidth);
            var height = FirstPositive(sizes.Height, sizes.IHeight, sourceHeight);
            var left = Math.Min(sizes.LeftMargin, rowStride);
            var top = Math.Min(sizes.TopMargin, sourceHeight);

            width = Math.Min(width, rowStride - left);
            height = Math.Min(height, sourceHeight - top);

            if (width <= 0 || height <= 0 || rowStride <= 0) {
                throw new InvalidOperationException("LibRaw returned invalid RAW image dimensions.");
            }

            return new ActiveFrame(left, top, width, height, rowStride);
        }

        private static int FirstPositive(params ushort[] values) {
            foreach (var value in values) {
                if (value > 0) {
                    return value;
                }
            }

            return 0;
        }

        private static IntPtr ReadRawDataPointer(IntPtr processor, int rawDataFieldOffset) {
            return Marshal.ReadIntPtr(processor, RawDataOffset + rawDataFieldOffset);
        }

        private static void ThrowIfError(int result, string operation) {
            if (result == 0) {
                return;
            }

            var message = Marshal.PtrToStringAnsi(LibRawNative.StrError(result));
            if (string.IsNullOrWhiteSpace(message)) {
                message = $"LibRaw error {result}";
            }

            throw new InvalidOperationException($"{operation} failed: {message}");
        }

        private static unsafe ushort[] CopyUshortFrame(IntPtr image, ActiveFrame frame) {
            var pixels = new ushort[frame.Width * frame.Height];
            var source = (ushort*)image.ToPointer();
            fixed (ushort* destination = pixels) {
                for (var y = 0; y < frame.Height; y++) {
                    var sourceRow = source + ((frame.Top + y) * frame.RowStride) + frame.Left;
                    var destinationRow = destination + (y * frame.Width);
                    Buffer.MemoryCopy(sourceRow, destinationRow, frame.Width * sizeof(ushort), frame.Width * sizeof(ushort));
                }
            }

            return pixels;
        }

        [StructLayout(LayoutKind.Explicit, Size = 184)]
        private struct LibRawImageSizes {
            [FieldOffset(0)]
            public ushort RawHeight;

            [FieldOffset(2)]
            public ushort RawWidth;

            [FieldOffset(4)]
            public ushort Height;

            [FieldOffset(6)]
            public ushort Width;

            [FieldOffset(8)]
            public ushort TopMargin;

            [FieldOffset(10)]
            public ushort LeftMargin;

            [FieldOffset(12)]
            public ushort IHeight;

            [FieldOffset(14)]
            public ushort IWidth;

            [FieldOffset(16)]
            public uint RawPitch;
        }

        private readonly struct ActiveFrame {
            public ActiveFrame(int left, int top, int width, int height, int rowStride) {
                Left = left;
                Top = top;
                Width = width;
                Height = height;
                RowStride = rowStride;
            }

            public int Left { get; }
            public int Top { get; }
            public int Width { get; }
            public int Height { get; }
            public int RowStride { get; }
        }

        private static class LibRawNative {

            [DllImport(LibRawDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "libraw_init")]
            public static extern IntPtr Init(uint flags);

            [DllImport(LibRawDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "libraw_open_buffer")]
            public static extern int OpenBuffer(IntPtr processor, IntPtr buffer, UIntPtr bufferSize);

            [DllImport(LibRawDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "libraw_unpack")]
            public static extern int Unpack(IntPtr processor);

            [DllImport(LibRawDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "libraw_close")]
            public static extern void Close(IntPtr processor);

            [DllImport(LibRawDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "libraw_strerror")]
            public static extern IntPtr StrError(int errorCode);
        }
    }
}
