#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Globalization;

namespace NINA.Utility.SerialCommunication {

    public abstract class Response {
        private string _deviceResponse;

        public string DeviceResponse {
            protected get => _deviceResponse;
            set {
                _deviceResponse = value;
                ParseResponse(value);
            }
        }

        protected virtual void ParseResponse(string response) {
            if (!string.IsNullOrEmpty(response)) return;
            Logger.Error("response was null or empty");
            throw new InvalidDeviceResponseException();
        }

        public virtual int Ttl => 0;

        public override string ToString() {
            return GetType().Name + $" : {_deviceResponse}";
        }

        protected static bool TryParseBoolFromZeroOne(char c, string fieldName, out bool fieldValue) {
            switch (c) {
                case '0':
                    fieldValue = false;
                    break;

                case '1':
                    fieldValue = true;
                    break;

                default:
                    Logger.Error($"Could not parse {fieldName} from response: {c}.");
                    fieldValue = false;
                    return false;
            }

            return true;
        }

        protected static bool TryParseDouble(string token, string fieldName, out double fieldValue) {
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out fieldValue)) return true;
            Logger.Error($"Could not parse {fieldName} from response: {token}.");
            return false;
        }

        protected static bool TryParseShort(string token, string fieldName, out short fieldValue) {
            if (short.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out fieldValue)) return true;
            Logger.Error($"Could not parse {fieldName} from response: {token}.");
            return false;
        }

        protected static bool TryParseInteger(string token, string fieldName, out int fieldValue) {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out fieldValue)) return true;
            Logger.Error($"Could not parse {fieldName} from response: {token}.");
            return false;
        }

        protected static bool TryParseLong(string token, string fieldName, out long fieldValue) {
            if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out fieldValue)) return true;
            Logger.Error($"Could not parse {fieldName} from response: {token}.");
            return false;
        }
    }
}
