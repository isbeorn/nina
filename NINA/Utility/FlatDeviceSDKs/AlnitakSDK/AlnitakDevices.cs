using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public static class AlnitakDevices {

        public static List<string> GetDevices() {
            var result = new List<string>();
            foreach (var portName in SerialPort.GetPortNames()) {
                var serialPort = new SerialPort {
                    PortName = portName,
                    BaudRate = 9600,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    NewLine = "\n",
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

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

                    result.Add($"{response.Name};{serialPort.PortName}");
                } catch (TimeoutException) {
                    Logger.Debug($"AlnitakFlatDevice: timed out for port : {serialPort.PortName}");
                } catch (Exception ex) {
                    Logger.Debug($"AlnitakFlatDevice: Unexpected exception : {ex}");
                } finally {
                    serialPort.Close();
                }
            }

            return result;
        }
    }
}