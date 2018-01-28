using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    class SerialPortInteraction {
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
