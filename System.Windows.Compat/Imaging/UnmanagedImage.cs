#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Accord.Imaging {
    /// <summary>
    /// UnmanagedImage represents an image stored in unmanaged memory
    /// This is a wrapper around OpenCV Mat that provides access to raw pixel data
    /// </summary>
    public unsafe class UnmanagedImage : IDisposable {
        private Mat mat;
        private IntPtr imageData;
        private bool ownsData;

        /// <summary>
        /// Pointer to image data in unmanaged memory
        /// </summary>
        public IntPtr ImageData => imageData;

        /// <summary>
        /// Width of the image in pixels
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Height of the image in pixels
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Stride (line size in bytes)
        /// </summary>
        public int Stride { get; private set; }

        /// <summary>
        /// Pixel format of the image
        /// </summary>
        public PixelFormat PixelFormat { get; private set; }

        private UnmanagedImage(Mat mat, bool ownsData) {
            this.mat = mat;
            this.ownsData = ownsData;
            this.imageData = mat.Data;
            this.Width = mat.Width;
            this.Height = mat.Height;
            this.Stride = (int)mat.Step();

            // Map OpenCV type to PixelFormat
            if (mat.Type() == MatType.CV_8UC1) {
                this.PixelFormat = PixelFormat.Format8bppIndexed;
            } else if (mat.Type() == MatType.CV_16UC1) {
                this.PixelFormat = PixelFormat.Format16bppGrayScale;
            } else if (mat.Type() == MatType.CV_8UC3) {
                this.PixelFormat = PixelFormat.Format24bppRgb;
            } else if (mat.Type() == MatType.CV_8UC4) {
                this.PixelFormat = PixelFormat.Format32bppArgb;
            } else {
                this.PixelFormat = PixelFormat.Format24bppRgb;
            }
        }

        /// <summary>
        /// Creates an UnmanagedImage from an OpenCV Mat
        /// </summary>
        public static UnmanagedImage FromMat(Mat mat) {
            return new UnmanagedImage(mat, false);
        }

        /// <summary>
        /// Creates an UnmanagedImage from a Bitmap
        /// </summary>
        public static UnmanagedImage FromManagedImage(Bitmap bitmap) {
            // Clone the Mat to ensure it's not disposed when the Bitmap is disposed
            Mat mat = bitmap.GetMat();
            return new UnmanagedImage(mat, true);
        }

        /// <summary>
        /// Creates an UnmanagedImage from BitmapData
        /// </summary>
        public UnmanagedImage(BitmapData bitmapData) {
            this.ownsData = false;
            this.imageData = bitmapData.Scan0;
            this.Width = bitmapData.Width;
            this.Height = bitmapData.Height;
            this.Stride = bitmapData.Stride;
            this.PixelFormat = bitmapData.PixelFormat;
            this.mat = null; // Not backed by a Mat in this case
        }

        /// <summary>
        /// Copies the unmanaged image data back to a Mat
        /// </summary>
        public void CopyToMat(Mat destination) {
            if (mat != null) {
                mat.CopyTo(destination);
            }
        }

        /// <summary>
        /// Creates a Bitmap from this unmanaged image
        /// </summary>
        public Bitmap ToManagedImage() {
            return new Bitmap(mat);
        }

        /// <summary>
        /// Collect pixel values from the specified list of coordinates
        /// </summary>
        public byte[] Collect8bppPixelValues(System.Collections.Generic.List<IntPoint> points) {
            if (PixelFormat != PixelFormat.Format8bppIndexed) {
                throw new InvalidOperationException("Image must be 8bpp grayscale");
            }

            byte[] values = new byte[points.Count];
            byte* ptr = (byte*)imageData;

            for (int i = 0; i < points.Count; i++) {
                int x = points[i].X;
                int y = points[i].Y;
                if (x >= 0 && x < Width && y >= 0 && y < Height) {
                    values[i] = ptr[y * Stride + x];
                }
            }

            return values;
        }

        /// <summary>
        /// Collect pixel values from the specified list of coordinates (16-bit)
        /// </summary>
        public ushort[] Collect16bppPixelValues(System.Collections.Generic.List<IntPoint> points) {
            if (PixelFormat != PixelFormat.Format16bppGrayScale) {
                throw new InvalidOperationException("Image must be 16bpp grayscale");
            }

            ushort[] values = new ushort[points.Count];
            ushort* ptr = (ushort*)imageData;

            for (int i = 0; i < points.Count; i++) {
                int x = points[i].X;
                int y = points[i].Y;
                if (x >= 0 && x < Width && y >= 0 && y < Height) {
                    values[i] = ptr[y * (Stride / 2) + x];
                }
            }

            return values;
        }

        public void Dispose() {
            if (ownsData && mat != null) {
                mat.Dispose();
                mat = null;
            }
        }
    }
}
