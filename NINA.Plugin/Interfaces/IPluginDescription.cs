#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Plugin.Interfaces {

    public interface IPluginDescription {

        /// <summary>
        /// A short summary of the plugin capabilities.
        /// </summary>
        string ShortDescription { get; }

        /// <summary>
        /// A more in-depth description of the plugin with all of its capabilities explained in detail.
        /// </summary>
        string LongDescription { get; }

        /// <summary>
        /// The most relevant image for the plugin that should be prominently displayed.
        /// </summary>
        string FeaturedImageURL { get; }

        /// <summary>
        /// An example image of using the plugin.
        /// </summary>
        string ScreenshotURL { get; }

        /// <summary>
        /// An alternative image of using the plugin.
        /// </summary>
        string AltScreenshotURL { get; }
    }
}