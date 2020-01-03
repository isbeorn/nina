#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Web.WebSockets;
using NINA.Profile;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public static class AlnitakDevices {

        public static string DetectSerialPort() {
            foreach (var portName in SerialPort.GetPortNames().OrderBy(s => s)) {
                using (SerialPort serialPort = new SerialPort {
                    PortName = portName,
                    BaudRate = 9600,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    NewLine = "\n",
                    ReadTimeout = 500,
                    WriteTimeout = 500
                }) {
                    try {
                        serialPort.Open();
                        var command = new PingCommand();
                        Logger.Debug($"AlnitakFlatDevice: command : {command}");
                        serialPort.Write(command.CommandString);
                        var response = new PingResponse {
                            DeviceResponse = serialPort.ReadLine()
                        };
                        Logger.Debug($"AlnitakFlatDevice: response : {response}");
                        if (!response.IsValid) {
                            continue;
                        }

                        return serialPort.PortName;
                    } catch (TimeoutException) {
                        Logger.Debug($"AlnitakFlatDevice: timed out for port : {serialPort.PortName}");
                    } catch (Exception ex) {
                        Logger.Debug($"AlnitakFlatDevice: Unexpected exception : {ex}");
                    }
                }
            }

            return null;
        }
    }
}