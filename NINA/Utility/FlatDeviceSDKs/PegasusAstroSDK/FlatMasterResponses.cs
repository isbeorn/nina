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

using System;
using NINA.Utility.SerialCommunication;

namespace NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK {

    public class StatusResponse : Response {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response.Equals("OK_FM")) return true;
            IsValid = false;
            Logger.Debug($"Response should have been OK_FM, was:{response}");
            return false;
        }
    }

    public class FirmwareVersionResponse : Response {
        public double FirmwareVersion { get; set; }

        protected override bool ParseResponse(string response) {
            try {
                IsValid &= base.ParseResponse(response);
                if (!IsValid || !response.StartsWith("V:")) return false;
                FirmwareVersion = double.Parse(response.Substring(2));
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public class OnOffResponse : Response {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response.StartsWith("E:")) return true;
            Logger.Debug($"Response should have been E:{{0|1}}, was:{response}");
            return false;
        }
    }

    public class SetBrightnessResponse : Response {

        protected override bool ParseResponse(string response) {
            IsValid &= base.ParseResponse(response);
            if (IsValid && response.StartsWith("L:")) return true;
            Logger.Debug($"Response should have been L:nnn, was:{response}");
            return false;
        }
    }
}