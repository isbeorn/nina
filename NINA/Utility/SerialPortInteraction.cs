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
using System.IO.Ports;

namespace NINA.Utility {

    internal class SerialPortInteraction {

        public SerialPortInteraction(string portName) {
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

        public bool EnableRts(bool enable) {
            if (!port.IsOpen) { return false; }
            bool success = false;
            try {
                Logger.Debug("Toggle Rts on " + port.PortName);
                port.RtsEnable = enable;
                Logger.Debug("Rts is now: " + port.RtsEnable);
                success = true;
            } catch (Exception ex) {
                Logger.Debug(ex.Message + "\t" + ex.StackTrace);
            }
            return success;
        }

        public bool Close() {
            bool success = false;
            try {
                if (port.IsOpen) {
                    Logger.Debug("Closing Serial Port " + port.PortName);
                    port.Close();
                }
                success = true;
            } catch (Exception ex) {
                Logger.Debug(ex.Message + "\t" + ex.StackTrace);
            }
            return success;
        }
    }
}