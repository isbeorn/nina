#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Drawing.Imaging {
    /// <summary>
    /// Specifies the format of the color data for each pixel in the image
    /// </summary>
    public enum PixelFormat {
        /// <summary>
        /// The pixel format is undefined
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// The pixel data contains color-indexed values, which means the values are an index to colors in the system color table
        /// </summary>
        Indexed = 0x00010000,

        /// <summary>
        /// The pixel data contains GDI colors
        /// </summary>
        Gdi = 0x00020000,

        /// <summary>
        /// Specifies that the format is 16 bits per pixel; 5 bits each are used for the red, green, and blue components. The remaining bit is not used.
        /// </summary>
        Format16bppRgb555 = 0x00021005,

        /// <summary>
        /// Specifies that the format is 16 bits per pixel; 5 bits are used for the red component, 6 bits are used for the green component, and 5 bits are used for the blue component.
        /// </summary>
        Format16bppRgb565 = 0x00021006,

        /// <summary>
        /// Specifies that the format is 24 bits per pixel; 8 bits each are used for the red, green, and blue components.
        /// </summary>
        Format24bppRgb = 0x00021808,

        /// <summary>
        /// Specifies that the format is 32 bits per pixel; 8 bits each are used for the red, green, and blue components. The remaining 8 bits are not used.
        /// </summary>
        Format32bppRgb = 0x00022009,

        /// <summary>
        /// Specifies that the format is 32 bits per pixel; 8 bits each are used for the alpha, red, green, and blue components.
        /// </summary>
        Format32bppArgb = 0x0026200A,

        /// <summary>
        /// Specifies that the format is 32 bits per pixel; 8 bits each are used for the alpha, red, green, and blue components. The red, green, and blue components are premultiplied according to the alpha component.
        /// </summary>
        Format32bppPArgb = 0x000E200B,

        /// <summary>
        /// Specifies that the format is 48 bits per pixel; 16 bits each are used for the red, green, and blue components.
        /// </summary>
        Format48bppRgb = 0x0010300C,

        /// <summary>
        /// Specifies that the format is 64 bits per pixel; 16 bits each are used for the alpha, red, green, and blue components.
        /// </summary>
        Format64bppArgb = 0x0034400D,

        /// <summary>
        /// Specifies that the format is 64 bits per pixel; 16 bits each are used for the alpha, red, green, and blue components. The red, green, and blue components are premultiplied according to the alpha component.
        /// </summary>
        Format64bppPArgb = 0x001C400E,

        /// <summary>
        /// Specifies that the format is 16 bits per pixel grayscale
        /// </summary>
        Format16bppGrayScale = 0x00101004,

        /// <summary>
        /// Specifies that the format is 8 bits per pixel grayscale
        /// </summary>
        Format8bppIndexed = 0x00030803,

        /// <summary>
        /// Specifies that the format is 4 bits per pixel indexed
        /// </summary>
        Format4bppIndexed = 0x00030402,

        /// <summary>
        /// Specifies that the format is 1 bit per pixel indexed
        /// </summary>
        Format1bppIndexed = 0x00030101,

        /// <summary>
        /// The default pixel format of 32 bits per pixel. The format specifies 24-bit color depth and an 8-bit alpha channel.
        /// </summary>
        Canonical = Format32bppArgb,

        /// <summary>
        /// Specifies that pixel format contains alpha information
        /// </summary>
        Alpha = 0x00040000,

        /// <summary>
        /// Specifies that the pixel format contains pre-multiplied alpha information
        /// </summary>
        PAlpha = 0x00080000,

        /// <summary>
        /// Specifies that pixel format is extended color 16 bits per channel
        /// </summary>
        Extended = 0x00100000,

        /// <summary>
        /// The maximum value for this enumeration
        /// </summary>
        Max = 15
    }
}
