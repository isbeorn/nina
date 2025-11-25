#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Drawing;
using Accord.Imaging;

namespace Accord.Imaging {
    /// <summary>
    /// Drawing utility methods for UnmanagedImage
    /// </summary>
    public static class Drawing {
        /// <summary>
        /// Draws a rectangle on an unmanaged image
        /// </summary>
        public static unsafe void Rectangle(UnmanagedImage image, System.Drawing.Rectangle rect, Color color) {
            int width = image.Width;
            int height = image.Height;
            
            // Calculate pixel size based on format
            int pixelSize = image.PixelFormat switch {
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed => 1,
                System.Drawing.Imaging.PixelFormat.Format16bppGrayScale => 2,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb => 3,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb => 4,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb => 4,
                System.Drawing.Imaging.PixelFormat.Format48bppRgb => 6,
                _ => 3
            };

            byte* basePtr = (byte*)image.ImageData.ToPointer();
            int stride = image.Stride;

            byte grayValue = (byte)((color.R + color.G + color.B) / 3);

            // Draw top and bottom horizontal lines
            for (int x = rect.Left; x < rect.Right && x < width; x++) {
                if (rect.Top >= 0 && rect.Top < height) {
                    byte* ptr = basePtr + rect.Top * stride + x * pixelSize;
                    for (int i = 0; i < pixelSize; i++) {
                        ptr[i] = grayValue;
                    }
                }
                if (rect.Bottom - 1 >= 0 && rect.Bottom - 1 < height) {
                    byte* ptr = basePtr + (rect.Bottom - 1) * stride + x * pixelSize;
                    for (int i = 0; i < pixelSize; i++) {
                        ptr[i] = grayValue;
                    }
                }
            }

            // Draw left and right vertical lines
            for (int y = rect.Top; y < rect.Bottom && y < height; y++) {
                if (rect.Left >= 0 && rect.Left < width) {
                    byte* ptr = basePtr + y * stride + rect.Left * pixelSize;
                    for (int i = 0; i < pixelSize; i++) {
                        ptr[i] = grayValue;
                    }
                }
                if (rect.Right - 1 >= 0 && rect.Right - 1 < width) {
                    byte* ptr = basePtr + y * stride + (rect.Right - 1) * pixelSize;
                    for (int i = 0; i < pixelSize; i++) {
                        ptr[i] = grayValue;
                    }
                }
            }
        }
    }
}
