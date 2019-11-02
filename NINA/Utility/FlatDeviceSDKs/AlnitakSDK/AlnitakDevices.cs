using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public static class AlnitakDevices {

        public static List<string> GetDevices() {
            //supporting up to 4 devices, if more are needed, just add more strings below
            return new List<string>()
            {
                "Alnitak1;COM1",
                "Alnitak2;COM2",
                "Alnitak3;COM3",
                "Alnitak4;COM4"
            };
        }
    }
}