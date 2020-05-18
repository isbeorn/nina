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

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!TryParseDouble(response, "firmware version", out _firmwareVersion)) throw new InvalidDeviceResponseException(response);
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

        protected override void ParseResponse(string value) {
            base.ParseResponse(value);

            var tokens = value.Split(':');
            if (tokens.Length != 21) {
                Logger.Error($"Wrong number of tokens. Should have been 21, was {tokens.Length}");
                throw new InvalidDeviceResponseException(value);
            }

            DeviceName = tokens[0];
            if (!TryParseDouble(tokens[1], "device input voltage", out _deviceInputVoltage)) throw new InvalidDeviceResponseException(value);
            if (!TryParseDouble(tokens[2], "device current ampere", out _deviceCurrentAmpere)) throw new InvalidDeviceResponseException(value);
            if (!TryParseInteger(tokens[3], "device power", out _devicePower)) throw new InvalidDeviceResponseException(value);
            if (!TryParseDouble(tokens[4], "temperature", out _temperature)) throw new InvalidDeviceResponseException(value);
            if (!TryParseDouble(tokens[5], "humidity", out _humidity)) throw new InvalidDeviceResponseException(value);
            if (!TryParseDouble(tokens[6], "dew point", out _dewPoint)) throw new InvalidDeviceResponseException(value);

            var tempBool = new bool[4];
            for (var i = 0; i < 4; i++) {
                if (!TryParseBoolFromZeroOne(tokens[7][i], "power port status", out tempBool[i])) throw new InvalidDeviceResponseException(value);
            }
            PowerPortOn = new ReadOnlyCollection<bool>(tempBool);

            tempBool = new bool[6];
            for (var i = 0; i < 6; i++) {
                if (!TryParseBoolFromZeroOne(tokens[8][i], "usb port status", out tempBool[i])) throw new InvalidDeviceResponseException(value);
            }
            UsbPortOn = new ReadOnlyCollection<bool>(tempBool);

            var tempShort = new short[3];
            for (var i = 0; i < 3; i++) {
                if (!TryParseShort(tokens[i + 9], "dew heater cycle", out tempShort[i])) throw new InvalidDeviceResponseException(value);
            }
            DewHeaterDutyCycle = new ReadOnlyCollection<short>(tempShort);

            var tempDouble = new double[4];
            for (var i = 0; i < 4; i++) {
                if (!TryParseDouble(tokens[i + 12], "power port current", out tempDouble[i])) throw new InvalidDeviceResponseException(value);
                tempDouble[i] /= 300d;
            }
            PortPowerFlow = new ReadOnlyCollection<double>(tempDouble);

            tempDouble = new double[3];
            for (var i = 0; i < 3; i++) {
                if (!TryParseDouble(tokens[i + 16], "dew heater port current", out tempDouble[i])) throw new InvalidDeviceResponseException(value);
                tempDouble[i] /= 300d;
            }
            tempDouble[2] /= 2d;// different MOSFET, needs to be divided by 600

            DewHeaterPowerFlow = new ReadOnlyCollection<double>(tempDouble);

            tempBool = new bool[4];
            for (var i = 0; i < 4; i++) {
                if (!TryParseBoolFromZeroOne(tokens[19][i], "port over current status", out tempBool[i])) throw new InvalidDeviceResponseException(value);
            }
            PortOverCurrent = new ReadOnlyCollection<bool>(tempBool);

            tempBool = new bool[3];
            for (var i = 0; i < 3; i++) {
                if (!TryParseBoolFromZeroOne(tokens[19][i + 4], "dew heater over current status", out tempBool[i])) throw new InvalidDeviceResponseException(value);
            }
            DewHeaterOverCurrent = new ReadOnlyCollection<bool>(tempBool);

            if (!TryParseShort(tokens[20], "auto-dew status", out var autoDewStatus)) throw new InvalidDeviceResponseException(value);
            AutoDewStatus = ParseAutoDewStatus(autoDewStatus);
        }
    }

    public class SetPowerResponse : PegasusUpbv2Response {
        private bool _on;

        public bool On => _on;

        public short SwitchNumber { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response.Length < 4 || response[0] != 'P' || response[2] != ':') {
                Logger.Error($"Could not parse SetPower Response {response}");
                throw new InvalidDeviceResponseException(response);
            }
            if (!TryParseShort(response[1].ToString(), "switch number", out var switchNumber)) throw new InvalidDeviceResponseException(response);
            SwitchNumber = (short)(switchNumber - 1);
            if (!TryParseBoolFromZeroOne(response[3], "switch status", out _on)) throw new InvalidDeviceResponseException(response);
        }
    }

    public class SetUsbPowerResponse : PegasusUpbv2Response {
        private bool _on;

        public bool On => _on;

        public short SwitchNumber { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (response.Length < 4 || response[0] != 'U' || response[2] != ':') {
                Logger.Error($"Could not parse SetUsbPower Response {response}");
                throw new InvalidDeviceResponseException(response);
            }
            if (!TryParseShort(response[1].ToString(), "switch number", out var switchNumber)) throw new InvalidDeviceResponseException(response);
            SwitchNumber = (short)(switchNumber - 1);
            if (!TryParseBoolFromZeroOne(response[3], "switch status", out _on)) throw new InvalidDeviceResponseException(response);
        }
    }

    public class PowerStatusResponse : PegasusUpbv2Response {
        public double VariableVoltage { get; protected set; }
        public ReadOnlyCollection<bool> PowerStatusOnBoot { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.StartsWith("PS:") || response.Length < 9) {
                Logger.Error($"Could not parse PowerStatus Response {response}");
                throw new InvalidDeviceResponseException(response);
            }
            var temp = new bool[4];
            for (var i = 0; i < 4; i++) {
                if (!TryParseBoolFromZeroOne(response[i + 3], "power status", out temp[i])) throw new InvalidDeviceResponseException(response);
            }
            PowerStatusOnBoot = new ReadOnlyCollection<bool>(temp);

            if (!TryParseDouble(response.Substring(8), "variable voltage value", out var voltage)) throw new InvalidDeviceResponseException(response);
            VariableVoltage = voltage <= 12d ? voltage : 0;
        }
    }

    public class SetVariableVoltageResponse : PegasusUpbv2Response {
        public double VariableVoltage { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (string.IsNullOrEmpty(response) || !response.StartsWith("P8:") || response.Length < 4) {
                Logger.Error($"Could not parse SetVariableVoltage Response {response}");
                throw new InvalidDeviceResponseException(response);
            }

            if (!TryParseDouble(response.Substring(3), "variable voltage value", out var voltage)) throw new InvalidDeviceResponseException(response);
            VariableVoltage = voltage <= 12d ? voltage : 0;
        }
    }

    public class SetDewHeaterPowerResponse : PegasusUpbv2Response {
        public short DewHeaterNumber { get; protected set; }

        public double DutyCycle { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.StartsWith("P") || response.Length < 4) {
                Logger.Error($"Could not parse SetDewHeaterPower Response {response}");
                throw new InvalidDeviceResponseException(response);
            }

            if (!TryParseShort(response[1].ToString(), "dew heater number", out var dewHeaterNumber)) throw new InvalidDeviceResponseException(response);
            DewHeaterNumber = (short)(dewHeaterNumber - 5);

            if (!TryParseDouble(response.Substring(3), "dew heater duty cycle", out var dutyCycle)) throw new InvalidDeviceResponseException(response);
            DutyCycle = dutyCycle / 255d * 100d;
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

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);

            var tokens = response.Split(':');
            if (tokens.Length < 4) {
                Logger.Error($"Not enough tokens in PowerConsumption Response {response}. Should have been 4, was: {tokens.Length}");
                throw new InvalidDeviceResponseException(response);
            }

            if (!TryParseDouble(tokens[0], "average power", out _averagePower)) throw new InvalidDeviceResponseException(response);
            if (!TryParseDouble(tokens[1], "ampere hours", out _ampereHours)) throw new InvalidDeviceResponseException(response);
            if (!TryParseDouble(tokens[2], "watt hours", out _wattHours)) throw new InvalidDeviceResponseException(response);
            if (!TryParseLong(tokens[3], "up time", out var upTime)) throw new InvalidDeviceResponseException(response);

            UpTime = TimeSpan.FromMilliseconds(upTime);
        }
    }

    public class SetAutoDewResponse : PegasusUpbv2Response {
        public ReadOnlyCollection<bool> AutoDewStatus;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.StartsWith("PD:") || response.Length < 4) {
                Logger.Error($"Could not parse SetAutoDew Response {response}");
                throw new InvalidDeviceResponseException(response);
            }
            if (!TryParseShort(response.Substring(3), "auto-dew status", out var autoDewStatus)) throw new InvalidDeviceResponseException(response);
            AutoDewStatus = ParseAutoDewStatus(autoDewStatus);
        }
    }

    public class StepperMotorTemperatureResponse : PegasusUpbv2Response {
        private double _temperature;

        public double Temperature => _temperature;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!TryParseDouble(response, "stepper motor temperature", out _temperature)) throw new InvalidDeviceResponseException(response);
        }
    }

    public class StepperMotorGetCurrentPositionResponse : PegasusUpbv2Response {
        private int _position;

        public int Position => _position;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!TryParseInteger(response, "", out _position)) throw new InvalidDeviceResponseException(response);
        }
    }

    public class StepperMotorMoveToPositionResponse : PegasusUpbv2Response {
        private int _position;

        public int Position => _position;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.StartsWith("SM:") || response.Length < 4) {
                Logger.Error($"Could not parse StepperMotorMoveToPosition Response {response}");
                throw new InvalidDeviceResponseException(response);
            }

            if (!TryParseInteger(response.Substring(3), "position", out _position)) throw new InvalidDeviceResponseException(response);
        }
    }

    public class StepperMotorIsMovingResponse : PegasusUpbv2Response {
        private bool _isMoving;

        public bool IsMoving => _isMoving;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!TryParseBoolFromZeroOne(response[0], "moving value", out _isMoving)) throw new InvalidDeviceResponseException(response);
        }
    }

    public class StepperMotorHaltResponse : PegasusUpbv2Response {

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.Equals("SH")) throw new InvalidDeviceResponseException(response);
        }
    }

    public class StepperMotorDirectionResponse : PegasusUpbv2Response {
        public bool DirectionClockwise { get; protected set; }

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.StartsWith("SR:") || response.Length < 4) {
                Logger.Error($"Could not parse StepperDirection Response {response}");
                throw new InvalidDeviceResponseException(response);
            }
            if (!TryParseBoolFromZeroOne(response[3], "motor direction", out var zero)) throw new InvalidDeviceResponseException(response);
            DirectionClockwise = !zero;
        }
    }

    public class StepperMotorSetCurrentPositionResponse : PegasusUpbv2Response {
        private int _position;

        public int Position => _position;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.StartsWith("SC:") || response.Length < 4) {
                Logger.Error($"Could not parse StepperMotorSetCurrentPosition Response {response}");
                throw new InvalidDeviceResponseException(response);
            }
            if (!TryParseInteger(response.Substring(3), "position", out _position)) throw new InvalidDeviceResponseException(response);
        }
    }

    public class StepperMotorSetMaximumSpeedResponse : PegasusUpbv2Response {

        protected override void ParseResponse(string response) {
            return;
        }
    }

    public class StepperMotorSetBacklashStepsResponse : PegasusUpbv2Response {
        private int _steps;

        public int Steps => _steps;

        protected override void ParseResponse(string response) {
            base.ParseResponse(response);
            if (!response.StartsWith("SB:") || response.Length < 4) {
                Logger.Error($"Could not parse StepperMotorSetBacklashSteps Response {response}");
                throw new InvalidDeviceResponseException(response);
            }
            if (!TryParseInteger(response.Substring(3), "backlash steps", out _steps)) throw new InvalidDeviceResponseException(response);
        }
    }
}
