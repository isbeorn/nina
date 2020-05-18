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

namespace NINA.Utility.FlatDeviceSDKs.Artesky {

    public class LightOnCommand : ICommand {
        public string CommandString => ">LOOO\r";
        public bool HasResponse => true;
    }

    public class LightOffCommand : ICommand {
        public string CommandString => ">DOOO\r";
        public bool HasResponse => true;
    }

    public class SetBrightnessCommand : ICommand {
        public double Brightness { get; set; }
        public string CommandString => $">B{Brightness:000}\r";
        public bool HasResponse => true;
    }

    public class GetBrightnessCommand : ICommand {
        public string CommandString => ">JOOO\r";
        public bool HasResponse => true;
    }

    public class StateCommand : ICommand {
        public string CommandString => ">SOOO\r";
        public bool HasResponse => true;
    }

    public class FirmwareVersionCommand : ICommand {
        public string CommandString => ">VOOO\r";
        public bool HasResponse => true;
    }
}
