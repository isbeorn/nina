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
using System.Collections.ObjectModel;
using NINA.Utility.SerialCommunication;

namespace NINA.Utility.SwitchSDKs.PegasusAstro {

    public abstract class PegasusUpbv2Response : Response {

        protected ReadOnlyCollection<bool> ParseAutoDewStatus(short autoDewInteger) {
            switch (autoDewInteger) {
                case 0:
                    return new ReadOnlyCollection<bool>(new[] { false, false, false });

                case 1:
                    return new ReadOnlyCollection<bool>(new[] { true, true, true });

                case 2:
                    return new ReadOnlyCollection<bool>(new[] { true, false, false });

                case 3:
                    return new ReadOnlyCollection<bool>(new[] { false, true, false });

                case 4:
                    return new ReadOnlyCollection<bool>(new[] { false, false, true });

                case 5:
                    return new ReadOnlyCollection<bool>(new[] { true, true, false });

                case 6:
                    return new ReadOnlyCollection<bool>(new[] { true, false, true });

                case 7:
                    return new ReadOnlyCollection<bool>(new[] { false, true, true });

                default:
                    Logger.Error($"invalid auto dew status {autoDewInteger}");
                    return new ReadOnlyCollection<bool>(new[] { false, false, false });
            }
        }
    }

    public class FirmwareVersionResponse : PegasusUpbv2Response {
        public double FirmwareVersion { get; protected set; }

