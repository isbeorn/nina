#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace System.Drawing.Imaging {
    /// <summary>
    /// ImageLockMode - specifies flags for locking image data
    /// </summary>
    [Flags]
    public enum ImageLockMode {
        ReadOnly = 1,
        WriteOnly = 2,
        ReadWrite = 3,
        UserInputBuffer = 4
    }

    /// <summary>
    /// BitmapData - provides information about a locked bitmap
    /// </summary>
    public class BitmapData {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public IntPtr Scan0 { get; set; }
        public PixelFormat PixelFormat { get; set; }
    }
}
