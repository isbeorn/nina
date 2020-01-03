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

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public abstract class Command {
        public string CommandString { get; protected set; }
    }

    public class PingCommand : Command {

        public PingCommand() {
            CommandString = ">POOO\r";
        }
    }

    public class OpenCommand : Command {

        public OpenCommand() {
            CommandString = ">OOOO\r";
        }
    }

    public class CloseCommand : Command {

        public CloseCommand() {
            CommandString = ">COOO\r";
        }
    }

    public class LightOnCommand : Command {

        public LightOnCommand() {
            CommandString = ">LOOO\r";
        }
    }

    public class LightOffCommand : Command {

        public LightOffCommand() {
            CommandString = ">DOOO\r";
        }
    }

    public class SetBrightnessCommand : Command {

        public SetBrightnessCommand(double brightness) {
            CommandString = $">B{brightness:000}\r";
        }
    }

    public class GetBrightnessCommand : Command {

        public GetBrightnessCommand() {
            CommandString = ">JOOO\r";
        }
    }

    public class StateCommand : Command {

        public StateCommand() {
            CommandString = ">SOOO\r";
        }
    }

    public class FirmwareVersionCommand : Command {

        public FirmwareVersionCommand() {
            CommandString = ">VOOO\r";
        }
    }
}