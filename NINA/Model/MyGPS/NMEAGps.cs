#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
// uses NMEAParser , by Morten Nielsen : https://github.com/dotMorten/NmeaParser

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.Model;
using System.Linq;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using System;
using NmeaParser;
using System.Collections.Generic;

namespace NINA.Model.MyGPS {

    /// <summary>
    /// NMEA GPS Class detects comport based NMEA GPS Devices
    /// Flow : construct -> Autodiscover [detect, Connect, listens to messages]
    /// </summary>
    internal class NMEAGps : BaseINPC, IDevice, IDisposable {
        private IProfileService profileService;
        private int gpsId;
        private bool connected;
        private string portName;
        private int baudRate = 0;
        private System.Timers.Timer fixTimer;
        private NmeaParser.SerialPortDevice currentDevice;
        private TaskCompletionSource<bool> gotGPSFix;
        public double[] Coords = new double[2];

        public NMEAGps(int gpsId, IProfileService profileService) {
            this.profileService = profileService;
            this.gpsId = gpsId;
            connected = false;
        }

        public string Category { get; } = "NMEA";

        public string Id => $"#{gpsId} (#{portName})";

        public string Name {
            get {
                return "NMEA GPS Device";
            }
        }

        public string Description {
            get {
                return string.Empty;
            }
        }

        public string DriverInfo {
            get {
                string s = "NMEA GPS";
                return s;
            }
        }

        public string DriverVersion {
            get {
                string version = "0.1";
                return version;
            }
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public void SetupDialog() {
            //TODO : allow the user to select a specific com port maybe?
        }

        public void Initialize() {
            if (connected) Disconnect();
            connected = false;
            baudRate = 0;
            portName = "";
        }

        private void OnFixTimedEvent(Object source, System.Timers.ElapsedEventArgs e) {
            // give up with a connected GPS that has no Fix
            Notification.ShowWarning(Locale.Loc.Instance["LblGPSNoFix"]);
            Disconnect();
            gotGPSFix.TrySetResult(false);
        }

        public bool Connected {
            get {
                return connected;
            }
            set {
                connected = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///checks GPS messages, transfers location to the options view
        ///if a fix is obtained
        /// </summary>
        private void Device_MessageReceived(object sender, NmeaParser.NmeaMessageReceivedEventArgs args) {
            var message = args.Message;
            if (message is NmeaParser.Nmea.Rmc) {
                Coords[0] = ((NmeaParser.Nmea.Rmc)message).Longitude;
                Coords[1] = ((NmeaParser.Nmea.Rmc)message).Latitude;
            } else if (args.Message is NmeaParser.Nmea.Gga) {
                Coords[0] = ((NmeaParser.Nmea.Gga)message).Longitude;
                Coords[1] = ((NmeaParser.Nmea.Gga)message).Latitude;
            } else if (args.Message is NmeaParser.Nmea.Gll) {
                Coords[0] = ((NmeaParser.Nmea.Gll)message).Longitude;
                Coords[1] = ((NmeaParser.Nmea.Gll)message).Latitude;
            } else return;

            if (Double.IsNaN(Coords[0]) || Double.IsNaN(Coords[1])) return; // no fix yet
            try {
                currentDevice.MessageReceived -= Device_MessageReceived; // unsubscribe to avoid multiple messages
                fixTimer.Enabled = false;
                fixTimer.Dispose();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            Notification.ShowSuccess(Locale.Loc.Instance["LblGPSLocationSet"]);
            gotGPSFix.TrySetResult(true);
        }

        public async Task<bool> Connect(CancellationToken token) {
            if ((currentDevice != null) || (baudRate == 0)) return false; // disconenct first
            try {
                var device = new NmeaParser.SerialPortDevice(new System.IO.Ports.SerialPort(portName, baudRate));
                currentDevice = device;
                device.MessageReceived += Device_MessageReceived;
                fixTimer = new System.Timers.Timer(4000); // try for 4 secs
                fixTimer.Elapsed += OnFixTimedEvent;
                fixTimer.AutoReset = false;
                fixTimer.Enabled = true;
                gotGPSFix = new TaskCompletionSource<bool>();
                connected = true;
                await device.OpenAsync();
                Notification.ShowSuccess(Locale.Loc.Instance["LblGPSConnected"] + " " + portName);
                return await gotGPSFix.Task;
            } catch (System.Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblGPSConnectFail"] + " " + portName);
                return false;
            }
        }

        public void Disconnect() {
            if (currentDevice.IsOpen) currentDevice.CloseAsync();
            try {
                fixTimer.Enabled = false;
                fixTimer.Dispose();
                currentDevice.Dispose();
            } catch (Exception ex) {
                // something went wrong
                Logger.Error(ex);
            }
            connected = false;
        }

        private static string[] GetComPorts() {
            var ports = System.IO.Ports.SerialPort.GetPortNames().OrderBy(s => s);
            string[] ret = new string[ports.Count()];
            int i = 0;
            foreach (var cportName in ports)
                ret[i++] = cportName;
            return ret;
        }

        /// <summary>
        /// this finds a suitable comport, connects and listens for CPS messages
        /// </summary>
        private System.IO.Ports.SerialPort FindPort() {
            string[] allPorts = GetComPorts();
            int[,] portRates = new int[allPorts.Length, 7];
            // set port / baud test precedence
            for (int pnum = 0; pnum < allPorts.Length; pnum++) {
                List<int> baudRatesToTest = new List<int>(new[]
                   { 9600, 4800, 115200, 19200, 57600, 38400, 2400 });
                string cportName = allPorts[pnum];
                using (var port = new System.IO.Ports.SerialPort(cportName)) {
                    var defaultRate = port.BaudRate;
                    if (baudRatesToTest.Contains(defaultRate)) baudRatesToTest.Remove(defaultRate);
                    baudRatesToTest.Insert(0, defaultRate);
                    for (int bnum = 0; bnum < baudRatesToTest.Count; bnum++)
                        portRates[pnum, bnum] = baudRatesToTest[bnum];
                }
            }
            // use computed precedences to test the ports
            for (int bnum = 0; bnum < 7; bnum++)
                for (var pnum = 0; pnum < allPorts.Length; pnum++) {
                    string cportName = allPorts[pnum];
                    int baud = portRates[pnum, bnum];
                    using (var port = new System.IO.Ports.SerialPort(cportName)) {
                        port.BaudRate = baud;
                        port.ReadTimeout = 2000;
                        bool success = false;
                        try {
                            port.Open();
                            if (!port.IsOpen)
                                continue; //couldn't open port
                            try { // ths is blocking
                                port.ReadTo("$GP");
                            } catch (TimeoutException) {
                                continue;
                            }
                            success = true;
                        } catch (Exception ex) {
                            //Error reading
                            Logger.Error(ex);
                        }
                        if (success) {
                            return new System.IO.Ports.SerialPort(cportName, baud);
                        }
                    }
                }
            return null;
        }

        public void Dispose() {
            if (connected) Disconnect();
        }

        /// <summary>
        /// discovers the first GPS device connected to a serial port
        /// </summary>
        public bool AutoDiscover() {
            using (System.IO.Ports.SerialPort port = FindPort()) {
                if (port != null) //we found a port with a GPS
                {
                    portName = port.PortName;
                    baudRate = port.BaudRate;
                    return true;
                } else // no GPS found
                  {
                    portName = "";
                    baudRate = 0;
                    Notification.ShowError(Locale.Loc.Instance["LblGPSNotFound"]);
                    return false;
                }
            }
        }
    }
}
