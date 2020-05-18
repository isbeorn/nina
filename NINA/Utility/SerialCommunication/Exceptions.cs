using System;

namespace NINA.Utility.SerialCommunication {

    [Serializable]
    public class SerialPortClosedException : Exception {

        public SerialPortClosedException() {
        }

        public SerialPortClosedException(string message) : base(message) {
        }

        public SerialPortClosedException(string message, Exception innerException) : base(message, innerException) {
        }

        protected SerialPortClosedException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext) {
        }
    }

    [Serializable]
    public class InvalidDeviceResponseException : Exception {

        public InvalidDeviceResponseException() {
        }

        public InvalidDeviceResponseException(string message) : base(message) {
        }

        public InvalidDeviceResponseException(string message, Exception innerException) : base(message, innerException) {
        }

        protected InvalidDeviceResponseException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext) {
        }
    }
}