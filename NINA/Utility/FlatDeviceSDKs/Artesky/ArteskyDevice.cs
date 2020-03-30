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
using System.Threading.Tasks;

namespace NINA.Utility.FlatDeviceSDKs.Artesky {

    public class ArteskyUSBFlatBox : SerialSdk, IArteskyFlatBox {
        public static readonly IArteskyFlatBox Instance = new ArteskyUSBFlatBox();

        protected override string LogName => "ArteskyFlatBox";

        // Artesky Flat Boxes are implemented using an Arduino Uno R3
        private const string DEVICE_ID_QUERY = @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%VID_2341&PID_0043%'";

        public ReadOnlyCollection<string> PortNames {
            get {
                var result = new List<string> { "AUTO" };
                result.AddRange(SerialPortProvider.GetPortNames(DEVICE_ID_QUERY));
                return new ReadOnlyCollection<string>(result);
            }
        }

        public async Task<bool> InitializeSerialPort(string aPortName, object client) {
            if (string.IsNullOrEmpty(aPortName)) return false;
            return base.InitializeSerialPort(aPortName.Equals("AUTO")
                ? SerialPortProvider.GetPortNames(DEVICE_ID_QUERY, addDivider: false, addGenericPorts: false).FirstOrDefault()
                : aPortName, client, newLine: "\r\n", readTimeout: 1500);
        }
    }
}