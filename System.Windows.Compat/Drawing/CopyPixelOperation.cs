#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Drawing {
    /// <summary>
    /// Specifies how the source color in a copy pixel operation is combined with the destination color
    /// </summary>
    public enum CopyPixelOperation {
        /// <summary>
        /// The source bitmap is copied directly to the destination bitmap
        /// </summary>
        SourceCopy = 0x00CC0020,

        /// <summary>
        /// The source and destination colors are combined using the Boolean AND operator
        /// </summary>
        SourceAnd = 0x008800C6,

        /// <summary>
        /// The source and destination colors are combined using the Boolean OR operator
        /// </summary>
        SourcePaint = 0x00EE0086,

        /// <summary>
        /// Fills the destination rectangle using the color associated with index 0 in the physical palette
        /// </summary>
        Blackness = 0x00000042,

        /// <summary>
        /// Fills the destination rectangle using the color associated with index 1 in the physical palette
        /// </summary>
        Whiteness = 0x00FF0062
    }
}
