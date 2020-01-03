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
using System;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public abstract class Response {
        public string Name { get; private set; }
        public bool IsValid { get; protected set; }
        public bool DeviceSupportsOpenClose { get; protected set; }

        private string _deviceResponse;

        public virtual string DeviceResponse {
            set {
                _deviceResponse = value;
                if (value == null || value.Length != 7) {
                    IsValid = false;
                    return;
                }
                if (value[0] != '*') {
                    IsValid = false;
                    return;
                }

                if (!ParseDeviceId(value)) {
                    IsValid = false;
                }
            }
        }

        protected Response() {
            IsValid = true;
        }

        private bool ParseDeviceId(string response) {
            try {
                switch (int.Parse(response.Substring(2, 2))) {
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
            } catch (Exception) {
                return false;
            }
        }

        protected bool EndsInOOO(string response) {
            return response.Substring(4, 3).Equals("OOO");
        }

        public override string ToString() {
            return this.GetType().Name + $" : {_deviceResponse}";
        }
    }

    public class PingResponse : Response {

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && (value[1] != 'P' || !EndsInOOO(value))) {
                    IsValid = false;
                }
            }
        }
    }

    public class OpenResponse : Response {

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && (value[1] != 'O' || !EndsInOOO(value))) {
                    IsValid = false;
                }
            }
        }
    }

    public class CloseResponse : Response {

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && (value[1] != 'C' || !EndsInOOO(value))) {
                    IsValid = false;
                }
            }
        }
    }

    public class LightOnResponse : Response {

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && (value[1] != 'L' || !EndsInOOO(value))) {
                    IsValid = false;
                }
            }
        }
    }

    public class LightOffResponse : Response {

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && (value[1] != 'D' || !EndsInOOO(value))) {
                    IsValid = false;
                }
            }
        }
    }

    public abstract class BrightnessResponse : Response {
        public int Brightness { get; protected set; }

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && !ParseBrightness(value)) {
                    IsValid = false;
                }
            }
        }

        protected bool ParseBrightness(string response) {
            try {
                var value = int.Parse(response.Substring(4, 3));
                if (value < 0 || value > 255) {
                    return false;
                }

                Brightness = value;
            } catch (Exception) {
                return false;
            }

            return true;
        }
    }

    public class SetBrightnessResponse : BrightnessResponse {

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && value[1] != 'B') {
                    IsValid = false;
                }
            }
        }
    }

    public class GetBrightnessResponse : BrightnessResponse {

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && value[1] != 'J') {
                    IsValid = false;
                }
            }
        }
    }

    public class StateResponse : Response {
        public bool MotorRunning { get; private set; }
        public bool LightOn { get; private set; }
        public CoverState CoverState { get; private set; }

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && (value[1] != 'S' || !ParseState(value))) {
                    IsValid = false;
                }
            }
        }

        private bool ParseState(string response) {
            switch (response[4]) {
                case '0':
                    MotorRunning = false;
                    break;

                case '1':
                    MotorRunning = true;
                    break;

                default:
                    return false;
            }

            switch (response[5]) {
                case '0':
                    LightOn = false;
                    break;

                case '1':
                    LightOn = true;
                    break;

                default:
                    return false;
            }

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
                    return false;
            }

            return true;
        }
    }

    public class FirmwareVersionResponse : Response {
        public int FirmwareVersion { get; private set; }

        public override string DeviceResponse {
            set {
                base.DeviceResponse = value;
                if (IsValid && (value[1] != 'V' || !ParseFirmwareVersion(value))) {
                    IsValid = false;
                }
            }
        }

        private bool ParseFirmwareVersion(string response) {
            try {
                FirmwareVersion = int.Parse(response.Substring(4, 3));
                return true;
            } catch (Exception) {
                return false;
            }
        }
    }
}