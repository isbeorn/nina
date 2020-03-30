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
using System;
using System.Collections.ObjectModel;

namespace NINA.Utility.SwitchSDKs.PegasusAstro {

    public abstract class PegasusUpbv2Response : Response {

        protected static ReadOnlyCollection<bool> ParseAutoDewStatus(short autoDewInteger) {
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
        private double _firmwareVersion;

        public double FirmwareVersion => _firmwareVersion;

        protected override bool ParseResponse(string response) {
            return ParseDouble(response, "firmware version", out _firmwareVersion);
        }
    }

    public class StatusResponse : PegasusUpbv2Response {
        private double _deviceInputVoltage;
        private double _deviceCurrentAmpere;
        private int _devicePower;
        private double _temperature;
        private double _humidity;
        private double _dewPoint;
        public override int Ttl => 100;
        public string DeviceName { get; protected set; }

        public double DeviceInputVoltage => _deviceInputVoltage;

        public double DeviceCurrentAmpere => _deviceCurrentAmpere;

        public int DevicePower => _devicePower;

        public double Temperature => _temperature;

        public double Humidity => _humidity;

        public double DewPoint => _dewPoint;

        public ReadOnlyCollection<bool> PowerPortOn { get; protected set; }
        public ReadOnlyCollection<bool> UsbPortOn { get; protected set; }
        public ReadOnlyCollection<short> DewHeaterDutyCycle { get; protected set; }
        public ReadOnlyCollection<double> PortPowerFlow { get; protected set; }
        public ReadOnlyCollection<double> DewHeaterPowerFlow { get; protected set; }
        public ReadOnlyCollection<bool> PortOverCurrent { get; protected set; }
        public ReadOnlyCollection<bool> DewHeaterOverCurrent { get; protected set; }
        public ReadOnlyCollection<bool> AutoDewStatus { get; protected set; }

        protected override bool ParseResponse(string value) {
            if (string.IsNullOrEmpty(value)) {
                Logger.Error("Null or Empty response.");
                return false;
            }

            var tokens = value.Split(':');
            if (tokens.Length != 21) {
                Logger.Error($"Wrong number of tokens. Should have been 21, was {tokens.Length}");
                return false;
            }

            DeviceName = tokens[0];
            if (!ParseDouble(tokens[1], "device input voltage", out _deviceInputVoltage)) return false;
            if (!ParseDouble(tokens[2], "device current ampere", out _deviceCurrentAmpere)) return false;
            if (!ParseInteger(tokens[3], "device power", out _devicePower)) return false;
            if (!ParseDouble(tokens[4], "temperature", out _temperature)) return false;
            if (!ParseDouble(tokens[5], "humidity", out _humidity)) return false;
            if (!ParseDouble(tokens[6], "dew point", out _dewPoint)) return false;

            var tempBool = new bool[4];
            for (var i = 0; i < 4; i++) {
                if (!ParseBoolFromZeroOne(tokens[7][i], "power port status", out tempBool[i])) return false;
            }
            PowerPortOn = new ReadOnlyCollection<bool>(tempBool);

            tempBool = new bool[6];
            for (var i = 0; i < 6; i++) {
                if (!ParseBoolFromZeroOne(tokens[8][i], "usb port status", out tempBool[i])) return false;
            }
            UsbPortOn = new ReadOnlyCollection<bool>(tempBool);

            var tempShort = new short[3];
            for (var i = 0; i < 3; i++) {
                if (!ParseShort(tokens[i + 9], "dew heater cycle", out tempShort[i])) return false;
            }
            DewHeaterDutyCycle = new ReadOnlyCollection<short>(tempShort);

            var tempDouble = new double[4];
            for (var i = 0; i < 4; i++) {
                if (!ParseDouble(tokens[i + 12], "power port current", out tempDouble[i])) return false;
                tempDouble[i] /= 300d;
            }
            PortPowerFlow = new ReadOnlyCollection<double>(tempDouble);

            tempDouble = new double[3];
            for (var i = 0; i < 3; i++) {
                if (!ParseDouble(tokens[i + 16], "dew heater port current", out tempDouble[i])) return false;
                tempDouble[i] /= 300d;
            }
            tempDouble[2] /= 2d;// different MOSFET, needs to be divided by 600

            DewHeaterPowerFlow = new ReadOnlyCollection<double>(tempDouble);

            tempBool = new bool[4];
            for (var i = 0; i < 4; i++) {
                if (!ParseBoolFromZeroOne(tokens[19][i], "port over current status", out tempBool[i])) return false;
            }
            PortOverCurrent = new ReadOnlyCollection<bool>(tempBool);

            tempBool = new bool[3];
            for (var i = 0; i < 3; i++) {
                if (!ParseBoolFromZeroOne(tokens[19][i + 4], "dew heater over current status", out tempBool[i])) return false;
            }
            DewHeaterOverCurrent = new ReadOnlyCollection<bool>(tempBool);

            if (!ParseShort(tokens[20], "auto-dew status", out var autoDewStatus)) return false;
            AutoDewStatus = ParseAutoDewStatus(autoDewStatus);
            return true;
        }
    }

