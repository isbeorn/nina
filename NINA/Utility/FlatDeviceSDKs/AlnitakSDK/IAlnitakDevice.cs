#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.SerialCommunication;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public interface IAlnitakDevice : ISerialSdk {
        ReadOnlyCollection<string> PortNames { get; }

        Task<bool> InitializeSerialPort(string portName, object client, int rtsDelay = 2000);
    }
}
