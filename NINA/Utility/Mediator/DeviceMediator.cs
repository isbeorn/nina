using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    /// <summary>
    /// Abstract class for communication between a device handler and consumers
    /// Consumers will receive device updates from the handler in form of TInfo object when registered.
    /// </summary>
    /// <typeparam name="THandler"></typeparam>
    /// <typeparam name="TConsumer"></typeparam>
    /// <typeparam name="TInfo"></typeparam>
    internal abstract class DeviceMediator<THandler, TConsumer, TInfo> where THandler : IDeviceVM<TInfo> where TConsumer : IDeviceConsumer<TInfo> {
        protected THandler handler;
        protected List<TConsumer> consumers = new List<TConsumer>();

        internal void RegisterHandler(THandler handler) {
            this.handler = handler;
            var info = handler.GetDeviceInfo();
            Broadcast(info);
        }

        internal void RegisterConsumer(TConsumer consumer) {
            consumers.Add(consumer);
            if (handler != null) {
                var info = handler.GetDeviceInfo();
                consumer.UpdateDeviceInfo(info);
            }
        }

        internal void RemoveConsumer(TConsumer consumer) {
            consumers.Remove(consumer);
        }

        /// <summary>
        /// Connect the device
        /// </summary>
        /// <returns></returns>
        internal Task<bool> Connect() {
            return handler.Connect();
        }

        /// <summary>
        /// Disconnect the device
        /// </summary>
        internal void Disconnect() {
            handler.Disconnect();
        }

        /// <summary>
        /// Broadcast device info updates to all consumers
        /// </summary>
        /// <param name="deviceInfo"></param>
        internal void Broadcast(TInfo deviceInfo) {
            foreach (TConsumer c in consumers) {
                c.UpdateDeviceInfo(deviceInfo);
            }
        }
    }
}