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
    /// Defines an array of colors that make up a color palette
    /// </summary>
    public sealed class ColorPalette {
        private Color[] entries;
        private int flags;

        internal ColorPalette() {
            entries = new Color[256];
        }

        internal ColorPalette(int count) {
            entries = new Color[count];
        }

        /// <summary>
        /// Gets an array of Color structures
        /// </summary>
        public Color[] Entries {
            get { return entries; }
        }

        /// <summary>
        /// Gets a value that specifies how to interpret the color information in the array of colors
        /// </summary>
        public int Flags {
            get { return flags; }
        }
    }
}