        protected override bool ParseResponse(string response) {
            try {
                FirmwareVersion = double.Parse(response);
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public class StatusResponse : PegasusUpbv2Response {
        public override int Ttl => 100;
        public string DeviceName { get; protected set; }
        public double DeviceInputVoltage { get; protected set; }
        public double DeviceCurrentAmpere { get; protected set; }
        public int DevicePower { get; protected set; }
        public double Temperature { get; protected set; }
        public double Humidity { get; protected set; }
        public double DewPoint { get; protected set; }
        public ReadOnlyCollection<bool> PowerPortOn { get; protected set; }
        public ReadOnlyCollection<bool> UsbPortOn { get; protected set; }
        public ReadOnlyCollection<short> DewHeaterDutyCycle { get; protected set; }
        public ReadOnlyCollection<double> PortPowerFlow { get; protected set; }
        public ReadOnlyCollection<double> DewHeaterPowerFlow { get; protected set; }
        public ReadOnlyCollection<bool> PortOverCurrent { get; protected set; }
        public ReadOnlyCollection<bool> DewHeaterOverCurrent { get; protected set; }
        public ReadOnlyCollection<bool> AutoDewStatus { get; protected set; }

        protected override bool ParseResponse(string value) {
            if (string.IsNullOrEmpty(value)) return false;
            try {
                var tokens = value.Split(':');
                DeviceName = tokens[0];
                DeviceInputVoltage = double.Parse(tokens[1]);
                DeviceCurrentAmpere = double.Parse(tokens[2]);
                DevicePower = int.Parse(tokens[3]);
                Temperature = double.Parse(tokens[4]);
                Humidity = double.Parse(tokens[5]);
                DewPoint = double.Parse(tokens[6]);
                var tempBool = new bool[4];
                for (var i = 0; i < 4; i++) {
                    tempBool[i] = tokens[7][i] == '1';
                }
                PowerPortOn = new ReadOnlyCollection<bool>(tempBool);

                tempBool = new bool[6];
                for (var i = 0; i < 6; i++) {
                    tempBool[i] = tokens[8][i] == '1';
                }
                UsbPortOn = new ReadOnlyCollection<bool>(tempBool);

                var tempShort = new[] {
                    short.Parse(tokens[9]),
                    short.Parse(tokens[10]), short.Parse(tokens[11])
                };

                DewHeaterDutyCycle = new ReadOnlyCollection<short>(tempShort);

                var tempDouble = new[] {
                    double.Parse(tokens[12]) / 300d, double.Parse(tokens[13]) / 300d,
                    double.Parse(tokens[14]) / 300d, double.Parse(tokens[15]) / 300d
                };

                PortPowerFlow = new ReadOnlyCollection<double>(tempDouble);

                tempDouble = new[] {
                    double.Parse(tokens[16]) / 300d, double.Parse(tokens[17]) / 300d,
                    double.Parse(tokens[18]) / 600d
                };

                DewHeaterPowerFlow = new ReadOnlyCollection<double>(tempDouble);

                tempBool = new bool[4];
                for (var i = 0; i < 4; i++) {
                    tempBool[i] = tokens[19][i] == '1';
                }
                PortOverCurrent = new ReadOnlyCollection<bool>(tempBool);

                tempBool = new bool[3];
                for (var i = 0; i < 3; i++) {
                    tempBool[i] = tokens[19][i + 4] == '1';
                }
                DewHeaterOverCurrent = new ReadOnlyCollection<bool>(tempBool);

                AutoDewStatus = ParseAutoDewStatus(short.Parse(tokens[20]));
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }

            return true;
        }
    }

    public class SetPowerResponse : PegasusUpbv2Response {
        public bool On { get; protected set; }
        public short SwitchNumber { get; protected set; }

        protected override bool ParseResponse(string response) {
            try {
                if (response[0] != 'P' || response[2] != ':') return false;
                SwitchNumber = (short)(short.Parse(response[1].ToString()) - 1);
                On = response[3] == '1';
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public class SetUsbPowerResponse : PegasusUpbv2Response {
        public bool On { get; protected set; }
        public short SwitchNumber { get; protected set; }

        protected override bool ParseResponse(string response) {
            try {
                if (response[0] != 'U' || response[2] != ':') return false;
                SwitchNumber = (short)(short.Parse(response[1].ToString()) - 1);
                On = response[3] == '1';
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public class PowerStatusResponse : PegasusUpbv2Response {
        public double VariableVoltage { get; protected set; }
        public ReadOnlyCollection<bool> PowerStatusOnBoot { get; protected set; }

        protected override bool ParseResponse(string response) {
            try {
                if (!response.StartsWith("PS:")) return false;
                var temp = new bool[4];
                for (var i = 0; i < 4; i++) {
                    temp[i] = response[i + 3] == '1';
                }
                PowerStatusOnBoot = new ReadOnlyCollection<bool>(temp);

                var voltage = double.Parse(response.Substring(8));
                VariableVoltage = voltage <= 12d ? voltage : 0;
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public class SetVariableVoltageResponse : PegasusUpbv2Response {
        public double VariableVoltage { get; protected set; }

        protected override bool ParseResponse(string response) {
            try {
                if (!response.StartsWith("P8:")) return false;

                var voltage = double.Parse(response.Substring(3));
                VariableVoltage = voltage <= 12d ? voltage : 0;
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public class SetDewHeaterPowerResponse : PegasusUpbv2Response {
        public short DewHeaterNumber { get; protected set; }
        public double DutyCycle { get; protected set; }

        protected override bool ParseResponse(string response) {
            try {
                if (!response.StartsWith("P")) return false;
                DewHeaterNumber = (short)(short.Parse($"{response[1]}") - 5);

                var dutyCycle = double.Parse(response.Substring(3));
                DutyCycle = dutyCycle / 255d * 100d;
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public class PowerConsumptionResponse : PegasusUpbv2Response {
        public override int Ttl => 100;
        public double AveragePower { get; protected set; }
        public double AmpereHours { get; protected set; }
        public double WattHours { get; protected set; }
        public TimeSpan UpTime { get; protected set; }

        protected override bool ParseResponse(string response) {
            try {
                AveragePower = double.Parse(response.Split(':')[0]);
                AmpereHours = double.Parse(response.Split(':')[1]);
                WattHours = double.Parse(response.Split(':')[2]);
                UpTime = TimeSpan.FromMilliseconds(long.Parse(response.Split(':')[3]));
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public class SetAutoDewResponse : PegasusUpbv2Response {
        public ReadOnlyCollection<bool> AutoDewStatus;

        protected override bool ParseResponse(string response) {
            try {
                if (!response.StartsWith("PD:")) return false;
                AutoDewStatus = ParseAutoDewStatus(short.Parse(response.Substring(3)));
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }
}