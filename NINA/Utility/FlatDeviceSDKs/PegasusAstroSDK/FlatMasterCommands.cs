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

namespace NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK {

    public class StatusCommand : ICommand {
        public string CommandString => "#\n";
        public bool HasResponse => true;
    }

    public class FirmwareVersionCommand : ICommand {
        public string CommandString => "V\n";
        public bool HasResponse => true;
    }

    public class OnOffCommand : ICommand {
        public bool On { get; set; }

        public string CommandString => $"E:{(On ? 1 : 0)}\n";
        public bool HasResponse => true;
    }

    public class SetBrightnessCommand : ICommand {
        public double Brightness { get; set; }
        public string CommandString => $"L:{Brightness:000}\n";
        public bool HasResponse => true;
    }
}
