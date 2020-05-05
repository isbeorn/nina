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

namespace NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK {

    public class StatusResponse : Response {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response.Equals("OK_FM")) return;
            Logger.Debug($"Response should have been OK_FM, was:{response}");
            throw new InvalidDeviceResponseException(response);
        }
    }

    public class FirmwareVersionResponse : Response {
        private double _firmwareVersion;
        public double FirmwareVersion => _firmwareVersion;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.StartsWith("V:")) throw new InvalidDeviceResponseException(response);
            if (!TryParseDouble(response.Substring(2), "firmware version", out _firmwareVersion)) throw new InvalidDeviceResponseException(response);
        }
    }

    public class OnOffResponse : Response {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response.StartsWith("E:")) return;
            Logger.Debug($"Response should have been E:{{0|1}}, was:{response}");
            throw new InvalidDeviceResponseException(response);
        }
    }

    public class SetBrightnessResponse : Response {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response.StartsWith("L:")) return;
            Logger.Debug($"Response should have been L:nnn, was:{response}");
            throw new InvalidDeviceResponseException(response);
        }
    }
}