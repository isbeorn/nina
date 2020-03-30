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

using NINA.Model.MyFlatDevice;
using NINA.Utility.SerialCommunication;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public abstract class AlnitakResponse : Response {
        public string Name { get; private set; }
        public bool DeviceSupportsOpenClose { get; protected set; }

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (response == null || response.Length != 7) {
                Logger.Debug($"Response must not be null and length must be 7 characters long. Was: {response}");
                return false;
            }
            if (response[0] != '*') {
                Logger.Debug($"Response must start with *. Actual value:{response}");
                return false;
            }

            return ParseDeviceId(response);
        }

        private bool ParseDeviceId(string response) {
            if (!ParseInteger(response.Substring(2, 2), "device id", out var deviceType)) return false;
            switch (deviceType) {
                case 10:
                    Name = "Flat-Man_XL";
                    DeviceSupportsOpenClose = false;
                    return true;

                case 15:
                    Name = "Flat-Man_L";
                    DeviceSupportsOpenClose = false;
                    return true;

                case 19:
                    Name = "Flat-Man";
                    DeviceSupportsOpenClose = false;
                    return true;

                case 98:
                    Name = "Flip-Mask/Remote Dust Cover";
                    DeviceSupportsOpenClose = true;
                    return true;

                case 99:
                    Name = "Flip-Flat";
                    DeviceSupportsOpenClose = true;
                    return true;

                default:
                    Name = "Unknown device";
                    DeviceSupportsOpenClose = false;
                    return false;
            }
        }

        protected bool EndsInOOO(string response) {
            var result = response.Substring(4, 3).Equals("OOO");
            if (!result) Logger.Debug($"Response should have ended in OOO. Was {response}");
            return result;
        }
    }

    public class PingResponse : AlnitakResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response[1] == 'P' && EndsInOOO(response)) return true;
            Logger.Debug($"Second letter of response should have been a P. Actual value:{response}");
            return false;
        }
    }

    public class OpenResponse : AlnitakResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response[1] == 'O' && EndsInOOO(response)) return true;
            Logger.Debug($"Second letter of response should have been an O. Actual value:{response}");
            return false;
        }
    }

    public class CloseResponse : AlnitakResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response[1] == 'C' && EndsInOOO(response)) return true;
            Logger.Debug($"Second letter of response should have been a C. Actual value:{response}");
            return false;
        }
    }

    public class LightOnResponse : AlnitakResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && EndsInOOO(response) && response[1] == 'L') return true;
            Logger.Debug($"Second letter of response should have been an L. Actual value:{response}");
            return false;
        }
    }

    public class LightOffResponse : AlnitakResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && EndsInOOO(response) && response[1] == 'D') return true;
            Logger.Debug($"Second letter of response should have been a D. Actual value:{response}");
            return false;
        }
    }

    public abstract class BrightnessResponse : AlnitakResponse {
        public int Brightness { get; protected set; }

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            return IsValid && ParseBrightness(response);
        }

        protected bool ParseBrightness(string response) {
            if (!ParseInteger(response.Substring(4, 3), "brightness", out var brightness)) return false;
            if (brightness < 0 || brightness > 255) {
                Logger.Debug($"Brightness value should have been between 0 and 255. Was {brightness}");
                return false;
            }

            Brightness = brightness;
            return true;
        }
    }

    public class SetBrightnessResponse : BrightnessResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response[1] == 'B') return true;
            Logger.Debug($"Second letter of response should have been a B. Actual value:{response}");
            return false;
        }
    }

    public class GetBrightnessResponse : BrightnessResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response[1] == 'J') return true;
            Logger.Debug($"Second letter of response should have been a J. Actual value:{response}");
            return false;
        }
    }

    public class StateResponse : AlnitakResponse {
        private bool _motorRunning;
        private bool _lightOn;

        public bool MotorRunning => _motorRunning;

        public bool LightOn => _lightOn;

        public CoverState CoverState { get; private set; }

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response[1] == 'S' && ParseState(response)) return true;
            Logger.Debug($"Second letter of response should have been an S. Actual value:{response}");
            return false;
        }

        private bool ParseState(string response) {
            if (!ParseBoolFromZeroOne(response[4], "fifth letter of response", out _motorRunning)) return false;
            if (!ParseBoolFromZeroOne(response[5], "sixth letter of response", out _lightOn)) return false;

            switch (response[6]) {
                case '0':
                    CoverState = CoverState.NeitherOpenNorClosed;
                    break;

                case '1':
                    CoverState = CoverState.Closed;
                    break;

                case '2':
                    CoverState = CoverState.Open;
                    break;

                case '3':
                    CoverState = CoverState.Unknown;
                    break;

                default:
                    Logger.Debug($"Seventh letter of response should have been 0, 1, 2 or 3. Actual value:{response}");
                    return false;
            }

            return true;
        }
    }

    public class FirmwareVersionResponse : AlnitakResponse {
        private int _firmwareVersion;

        public int FirmwareVersion => _firmwareVersion;

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            return IsValid && response[1] == 'V' && ParseInteger(response.Substring(4, 3), "firmware version", out _firmwareVersion);
        }
    }
}