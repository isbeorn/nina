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

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public class PingCommand : ICommand {
        public string CommandString => ">POOO\r";
    }

    public class OpenCommand : ICommand {
        public string CommandString => ">OOOO\r";
    }

    public class CloseCommand : ICommand {
        public string CommandString => ">COOO\r";
    }

    public class LightOnCommand : ICommand {
        public string CommandString => ">LOOO\r";
    }

    public class LightOffCommand : ICommand {
        public string CommandString => ">DOOO\r";
    }

    public class SetBrightnessCommand : ICommand {
        public double Brightness { get; set; }
        public string CommandString => $">B{Brightness:000}\r";
    }

    public class GetBrightnessCommand : ICommand {
        public string CommandString => ">JOOO\r";
    }

    public class StateCommand : ICommand {
        public string CommandString => ">SOOO\r";
    }

    public class FirmwareVersionCommand : ICommand {
        public string CommandString => ">VOOO\r";
    }
}