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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK {

    public class PegasusFlatMaster : SerialSdk, IPegasusFlatMaster {
        public static readonly IPegasusFlatMaster Instance = new PegasusFlatMaster();

        protected override string LogName => "PegasusFlatMaster";

        private const string FLATMASTER_QUERY =
            @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'FTDIBUS\\VID_0403+PID_6015%FM%'";

        public ReadOnlyCollection<string> PortNames {
            get {
                var result = new List<string> { "AUTO" };
                result.AddRange(SerialPortProvider.GetPortNames(FLATMASTER_QUERY));
                return new ReadOnlyCollection<string>(result);
            }
        }

        public bool InitializeSerialPort(string aPortName, object client) {
            if (string.IsNullOrEmpty(aPortName)) return false;
            base.InitializeSerialPort(aPortName.Equals("AUTO")
                ? SerialPortProvider.GetPortNames(FLATMASTER_QUERY, addDivider: false, addGenericPorts: false).FirstOrDefault()
                : aPortName, client);
            return SerialPort != null;
        }
    }
}