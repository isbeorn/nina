#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.ViewModel.Equipment;
using NINA.ViewModel.Interfaces;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    public interface IDeviceMediator<THandler, TConsumer, TInfo> : IMediator<THandler> where THandler : IDeviceVM<TInfo> where TConsumer : IDeviceConsumer<TInfo> {

        void RegisterConsumer(TConsumer consumer);

        void RemoveConsumer(TConsumer consumer);

        Task<bool> Connect();

        void Disconnect();

        void Broadcast(TInfo deviceInfo);
    }
}