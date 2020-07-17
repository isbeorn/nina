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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public class AlnitakDevice : SerialSdk, IAlnitakDevice {
        public static readonly IAlnitakDevice Instance = new AlnitakDevice();

        protected override string LogName => "AlnitakFlatDevice";
        private const string ALNITAK_QUERY = @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'FTDIBUS\\VID_0403+PID_6001+A8%'";

        public ReadOnlyCollection<string> PortNames {
            get {
                var result = new List<string> { "AUTO" };
                result.AddRange(SerialPortProvider.GetPortNames(ALNITAK_QUERY));
                return new ReadOnlyCollection<string>(result);
            }
        }

        public async Task<bool> InitializeSerialPort(string aPortName, object client, int rtsDelay = 2000) {
            if (string.IsNullOrEmpty(aPortName)) return false;
            base.InitializeSerialPort(aPortName.Equals("AUTO")
                ? SerialPortProvider.GetPortNames(ALNITAK_QUERY, addDivider: false, addGenericPorts: false).FirstOrDefault()
                : aPortName, client);
            if (SerialPort == null) return false;
            await Task.Delay(rtsDelay); //need to wait {rtsDelay} seconds after port is open before setting RtsEnable or it won't work
            SerialPort.RtsEnable = false;
            return true;
        }
    }
}