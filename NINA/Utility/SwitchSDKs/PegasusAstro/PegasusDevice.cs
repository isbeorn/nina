#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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