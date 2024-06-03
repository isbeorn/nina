#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Model;
using System;
using System.Runtime.Serialization;

namespace NINA.Equipment.Exceptions {

    public class CameraDownloadFailedException : Exception {

        public CameraDownloadFailedException() {
        }

        public CameraDownloadFailedException(CaptureSequence sequence) : this(sequence, $"Camera download failed") {
        }

        public CameraDownloadFailedException(CaptureSequence sequence, string message) : this(sequence.ExposureTime, sequence.ImageType, sequence.Gain, sequence.FilterType?.Name ?? string.Empty, message) {
        }
        public CameraDownloadFailedException(double exposureTime, string imageType, int gain, string filter) : this($"Camera download failed - Exposure details: Exposure time: {exposureTime}, Type: {imageType}, Gain: {gain}, Filter: {filter}") {
        }

        public CameraDownloadFailedException(double exposureTime, string imageType, int gain, string filter, string message) : this($"{message} - Exposure details: Exposure time: {exposureTime}, Type: {imageType}, Gain: {gain}, Filter: {filter}") {
        }

        public CameraDownloadFailedException(string message) : base(message) {
        }

        public CameraDownloadFailedException(string message, Exception innerException) : base(message, innerException) {
        }

        protected CameraDownloadFailedException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}