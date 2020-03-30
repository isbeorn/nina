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

using System.Globalization;

namespace NINA.Utility.SerialCommunication {

    public abstract class Response {
        private string _deviceResponse;

        protected Response() {
            IsValid = true;
        }

        public string DeviceResponse {
            protected get => _deviceResponse;
            set {
                _deviceResponse = value;
                IsValid = ParseResponse(value);
            }
        }

        protected virtual bool ParseResponse(string response) {
            return !string.IsNullOrEmpty(response);
        }

        public virtual bool IsValid { get; protected set; }

        public virtual int Ttl => 0;

        public override string ToString() {
            return GetType().Name + $" : {_deviceResponse}";
        }

        protected static bool ParseBoolFromZeroOne(char c, string fieldName, out bool fieldValue) {
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

        protected static bool ParseDouble(string token, string fieldName, out double fieldValue) {
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out fieldValue)) return true;
            Logger.Error($"Could not parse {fieldName} from response: {token}.");
            return false;
        }

        protected static bool ParseShort(string token, string fieldName, out short fieldValue) {
            if (short.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out fieldValue)) return true;
            Logger.Error($"Could not parse {fieldName} from response: {token}.");
            return false;
        }

        protected static bool ParseInteger(string token, string fieldName, out int fieldValue) {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out fieldValue)) return true;
            Logger.Error($"Could not parse {fieldName} from response: {token}.");
            return false;
        }

        protected static bool ParseLong(string token, string fieldName, out long fieldValue) {
            if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out fieldValue)) return true;
            Logger.Error($"Could not parse {fieldName} from response: {token}.");
            return false;
        }
    }
}