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
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.SerialCommunication {

    public abstract class SerialSdk : ISerialSdk {
        protected virtual string LogName => "Generic Serial Sdk";

        private readonly ResponseCache _cache = new ResponseCache();
        public ISerialPort SerialPort { get; set; }

        private ISerialPortProvider _provider;

        public ISerialPortProvider SerialPortProvider {
            protected get => _provider ?? (_provider = new SerialPortProvider());
            set => _provider = value;
        }

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
                Notification.Notification.ShowError(string.Format(Locale.Loc.Instance["LblSerialPortCannotOpen"], SerialPort?.PortName, ex.GetType().Name));

                if (clients.Contains(client)) { clients.Remove(client); }
                SerialPort = null;
            } finally {
                ssSerial.Release();
            }
            return SerialPort != null;
        }

        public Task<TResult> SendCommand<TResult>(ICommand command) where TResult : Response, new() {
            return Task.Run(() => {
                if (command == null) throw new ArgumentNullException(nameof(command));
                ssSerial.Wait();
                TResult response;
                if (_cache.HasValidResponse(command.GetType())) {
                    response = (TResult)_cache.Get(command.GetType());
                    ssSerial.Release();
                    return response;
                }

                WriteAndLog(command);

                if (!command.HasResponse) {
                    ssSerial.Release();
                    return null;
                }

                var result = string.Empty;
                var retries = 2;
                try {
                    while (string.IsNullOrEmpty(result) && retries >= 0) {
                        result = ReadAndLog(command, ref retries);
                    }

                    response = new TResult { DeviceResponse = result };
                    _cache.Add(command, response);
                    return response;
                } catch (TimeoutException ex) {
                    Logger.Error($"{LogName}: Giving up and cleaning up buffer.{ex}");
                    CleanupInBuffer();
                    throw;
                } catch (InvalidOperationException ex) {
                    Logger.Error($"{LogName}: Port is closed. {ex}");
                    throw new SerialPortClosedException(
                        $"Serial port {SerialPort?.PortName ?? "null"} was closed when trying to read.", ex);
                } finally {
                    ssSerial.Release();
                }
            });
        }

        private void CleanupInBuffer() {
            try {
                Logger.Error($"Cleaning up {SerialPort?.BytesToRead.ToString()} from read buffer.");
                SerialPort?.DiscardInBuffer();
            } catch (Exception ex) {
                Logger.Error($"{LogName}: Port is in an invalid state. {ex}");
                throw new SerialPortClosedException($"Serial port {SerialPort?.PortName ?? "null"} was closed when trying to clean up buffer.", ex);
            }
        }

        private void WriteAndLog(ICommand command) {
            try {
                Logger.Trace($"{LogName}: command : {command}");
                SerialPort?.Write(command.CommandString);
            } catch (TimeoutException ex) {
                //Don't throw here, we still need to try and read whatever is on the line in case this actually succeeded
                Logger.Error($"{LogName}: timed out for port : {SerialPort?.PortName ?? "null"} {ex} \n" +
                             $"Command was : {command}");
            } catch (InvalidOperationException ex) {
                Logger.Error($"{LogName}: Port is closed. {ex}");
                throw new SerialPortClosedException($"Serial port {SerialPort?.PortName ?? "null"} was closed when trying to write command {command}", ex);
            } catch (ArgumentNullException ex) {
                Logger.Error($"{LogName}: {command} was trying to write null to port {SerialPort?.PortName ?? "null"} : {ex}");
                throw;
            }
        }

        private string ReadAndLog(ICommand command, ref int retries) {
            string result = null;
            try {
                result = SerialPort?.ReadLine();
                Logger.Trace($"{LogName}: response : {result}");
            } catch (TimeoutException ex) {
                Logger.Error($"{LogName}: timed out for port : {SerialPort?.PortName ?? "null"} {ex} \n" +
                             $"Command was : {command}. {retries} retries left");
                if (retries == 0) throw;
            } finally {
                retries--;
            }
            return result;
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
            } catch (IOException ex) {
                Logger.Error(ex);
                SerialPort = null;
            } finally {
                ssSerial.Release();
            }
        }
    }
}