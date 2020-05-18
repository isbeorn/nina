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

        public bool InitializeSerialPort(string aPortName, object client) {
            if (string.IsNullOrEmpty(aPortName)) return false;
            return base.InitializeSerialPort(aPortName.Equals("AUTO")
                ? SerialPortProvider.GetPortNames(DEVICE_ID_QUERY, addDivider: false, addGenericPorts: false).FirstOrDefault()
                : aPortName, client, newLine: "\r\n", readTimeout: 1500);
        }
    }
}
