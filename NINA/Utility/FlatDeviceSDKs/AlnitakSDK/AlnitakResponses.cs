#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyFlatDevice;
using NINA.Utility.SerialCommunication;

namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public abstract class AlnitakResponse : Response {
        public string Name { get; private set; }
        public bool DeviceSupportsOpenClose { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response == null || response.Length != 7) {
                Logger.Debug($"Response must not be null and length must be 7 characters long. Was: {response}");
                throw new InvalidDeviceResponseException(response);
            }

            if (response[0] != '*') {
                Logger.Debug($"Response must start with *. Actual value:{response}");
                throw new InvalidDeviceResponseException(response);
            }

            ParseDeviceId(response);
        }

        private void ParseDeviceId(string response) {
            if (!TryParseInteger(response.Substring(2, 2), "device id", out var deviceType))
                throw new InvalidDeviceResponseException(response);
            switch (deviceType) {
                case 10:
                    Name = "Flat-Man_XL";
                    DeviceSupportsOpenClose = false;
                    break;

                case 15:
                    Name = "Flat-Man_L";
                    DeviceSupportsOpenClose = false;
                    break;

                case 19:
                    Name = "Flat-Man";
                    DeviceSupportsOpenClose = false;
                    break;

                case 98:
                    Name = "Flip-Mask/Remote Dust Cover";
                    DeviceSupportsOpenClose = true;
                    break;

                case 99:
                    Name = "Flip-Flat";
                    DeviceSupportsOpenClose = true;
                    break;

                default:
                    Name = "Unknown device";
                    DeviceSupportsOpenClose = false;
                    throw new InvalidDeviceResponseException(response);
            }
        }

        protected bool EndsInOOO(string response) {
            if (response.Substring(4, 3).Equals("OOO")) return true;
            Logger.Debug($"Response should have ended in OOO. Was {response}");
            throw new InvalidDeviceResponseException();
        }
    }

    public class PingResponse : AlnitakResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'P' && EndsInOOO(response)) return;
            Logger.Debug($"Second letter of response should have been a P. Actual value:{response}");
            throw new InvalidDeviceResponseException();
        }
    }

    public class OpenResponse : AlnitakResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'O' && EndsInOOO(response)) return;
            Logger.Debug($"Second letter of response should have been an O. Actual value:{response}");
            throw new InvalidDeviceResponseException();
        }
    }

    public class CloseResponse : AlnitakResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'C' && EndsInOOO(response)) return;
            Logger.Debug($"Second letter of response should have been a C. Actual value:{response}");
            throw new InvalidDeviceResponseException();
        }
    }

    public class LightOnResponse : AlnitakResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (EndsInOOO(response) && response[1] == 'L') return;
            Logger.Debug($"Second letter of response should have been an L. Actual value:{response}");
            throw new InvalidDeviceResponseException();
        }
    }

    public class LightOffResponse : AlnitakResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (EndsInOOO(response) && response[1] == 'D') return;
            Logger.Debug($"Second letter of response should have been a D. Actual value:{response}");
            throw new InvalidDeviceResponseException();
        }
    }

    public abstract class BrightnessResponse : AlnitakResponse {
        public int Brightness { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            ParseBrightness(response);
        }

        protected void ParseBrightness(string response) {
            if (!TryParseInteger(response.Substring(4, 3), "brightness", out var brightness)) throw new InvalidDeviceResponseException();
            if (brightness < 0 || brightness > 255) {
                Logger.Debug($"Brightness value should have been between 0 and 255. Was {brightness}");
                throw new InvalidDeviceResponseException();
            }

            Brightness = brightness;
        }
    }

    public class SetBrightnessResponse : BrightnessResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'B') return;
            Logger.Debug($"Second letter of response should have been a B. Actual value:{response}");
            throw new InvalidDeviceResponseException();
        }
    }

    public class GetBrightnessResponse : BrightnessResponse {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'J') return;
            Logger.Debug($"Second letter of response should have been a J. Actual value:{response}");
            throw new InvalidDeviceResponseException();
        }
    }

    public class StateResponse : AlnitakResponse {
        private bool _motorRunning;
        private bool _lightOn;

        public bool MotorRunning => _motorRunning;

        public bool LightOn => _lightOn;

        public CoverState CoverState { get; private set; }

        public override int Ttl => 100;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'S' && ParseState(response)) return;
            Logger.Debug($"Second letter of response should have been an S. Actual value:{response}");
            throw new InvalidDeviceResponseException();
        }

        private bool ParseState(string response) {
            if (!TryParseBoolFromZeroOne(response[4], "fifth letter of response", out _motorRunning)) return false;
            if (!TryParseBoolFromZeroOne(response[5], "sixth letter of response", out _lightOn)) return false;

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

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response[1] == 'V' && TryParseInteger(response.Substring(4, 3), "firmware version", out _firmwareVersion)) return;
            throw new InvalidDeviceResponseException();
        }
    }
}