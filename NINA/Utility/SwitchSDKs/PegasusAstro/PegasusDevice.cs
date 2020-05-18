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

namespace NINA.Utility.SwitchSDKs.PegasusAstro {

    public class PegasusDevice : SerialSdk, IPegasusDevice {
        public static readonly IPegasusDevice Instance = new PegasusDevice();

        protected override string LogName => "Pegasus Astro Sdk";

        private const string UPB_QUERY =
            "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'FTDIBUS\\\\VID_0403+PID_6015%UPB%'";

        public ReadOnlyCollection<string> PortNames {
            get {
                var result = new List<string> { "AUTO" };
                result.AddRange(SerialPortProvider.GetPortNames(UPB_QUERY));
                return new ReadOnlyCollection<string>(result);
            }
        }

        public bool InitializeSerialPort(string aPortName, object client) {
            if (string.IsNullOrEmpty(aPortName)) return false;
            base.InitializeSerialPort(aPortName.Equals("AUTO")
                    ? SerialPortProvider.GetPortNames(UPB_QUERY, addDivider: false, addGenericPorts: false)
                        .FirstOrDefault()
                    : aPortName, client, newLine: "\r\n");
            return SerialPort != null;
        }

        public void Dispose() {
            base.Dispose(this);
        }
    }
}
