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

        public void Send(byte[] bytesToSend)
        {
            port.Write(bytesToSend, 0, bytesToSend.Length);
        }
    }
}