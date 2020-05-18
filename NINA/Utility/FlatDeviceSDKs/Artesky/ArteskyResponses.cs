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
