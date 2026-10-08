#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using System.IO;

namespace NINA.Profile {
    public static class ImageFileSettingsExtensions {
        /// <summary>
        /// Returns the directory result recorded when the output path was configured.
        /// Custom settings implementations retain direct filesystem validation.
        /// </summary>
        public static bool IsFilePathValid(this IImageFileSettings settings) => settings.GetType() == typeof(ImageFileSettings)
            ? ((ImageFileSettings)settings).FilePathExists
            : Directory.Exists(settings.FilePath);
    }
}