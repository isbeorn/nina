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
            return handler.Connect();
        }

        /// <summary>
        /// Disconnect the device
        /// </summary>
        public void Disconnect() {
            handler.Disconnect();
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