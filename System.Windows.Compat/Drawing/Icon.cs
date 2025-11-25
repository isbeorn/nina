#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.IO;

namespace System.Drawing {
    /// <summary>
    /// Icon class stub for compatibility
    /// Provides minimal Icon functionality needed for NINA
    /// </summary>
    public class Icon : IDisposable {
        private byte[] _iconData;

        public Icon(string fileName) {
            if (File.Exists(fileName)) {
                _iconData = File.ReadAllBytes(fileName);
            }
        }

        public Icon(Stream stream) {
            using (var ms = new MemoryStream()) {
                stream.CopyTo(ms);
                _iconData = ms.ToArray();
            }
        }

        public Icon(Type type, string resource) {
            // Load embedded resource
            var assembly = type.Assembly;
            var resourceName = resource;

            // Try to find the resource
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                if (stream != null) {
                    using (var ms = new MemoryStream()) {
                        stream.CopyTo(ms);
                        _iconData = ms.ToArray();
                    }
                }
            }
        }

        public int Width { get; set; } = 32;
        public int Height { get; set; } = 32;

        public Size Size => new Size(Width, Height);

        public void Dispose() {
            _iconData = null;
        }

        /// <summary>
        /// Extracts an icon from a file
        /// </summary>
        public static Icon ExtractAssociatedIcon(string filePath) {
            // On Linux, we can't extract Windows icons, so return a dummy icon
            return new Icon(Stream.Null);
        }
    }
}
