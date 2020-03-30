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
using System.Threading;

namespace NINA.Utility.SerialCommunication {

    public abstract class SerialSdk : ISerialSdk {
        protected virtual string LogName => "Generic Serial Sdk";

        private readonly ResponseCache _cache = new ResponseCache();
        public ISerialPort SerialPort { get; set; }
        public ISerialPortProvider SerialPortProvider { protected get; set; } = new SerialPortProvider();

        private readonly List<object> clients = new List<object>();
        private readonly SemaphoreSlim ssSerial = new SemaphoreSlim(1, 1);

        public virtual bool InitializeSerialPort(string portName, object client, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8,
            StopBits stopBits = StopBits.One, Handshake handShake = Handshake.None, bool dtrEnable = false,
            string newLine = "\n", int readTimeout = 500, int writeTimeout = 500) {
            try {
                ssSerial.Wait();
                if (string.IsNullOrEmpty(portName)) return false;
                if (!clients.Contains(client)) { clients.Add(client); }
                if (SerialPort != null && SerialPort.PortName.Equals(portName)) {
                    return true;
                }
                SerialPort = SerialPortProvider.GetSerialPort(portName, baudRate, parity, dataBits, stopBits, handShake, dtrEnable, newLine, readTimeout, writeTimeout);
                SerialPort?.Open();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError(string.Format(Locale.Loc.Instance["LblSerialPortCannotOpen"], SerialPort.PortName, ex.GetType().Name));

                if (clients.Contains(client)) { clients.Remove(client); }
                SerialPort = null;
            } finally {
                ssSerial.Release();
            }
            return SerialPort != null;
        }

        public T SendCommand<T>(ICommand command) where T : Response, new() {
            var result = string.Empty;
            T response;
            ssSerial.Wait();
            if (_cache.HasValidResponse(command.GetType())) {
                ssSerial.Release();
                return (T)_cache.Get(command.GetType());
            }
            try {
                Logger.Debug($"{LogName}: command : {command}");
                SerialPort.Write(command.CommandString);
                result = SerialPort.ReadLine();
                Logger.Debug($"{LogName}: response : {result}");
            } catch (TimeoutException ex) {
                Logger.Error($"{LogName}: timed out for port : {SerialPort.PortName} {ex} \n" +
                             $"Command was : {command}");
            } catch (Exception ex) {
                Logger.Error($"{LogName}: Unexpected exception : {ex}");
            } finally {
                response = new T { DeviceResponse = result };
                _cache.Add(command, response);
                ssSerial.Release();
            }
            return response;
        }

        public void Dispose(object client) {
            try {
                ssSerial.Wait();
                if (!clients.Contains(client)) return;
                clients.Remove(client);
                if (clients.Count > 0) return;
                _cache.Clear();
                SerialPort?.Close();
                SerialPort = null;
            } catch (Exception ex) {
                Logger.Error(ex);
            } finally {
                ssSerial.Release();
            }
        }
    }
}