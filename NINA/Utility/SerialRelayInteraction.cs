#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System;
using System.IO.Ports;

namespace NINA.Utility {

    internal class SerialRelayInteraction {

        public SerialRelayInteraction(string portName) {
            port = new SerialPort(portName);
            port.Handshake = Handshake.None;
        }

        private SerialPort port;

        public string PortName {
            get {
                return port.PortName;
            }
        }

        public bool Open() {
            bool success = false;
            try {
                if (!port.IsOpen) {
                    Logger.Debug("Opening Serial Port " + port.PortName);
                    port.Open();
                }
                success = true;
            } catch (Exception ex) {
                Logger.Debug(ex.Message + "\t" + ex.StackTrace);
            }
            return success;
        }

        public void Send(byte[] bytesToSend) {
            port.Write(bytesToSend, 0, bytesToSend.Length);
        }
    }
}