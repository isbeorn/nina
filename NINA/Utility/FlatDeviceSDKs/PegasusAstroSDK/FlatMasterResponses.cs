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
