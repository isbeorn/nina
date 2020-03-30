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

namespace NINA.Utility.FlatDeviceSDKs.Artesky {

    public abstract class ArteskyResponse : Response {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (response == null || response.Length != 7) {
                Logger.Debug($"Response must not be null and length must be 7 characters long. Was: {response}");
                return false;
            }
            if (response[0] != '*') {
                Logger.Debug($"Response must start with *. Actual value: {response}");
                return false;
            }

            return true;
        }

        protected bool EndsIn000(string response) {
            var result = response.Substring(4, 3).Equals("000");
            if (!result) Logger.Debug($"Response should have ended in 000. Was {response.Substring(4, 3)}");
            return result;
        }
    }

    public class LightOnResponse : ArteskyResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && EndsIn000(response) && response[1] == 'L') return true;
            Logger.Debug($"Second letter of response should have been an L. Actual value: {response}");
            return false;
        }
    }

    public class LightOffResponse : ArteskyResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && EndsIn000(response) && response[1] == 'D') return true;
            Logger.Debug($"Second letter of response should have been a D. Actual value: {response}");
            return false;
        }
    }

    public abstract class BrightnessResponse : ArteskyResponse {
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
            Logger.Debug($"Second letter of response should have been a B. Actual value: {response}");
            return false;
        }
    }

    public class GetBrightnessResponse : BrightnessResponse {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response[1] == 'J') return true;
            Logger.Debug($"Second letter of response should have been a J. Actual value: {response}");
            return false;
        }
    }

    public class StateResponse : ArteskyResponse {
        private bool _lightOn;

        public bool LightOn => _lightOn;

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response[1] == 'S' && ParseState(response)) return true;
            Logger.Debug($"Second letter of response should have been an S. Actual value: {response}");
            return false;
        }

        private bool ParseState(string response) {
            if (!ParseBoolFromZeroOne(response[5], "sixth letter of response", out _lightOn)) return false;
            return true;
        }
    }

    public class FirmwareVersionResponse : ArteskyResponse {
        private int _firmwareVersion;

        public int FirmwareVersion => _firmwareVersion;

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            return IsValid && response[1] == 'V' && ParseInteger(response.Substring(4, 3), "firmware version", out _firmwareVersion);
        }
    }
}