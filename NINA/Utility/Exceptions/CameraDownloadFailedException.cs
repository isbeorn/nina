#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Exceptions {

    internal class CameraDownloadFailedException : Exception {

        public CameraDownloadFailedException() {
        }

        public CameraDownloadFailedException(CaptureSequence sequence) : this($"Camera download failed. Exposure details: Exposure time: {sequence.ExposureTime}, Type: {sequence.ImageType}, Gain: {sequence.Gain}, Filter: {sequence.FilterType?.Name ?? string.Empty}") {
        }

        public CameraDownloadFailedException(string message) : base(message) {
        }

        public CameraDownloadFailedException(string message, Exception innerException) : base(message, innerException) {
        }

        protected CameraDownloadFailedException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}