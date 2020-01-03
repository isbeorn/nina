#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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