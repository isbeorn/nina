#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Media.Imaging {
    /// <summary>
    /// Metadata for bitmap images
    /// </summary>
    public class BitmapMetadata {
        private readonly string format;

        public BitmapMetadata(string format) {
            this.format = format;
        }

        /// <summary>
        /// Application name that created/modified the image
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Title of the image
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Author of the image
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Copyright information
        /// </summary>
        public string Copyright { get; set; }

        /// <summary>
        /// Subject/description of the image
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Date the photo was taken
        /// </summary>
        public string DateTaken { get; set; }
    }
}
