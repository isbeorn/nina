#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    /// <summary>
    /// Abstract class for communication between a device handler and consumers
    /// Consumers will receive device updates from the handler in form of TInfo object when registered.
    /// </summary>
    /// <typeparam name="THandler"></typeparam>
    /// <typeparam name="TConsumer"></typeparam>
    /// <typeparam name="TInfo"></typeparam>
    internal abstract class DeviceMediator<THandler, TConsumer, TInfo> : IDeviceMediator<THandler, TConsumer, TInfo> where THandler : IDeviceVM<TInfo> where TConsumer : IDeviceConsumer<TInfo> {
        protected THandler handler;
        protected List<TConsumer> consumers = new List<TConsumer>();

        public void RegisterHandler(THandler handler) {
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;
            var info = handler.GetDeviceInfo();
            Broadcast(info);
        }

        public void RegisterConsumer(TConsumer consumer) {
            consumers.Add(consumer);
            if (handler != null) {
                var info = handler.GetDeviceInfo();
                consumer.UpdateDeviceInfo(info);
            }
        }

        public void RemoveConsumer(TConsumer consumer) {
            consumers.Remove(consumer);
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
        public void Disconnect() {
            handler?.Disconnect();
        }

        /// <summary>
        /// Broadcast device info updates to all consumers
        /// </summary>
        /// <param name="deviceInfo"></param>
        public void Broadcast(TInfo deviceInfo) {
            foreach (TConsumer c in consumers) {
                c.UpdateDeviceInfo(deviceInfo);
            }
        }
    }
}
