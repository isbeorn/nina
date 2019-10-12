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