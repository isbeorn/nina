#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

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