    public class SetPowerResponse : PegasusUpbv2Response {
        private bool _on;

        public bool On => _on;

        public short SwitchNumber { get; protected set; }

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || response.Length < 4 || response[0] != 'P' || response[2] != ':') {
                Logger.Error($"Could not parse SetPower Response {response}");
                return false;
            }
            if (!ParseShort(response[1].ToString(), "switch number", out var switchNumber)) return false;
            SwitchNumber = (short)(switchNumber - 1);
            return ParseBoolFromZeroOne(response[3], "switch status", out _on);
        }
    }

    public class SetUsbPowerResponse : PegasusUpbv2Response {
        private bool _on;

        public bool On => _on;

        public short SwitchNumber { get; protected set; }

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || response.Length < 4 || response[0] != 'U' || response[2] != ':') {
                Logger.Error($"Could not parse SetUsbPower Response {response}");
                return false;
            }
            if (!ParseShort(response[1].ToString(), "switch number", out var switchNumber)) return false;
            SwitchNumber = (short)(switchNumber - 1);
            return ParseBoolFromZeroOne(response[3], "switch status", out _on);
        }
    }

    public class PowerStatusResponse : PegasusUpbv2Response {
        public double VariableVoltage { get; protected set; }
        public ReadOnlyCollection<bool> PowerStatusOnBoot { get; protected set; }

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || !response.StartsWith("PS:") || response.Length < 9) {
                Logger.Error($"Could not parse PowerStatus Response {response}");
                return false;
            }
            var temp = new bool[4];
            for (var i = 0; i < 4; i++) {
                if (!ParseBoolFromZeroOne(response[i + 3], "power status", out temp[i])) return false;
            }
            PowerStatusOnBoot = new ReadOnlyCollection<bool>(temp);

            if (!ParseDouble(response.Substring(8), "variable voltage value", out var voltage)) return false;
            VariableVoltage = voltage <= 12d ? voltage : 0;
            return true;
        }
    }

    public class SetVariableVoltageResponse : PegasusUpbv2Response {
        public double VariableVoltage { get; protected set; }

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || !response.StartsWith("P8:") || response.Length < 4) {
                Logger.Error($"Could not parse SetVariableVoltage Response {response}");
                return false;
            }

            if (!ParseDouble(response.Substring(3), "variable voltage value", out var voltage)) return false;
            VariableVoltage = voltage <= 12d ? voltage : 0;
            return true;
        }
    }

    public class SetDewHeaterPowerResponse : PegasusUpbv2Response {
        public short DewHeaterNumber { get; protected set; }

        public double DutyCycle { get; protected set; }

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || !response.StartsWith("P") || response.Length < 4) {
                Logger.Error($"Could not parse SetDewHeaterPower Response {response}");
                return false;
            }

            if (!ParseShort(response[1].ToString(), "dew heater number", out var dewHeaterNumber)) return false;
            DewHeaterNumber = (short)(dewHeaterNumber - 5);

            if (!ParseDouble(response.Substring(3), "dew heater duty cycle", out var dutyCycle)) return false;
            DutyCycle = dutyCycle / 255d * 100d;
            return true;
        }
    }

    public class PowerConsumptionResponse : PegasusUpbv2Response {
        private double _averagePower;
        private double _ampereHours;
        private double _wattHours;
        public override int Ttl => 100;

        public double AveragePower => _averagePower;

        public double AmpereHours => _ampereHours;

        public double WattHours => _wattHours;

        public TimeSpan UpTime { get; protected set; }

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response)) {
                Logger.Error($"Could not parse PowerConsumption Response {response}");
                return false;
            }

            var tokens = response.Split(':');
            if (tokens.Length < 4) {
                Logger.Error($"Not enough tokens in PowerConsumption Response {response}. Should have been 4, was: {tokens.Length}");
                return false;
            }

            if (!ParseDouble(tokens[0], "average power", out _averagePower)) return false;
            if (!ParseDouble(tokens[1], "ampere hours", out _ampereHours)) return false;
            if (!ParseDouble(tokens[2], "watt hours", out _wattHours)) return false;
            if (!ParseLong(tokens[3], "up time", out var upTime)) return false;

            UpTime = TimeSpan.FromMilliseconds(upTime);
            return true;
        }
    }

    public class SetAutoDewResponse : PegasusUpbv2Response {
        public ReadOnlyCollection<bool> AutoDewStatus;

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || !response.StartsWith("PD:") || response.Length < 4) {
                Logger.Error($"Could not parse SetAutoDew Response {response}");
                return false;
            }
            if (!ParseShort(response.Substring(3), "auto-dew status", out var autoDewStatus)) return false;
            AutoDewStatus = ParseAutoDewStatus(autoDewStatus);
            return true;
        }
    }

    public class StepperMotorTemperatureResponse : PegasusUpbv2Response {
        private double _temperature;

        public double Temperature => _temperature;

        protected override bool ParseResponse(string response) {
            return ParseDouble(response, "stepper motor temperature", out _temperature);
        }
    }

    public class StepperMotorGetCurrentPositionResponse : PegasusUpbv2Response {
        private int _position;

        public int Position => _position;

        protected override bool ParseResponse(string response) {
            return ParseInteger(response, "", out _position);
        }
    }

    public class StepperMotorMoveToPositionResponse : PegasusUpbv2Response {
        private int _position;

        public int Position => _position;

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || !response.StartsWith("SM:") || response.Length < 4) {
                Logger.Error($"Could not parse StepperMotorMoveToPosition Response {response}");
                return false;
            }

            return ParseInteger(response.Substring(3), "position", out _position);
        }
    }

    public class StepperMotorIsMovingResponse : PegasusUpbv2Response {
        private bool _isMoving;

        public bool IsMoving => _isMoving;

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response)) {
                Logger.Error($"Could not parse StepperMotorIsMoving Response {response}");
                return false;
            }
            return ParseBoolFromZeroOne(response[0], "moving value", out _isMoving);
        }
    }

    public class StepperMotorHaltResponse : PegasusUpbv2Response {

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response)) {
                Logger.Error($"Could not parse StepperMotorHalt Response {response}");
                return false;
            }

            return response.Equals("SH");
        }
    }

    public class StepperMotorDirectionResponse : PegasusUpbv2Response {
        public bool DirectionClockwise { get; protected set; }

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || !response.StartsWith("SR:") || response.Length < 4) {
                Logger.Error($"Could not parse StepperDirection Response {response}");
                return false;
            }
            if (!ParseBoolFromZeroOne(response[3], "motor direction", out var zero)) return false;
            DirectionClockwise = !zero;
            return true;
        }
    }

    public class StepperMotorSetCurrentPositionResponse : PegasusUpbv2Response {
        private int _position;

        public int Position => _position;

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || !response.StartsWith("SC:") || response.Length < 4) {
                Logger.Error($"Could not parse StepperMotorSetCurrentPosition Response {response}");
                return false;
            }
            return ParseInteger(response.Substring(3), "position", out _position);
        }
    }

    public class StepperMotorSetMaximumSpeedResponse : PegasusUpbv2Response {

        protected override bool ParseResponse(string response) {
            return true;
        }
    }

    public class StepperMotorSetBacklashStepsResponse : PegasusUpbv2Response {
        private int _steps;

        public int Steps => _steps;

        protected override bool ParseResponse(string response) {
            if (string.IsNullOrEmpty(response) || !response.StartsWith("SB:") || response.Length < 4) {
                Logger.Error($"Could not parse StepperMotorSetBacklashSteps Response {response}");
                return false;
            }
            return ParseInteger(response.Substring(3), "backlash steps", out _steps);
        }
    }
}