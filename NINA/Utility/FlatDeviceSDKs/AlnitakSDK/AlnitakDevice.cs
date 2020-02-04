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
using System.Collections.ObjectModel;
using System.Linq;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public class AlnitakDevice : SerialSdk, IAlnitakDevice {
        public static readonly IAlnitakDevice Instance = new AlnitakDevice();

        private ISerialPortProvider _serialPortProvider = new SerialPortProvider();

        protected override string LogName => "AlnitakFlatDevice";
        private const string ALNITAK_QUERY = @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'FTDIBUS\\VID_0403+PID_6001+A8%'";

        public ISerialPortProvider SerialPortProvider {
            set => _serialPortProvider = value;
        }

        public ReadOnlyCollection<string> PortNames => _serialPortProvider.GetPortNames(ALNITAK_QUERY);

        public bool InitializeSerialPort(string aPortName) {
            if (string.IsNullOrEmpty(aPortName)) return false;
            SerialPort = aPortName.Equals("AUTO")
                ? _serialPortProvider.GetSerialPort(_serialPortProvider.GetPortNames(ALNITAK_QUERY, addDivider: false, addGenericPorts: false).FirstOrDefault())
                : _serialPortProvider.GetSerialPort(aPortName);
            return SerialPort != null;
        }
    }
}