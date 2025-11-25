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

namespace Accord.Imaging.Filters {
    /// <summary>
    /// Blur filter - applies Gaussian blur to an image
    /// </summary>
    public class Blur : BaseUsingCopyPartialFilter {
        private int kernelSize = 5;
        private double sigma = 0;

        /// <summary>
        /// Size of the Gaussian kernel (must be odd)
        /// </summary>
        public int KernelSize {
            get => kernelSize;
            set => kernelSize = value % 2 == 0 ? value + 1 : value; // Ensure odd
        }

        /// <summary>
        /// Gaussian kernel standard deviation
        /// If 0, calculated from kernel size
        /// </summary>
        public double Sigma {
            get => sigma;
            set => sigma = value;
        }

        /// <summary>
        /// Apply blur filter to the image
        /// </summary>
        protected override void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData, Rectangle rect) {
            // Convert UnmanagedImage to OpenCV Mat
            Mat src = UnmanagedImageToMat(sourceData);
            Mat dst = new Mat();

            // Apply Gaussian blur
            Cv2.GaussianBlur(src, dst, new OpenCvSharp.Size(KernelSize, KernelSize), Sigma, Sigma);

            // Copy result back to destination
            CopyMatToUnmanagedImage(dst, destinationData);

            dst.Dispose();
            src.Dispose();
        }

        private Mat UnmanagedImageToMat(UnmanagedImage image) {
            unsafe {
                MatType matType = image.PixelFormat switch {
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed => MatType.CV_8UC1,
                    System.Drawing.Imaging.PixelFormat.Format16bppGrayScale => MatType.CV_16UC1,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb => MatType.CV_8UC3,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb => MatType.CV_8UC4,
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb => MatType.CV_8UC4,
                    _ => MatType.CV_8UC3
                };

                return Mat.FromPixelData(image.Height, image.Width, matType, image.ImageData, image.Stride);
            }
        }

        private void CopyMatToUnmanagedImage(Mat mat, UnmanagedImage dest) {
            unsafe {
                byte* dstPtr = (byte*)dest.ImageData.ToPointer();
                byte* srcPtr = (byte*)mat.Data.ToPointer();

                // Calculate the actual bytes to copy per row
                int bytesPerRow = System.Math.Min((int)mat.Step(), dest.Stride);

                for (int y = 0; y < dest.Height; y++) {
                    byte* srcRow = srcPtr + (y * mat.Step());
                    byte* dstRow = dstPtr + (y * dest.Stride);

                    for (int x = 0; x < bytesPerRow; x++) {
                        dstRow[x] = srcRow[x];
                    }
                }
            }
        }
    }
}
