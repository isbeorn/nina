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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Equipment.Interfaces.ViewModel {

    public interface IDeviceVM<TInfo> {

        Task<IList<string>> Rescan();

        Task<bool> Connect();

        Task Disconnect();

        TInfo GetDeviceInfo();

        string Action(string actionName, string actionParameters);

        string SendCommandString(string command, bool raw = true);

        bool SendCommandBool(string command, bool raw = true);

        void SendCommandBlind(string command, bool raw = true);
        IDevice GetDevice();

        event Func<object, EventArgs, Task> Connected;
        event Func<object, EventArgs, Task> Disconnected;
    }
}