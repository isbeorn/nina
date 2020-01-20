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
using System.Threading;

namespace NINA.Utility.SerialCommunication {

    public abstract class SerialSdk : ISerialSdk {
        private readonly ResponseCache _cache = new ResponseCache();
        public ISerialPort SerialPort { get; set; }
        protected virtual string LogName => "Generic Serial Sdk";
        private readonly SemaphoreSlim ssSendCommand = new SemaphoreSlim(1, 1);

        public T SendCommand<T>(ICommand command) where T : Response, new() {
            var result = string.Empty;
            ssSendCommand.Wait();
            if (_cache.HasValidResponse(command.GetType())) {
                ssSendCommand.Release();
                return (T)_cache.Get(command.GetType());
            }
            try {
                SerialPort.Open();
                Logger.Debug($"{LogName}: command : {command}");
                SerialPort.Write(command.CommandString);
                result = SerialPort.ReadLine();
                Logger.Debug($"{LogName}: response : {result}");
            } catch (TimeoutException ex) {
                Logger.Error($"{LogName}: timed out for port : {SerialPort.PortName} {ex}");
            } catch (Exception ex) {
                Logger.Error($"{LogName}: Unexpected exception : {ex}");
            } finally {
                SerialPort?.Close();
                ssSendCommand.Release();
            }
            var response = new T { DeviceResponse = result };
            _cache.Add(command, response);
            return response;
        }

        public void Dispose() {
            SerialPort?.Dispose();
        }
    }
}