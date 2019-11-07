using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Web.WebSockets;
using NINA.Profile;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public static class AlnitakDevices {

        public static string GetDevice(bool detectSerialPort) {
            /* automatic serial port detection only works for the Alnitak flat panel connected to the COM
               port with the lowest number. Should be completely sufficient and avoids cluttering the app.
             */
            if (!detectSerialPort) {
                return $"{Locale.Loc.Instance["LblAlnitakFlatPanel"]};COM99";
            }

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

                        return $"{Locale.Loc.Instance["LblAlnitakFlatPanel"]};{serialPort.PortName}";
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