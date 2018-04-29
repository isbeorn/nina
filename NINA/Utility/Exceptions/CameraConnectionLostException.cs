using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Exceptions {

    [Serializable]
    internal class CameraConnectionLostException : Exception {

        public CameraConnectionLostException() {
        }

        public CameraConnectionLostException(string message) : base(message) {
        }

        public CameraConnectionLostException(string message, Exception innerException) : base(message, innerException) {
        }

        protected CameraConnectionLostException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}