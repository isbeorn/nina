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

using NINA.Locale;
using NINA.Utility;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using System;
using System.Threading.Tasks;
using NINA.Utility.SerialCommunication;

namespace NINA.Model.MySwitch.PegasusAstro {

    public abstract class ContinuousSwitch : PegasusAstroSwitch {

        public abstract override Task<bool> Poll();

        public override double Maximum { get; protected set; }
        public override double Minimum { get; protected set; }
        public override double StepSize { get; protected set; }
        public override double TargetValue { get; set; }

        public abstract override Task SetValue();
    }

    public sealed class VariablePowerSwitch : ContinuousSwitch {

        public VariablePowerSwitch() {
            Maximum = 12d;
            Minimum = 3d;
            StepSize = 1d;
            Description = "3V - 12V";
        }

        public override async Task<bool> Poll() {
            return await Task.Run(async () => {
                var command = new PowerStatusCommand();
                try {
                    var response = await Sdk.SendCommand<PowerStatusResponse>(command);
                    Value = response.VariableVoltage;
                    return true;
                } catch (InvalidDeviceResponseException ex) {
                    Logger.Error($"Invalid response from Ultimate Powerbox V2. " +
                                 $"Command was: {command} Response was: {ex.Message}.");
                    return false;
                } catch (SerialPortClosedException ex) {
                    Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
                    return false;
                }
            });
        }

        public override Task SetValue() {
            return Task.Run(async () => {
                var command = new SetVariableVoltageCommand { VariableVoltage = TargetValue };
                try {
                    Logger.Trace($"Trying to set value {TargetValue} for variable power switch");
                    _ = await Sdk.SendCommand<SetVariableVoltageResponse>(command);
                } catch (InvalidDeviceResponseException ex) {
                    Logger.Error($"Invalid response from Ultimate Powerbox V2. " +
                                 $"Command was: {command} Response was: {ex.Message}.");
                } catch (SerialPortClosedException ex) {
                    Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
                }
            });
        }
    }

    public sealed class DewHeater : ContinuousSwitch {
        private double _currentAmps;

        public double CurrentAmps {
            get => _currentAmps;
            set {
                if (Math.Abs(_currentAmps - value) < Tolerance) return;
                _currentAmps = value;
                RaisePropertyChanged();
            }
        }

        private bool _excessCurrent;

        public bool ExcessCurrent {
            get => _excessCurrent;
            set {
                if (_excessCurrent == value) return;
                _excessCurrent = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoDewOn;

        public bool AutoDewOn {
            get => _autoDewOn;
            set {
                if (_autoDewOn == value) return;
                _autoDewOn = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoDewOnTarget;

        public bool AutoDewOnTarget {
            get => _autoDewOnTarget;
            set {
                if (_autoDewOnTarget == value) return;
                _autoDewOnTarget = value;
                RaisePropertyChanged();
            }
        }

        public DewHeater(short dewHeaterNumber) {
            Id = dewHeaterNumber;
            Name = $"{Loc.Instance["LblUPBV2DewHeater"]} {dewHeaterNumber + 1}";
            Description = "";
            Maximum = 100d;
            Minimum = 0d;
            StepSize = 1d;
        }

        public override async Task<bool> Poll() {
            return await Task.Run(async () => {
                var command = new StatusCommand();
                try {
                    var response = await Sdk.SendCommand<StatusResponse>(command);
                    Value = Math.Round(response.DewHeaterDutyCycle[Id] / 255d * 100d);
                    CurrentAmps = response.DewHeaterPowerFlow[Id];
                    ExcessCurrent = response.DewHeaterOverCurrent[Id];
                    AutoDewOn = response.AutoDewStatus[Id];
                    return true;
                } catch (InvalidDeviceResponseException ex) {
                    Logger.Error($"Invalid response from Ultimate Powerbox V2. " +
                                 $"Command was: {command} Response was: {ex.Message}.");
                    return false;
                } catch (SerialPortClosedException ex) {
                    Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
                    return false;
                }
            });
        }

        public override Task SetValue() {
            return Task.Run(async () => {
                ICommand command = new SetDewHeaterPowerCommand { DutyCycle = TargetValue, SwitchNumber = Id };
                try {
                    Logger.Trace($"Trying to set value {TargetValue}, {AutoDewOn} for dew heater {Id}");
                    _ = await Sdk.SendCommand<SetDewHeaterPowerResponse>(command);
                    command = new StatusCommand();
                    var response = await Sdk.SendCommand<StatusResponse>(command);
                    command = new SetAutoDewCommand(response.AutoDewStatus, Id, AutoDewOnTarget);
                    _ = await Sdk.SendCommand<SetAutoDewResponse>(command);
                } catch (InvalidDeviceResponseException ex) {
                    Logger.Error($"Invalid response from Ultimate Powerbox V2. " +
                                 $"Command was: {command} Response was: {ex.Message}.");
                } catch (SerialPortClosedException ex) {
                    Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
                }
            });
        }
    }
}