using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Exceptions {
    class CameraConnectionLostException : Exception {
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
