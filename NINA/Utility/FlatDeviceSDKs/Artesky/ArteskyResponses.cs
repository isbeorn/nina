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

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response == null || response.Length != 7) {
                Logger.Debug($"Response must not be null and length must be 7 characters long. Was: {response}");
                throw new InvalidDeviceResponseException(response);
            }

            if (response[0] == '*') return;
            Logger.Debug($"Response must start with *. Actual value: {response}");
            throw new InvalidDeviceResponseException(response);
        }

        protected bool EndsIn000(string response) {
            var result = response.Substring(4, 3).Equals("000");
            if (!result) Logger.Debug($"Response should have ended in 000. Was {response.Substring(4, 3)}");
            return result;
        }
    }

    public class LightOnResponse : ArteskyResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (EndsIn000(response) && response[1] == 'L') return;
            Logger.Debug($"Second letter of response should have been an L. Actual value: {response}");
            throw new InvalidDeviceResponseException(response);
        }
    }

    public class LightOffResponse : ArteskyResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (EndsIn000(response) && response[1] == 'D') return;
            Logger.Debug($"Second letter of response should have been a D. Actual value: {response}");
            throw new InvalidDeviceResponseException(response);
        }
    }

    public abstract class BrightnessResponse : ArteskyResponse {
        public int Brightness { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            ParseBrightness(response);
        }

        protected void ParseBrightness(string response) {
            if (!TryParseInteger(response.Substring(4, 3), "brightness", out var brightness)) throw new InvalidDeviceResponseException(response);
            if (brightness < 0 || brightness > 255) {
                Logger.Debug($"Brightness value should have been between 0 and 255. Was {brightness}");
                throw new InvalidDeviceResponseException(response);
            }

            Brightness = brightness;
        }
    }

    public class SetBrightnessResponse : BrightnessResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'B') return;
            Logger.Debug($"Second letter of response should have been a B. Actual value: {response}");
            throw new InvalidDeviceResponseException(response);
        }
    }

    public class GetBrightnessResponse : BrightnessResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'J') return;
            Logger.Debug($"Second letter of response should have been a J. Actual value: {response}");
            throw new InvalidDeviceResponseException(response);
        }
    }

    public class StateResponse : ArteskyResponse {
        private bool _lightOn;

        public bool LightOn => _lightOn;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'S' && ParseState(response)) return;
            Logger.Debug($"Second letter of response should have been an S. Actual value: {response}");
            throw new InvalidDeviceResponseException(response);
        }

        private bool ParseState(string response) {
            return TryParseBoolFromZeroOne(response[5], "sixth letter of response", out _lightOn);
        }
    }

    public class FirmwareVersionResponse : ArteskyResponse {
        private int _firmwareVersion;

        public int FirmwareVersion => _firmwareVersion;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'V' && TryParseInteger(response.Substring(4, 3), "firmware version", out _firmwareVersion)) return;
            throw new InvalidDeviceResponseException(response);
        }
    }
}