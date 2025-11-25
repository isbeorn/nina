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
    /// Specifies the format of an image
    /// </summary>
    public partial class ImageFormat {
        private readonly string _name;

        private ImageFormat(string name) {
            _name = name;
        }

        public static readonly ImageFormat Png = new ImageFormat("PNG");
        public static readonly ImageFormat Jpeg = new ImageFormat("JPEG");
        public static readonly ImageFormat Bmp = new ImageFormat("BMP");
        public static readonly ImageFormat Tiff = new ImageFormat("TIFF");
        public static readonly ImageFormat Gif = new ImageFormat("GIF");

        public override string ToString() => _name;
    }
}

namespace System.Drawing.Imaging {
    public partial class ImageFormat {
        public System.Guid Guid { get; set; }
    }
}
