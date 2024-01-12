#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System.ComponentModel;

namespace NINA.Core.Enum {

    /// <summary>
    /// This mirrors the ASCOM CameraStates enum with the addition of a -1 "Unknown" state
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum CameraStates {

        /// <summary>
        /// Camera driver does not give an indication of state
        /// </summary>
        [Description("LblCameraStateNoState")]
        NoState = -1,

        /// <summary>
        /// At idle state, available to start exposure
        /// </summary>
        [Description("LblCameraStateIdle")]
        Idle = 0,

        /// <summary>
        /// Exposure started but waiting (for shutter, trigger, filter wheel, etc.)
        /// </summary>
        [Description("LblCameraStateWaiting")]
        Waiting,

        /// <summary>
        /// Exposure currently in progress
        /// </summary>
        [Description("LblCameraStateExposing")]
        Exposing,

        /// <summary>
        /// CCD array is being read out (digitized)
        /// </summary>
        [Description("LblCameraStateReading")]
        Reading,

        /// <summary>
        /// Downloading data to PC
        /// </summary>
        [Description("LblCameraStateDownload")]
        Download,

        /// <summary>
        /// Camera error condition serious enough to prevent further operations (connection fail, etc.).
        /// </summary>
        [Description("LblCameraStateError")]
        Error,

        /// <summary>
        /// NINA: Camera simulator is loading a file
        /// </summary>
        [Description("LblCameraStateLoadingFile")]
        LoadingFile = 100,
    }
}