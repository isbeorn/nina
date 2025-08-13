#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces.Mediator {

    public interface IDeviceMediator<THandler, TConsumer, TInfo> : IMediator<THandler> where THandler : IDeviceVM<TInfo> where TConsumer : IDeviceConsumer<TInfo> {

        void RegisterConsumer(TConsumer consumer);

        void RemoveConsumer(TConsumer consumer);

        Task<IList<string>> Rescan();

        Task<bool> Connect();

        Task Disconnect();

        void Broadcast(TInfo deviceInfo);

        TInfo GetInfo();

        string Action(string actionName, string actionParameters);

        string SendCommandString(string command, bool raw = true);

        bool SendCommandBool(string command, bool raw = true);

        void SendCommandBlind(string command, bool raw = true);

        /// <summary>
        /// Returns the device instance from the handler for direct access
        /// Please use this only when no other method is available via the viewmodel
        /// </summary>
        IDevice GetDevice();

        event Func<object, EventArgs, Task> Connected;
        event Func<object, EventArgs, Task> Disconnected;
    }
}