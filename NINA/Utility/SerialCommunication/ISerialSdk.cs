#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.IO.Ports;
using System.Threading.Tasks;

namespace NINA.Utility.SerialCommunication {

    public interface ISerialSdk {

        Task<TResult> SendCommand<TResult>(ICommand command) where TResult : Response, new();

        ISerialPort SerialPort { get; set; }
        ISerialPortProvider SerialPortProvider { set; }

        bool InitializeSerialPort(string portName, object client, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8,
            StopBits stopBits = StopBits.One, Handshake handShake = Handshake.None, bool dtrEnable = false,
            string newLine = "\n", int readTimeout = 500, int writeTimeout = 500);

        void Dispose(object client);
    }
}