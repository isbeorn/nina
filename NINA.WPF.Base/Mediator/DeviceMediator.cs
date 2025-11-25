#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Extensions;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.WPF.Base.Mediator {

    /// <summary>
    /// Abstract class for communication between a device handler and consumers
    /// Consumers will receive device updates from the handler in form of TInfo object when registered.
    /// </summary>
    /// <typeparam name="THandler"></typeparam>
    /// <typeparam name="TConsumer"></typeparam>
    /// <typeparam name="TInfo"></typeparam>
    public abstract class DeviceMediator<THandler, TConsumer, TInfo> : IDeviceMediator<THandler, TConsumer, TInfo> where THandler : IDeviceVM<TInfo> where TConsumer : IDeviceConsumer<TInfo> {
        protected THandler handler;
        protected List<TConsumer> consumers = new List<TConsumer>();
        private event Func<object, EventArgs, Task> pendingConnected;
        private event Func<object, EventArgs, Task> pendingDisconnected;

        public event Func<object, EventArgs, Task> Connected {
            add { 
                if (this.handler != null) {
                    this.handler.Connected += value;
                } else {
                    pendingConnected += value;
                }
            }
            remove { 
                if (this.handler != null) {
                    this.handler.Connected -= value;
                } else {
                    pendingConnected -= value;
                }
            }
        }
        public event Func<object, EventArgs, Task> Disconnected {
            add { 
                if (this.handler != null) {
                    this.handler.Disconnected += value;
                } else {
                    pendingDisconnected += value;
                }
            }
            remove { 
                if (this.handler != null) {
                    this.handler.Disconnected -= value;
                } else {
                    pendingDisconnected -= value;
                }
            }
        }

        public void RegisterHandler(THandler handler) {            
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;

            // Attach any pending event handlers
            if (pendingConnected != null) {
                foreach (var d in pendingConnected.GetInvocationList()) {
                    this.handler.Connected += (Func<object, EventArgs, Task>)d;
                }
                pendingConnected = null;
            }
            if (pendingDisconnected != null) {
                foreach (var d in pendingDisconnected.GetInvocationList()) {
                    this.handler.Disconnected += (Func<object, EventArgs, Task>)d;
                }
                pendingDisconnected = null;
            }

            var info = handler.GetDeviceInfo();
            Broadcast(info);
        }

        public void RegisterConsumer(TConsumer consumer) {
            lock (consumers) {
                consumers.Add(consumer);
            }
            if (handler != null) {
                var info = handler.GetDeviceInfo();
                consumer.UpdateDeviceInfo(info);
            }
        }

        public void RemoveConsumer(TConsumer consumer) {
            lock (consumers) {
                consumers.Remove(consumer);
            }
        }

        public Task<IList<string>> Rescan() {
            return handler?.Rescan();
        }

        /// <summary>
        /// Connect the device
        /// </summary>
        /// <returns></returns>
        public Task<bool> Connect() {
            return handler?.Connect();
        }

        /// <summary>
        /// Disconnect the device
        /// </summary>
        public Task Disconnect() {
            return handler?.Disconnect();
        }

        /// <summary>
        /// Broadcast device info updates to all consumers
        /// </summary>
        /// <param name="deviceInfo"></param>
        public void Broadcast(TInfo deviceInfo) {
            List<TConsumer> receivers;
            lock (consumers) {
                receivers = new List<TConsumer>(consumers);
            }
            
            foreach (TConsumer c in receivers) {
                try {
                    c.UpdateDeviceInfo(deviceInfo);
                } catch (Exception e) {
                    Logger.Error(e);
                }
            }
        }

        public TInfo GetInfo() {
            if (handler == null) {
                return default;
            }
            return handler.GetDeviceInfo();
        }

        /// <summary>
        /// Returns the device instance from the handler for direct access
        /// Please use this only when no other method is available via the viewmodel
        /// </summary>
        /// <returns></returns>
        public IDevice GetDevice() {
            return handler.GetDevice();
        }

        public string Action(string actionName, string actionParameters) {
            return handler.Action(actionName, actionParameters);
        }

        public string SendCommandString(string command, bool raw = true) {
            return handler.SendCommandString(command, raw);
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return handler.SendCommandBool(command, raw);
        }

        public void SendCommandBlind(string command, bool raw = true) {
            handler.SendCommandBlind(command, raw);
        }
    }
}
