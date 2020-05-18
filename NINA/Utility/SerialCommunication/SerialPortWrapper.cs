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

namespace NINA.Utility.SerialCommunication {

    public sealed class SerialPortWrapper : ISerialPort {
        private readonly SerialPort _serialPort;

        public SerialPortWrapper() {
            _serialPort = new SerialPort();
        }

        public string PortName { get => _serialPort.PortName; set => _serialPort.PortName = value; }
        public int BaudRate { get => _serialPort.BaudRate; set => _serialPort.BaudRate = value; }
        public Parity Parity { get => _serialPort.Parity; set => _serialPort.Parity = value; }
        public int DataBits { get => _serialPort.DataBits; set => _serialPort.DataBits = value; }
        public StopBits StopBits { get => _serialPort.StopBits; set => _serialPort.StopBits = value; }
        public Handshake Handshake { get => _serialPort.Handshake; set => _serialPort.Handshake = value; }
        public bool DtrEnable { get => _serialPort.DtrEnable; set => _serialPort.DtrEnable = value; }
        public bool RtsEnable { get => _serialPort.RtsEnable; set => _serialPort.RtsEnable = value; }
        public string NewLine { get => _serialPort.NewLine; set => _serialPort.NewLine = value; }
        public int ReadTimeout { get => _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }
        public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }
        public int BytesToRead => _serialPort.BytesToRead;

        public void Close() => _serialPort.Close();

        public void DiscardInBuffer() => _serialPort.DiscardInBuffer();

        public void Open() => _serialPort.Open();

        public string ReadLine() => _serialPort.ReadLine();

        public void Write(string value) => _serialPort.Write(value);
    }
}
