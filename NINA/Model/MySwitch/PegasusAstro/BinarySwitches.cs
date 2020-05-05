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

using NINA.Utility;
using NINA.Utility.SerialCommunication;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using System;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch.PegasusAstro {

    public abstract class BinarySwitch : PegasusAstroSwitch {

        public abstract override Task<bool> Poll();

        public override double Maximum { get; protected set; } = 1d;
        public override double Minimum { get; protected set; } = 0d;
        public override double StepSize { get; protected set; } = 0d;
        public override double TargetValue { get; set; }

        public abstract override Task SetValue();
    }

    public sealed class PegasusAstroPowerSwitch : BinarySwitch {
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

        public PegasusAstroPowerSwitch(short switchNumber) {
            Id = switchNumber;
        }

        public override async Task<bool> Poll() {
            return await Task.Run(async () => {
                var command = new StatusCommand();
                try {
                    var response = await Sdk.SendCommand<StatusResponse>(command);
                    Value = response.PowerPortOn[Id] ? 1d : 0d;
                    CurrentAmps = response.PortPowerFlow[Id];
                    ExcessCurrent = response.PortOverCurrent[Id];
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
                var command = new SetPowerCommand { SwitchNumber = (short)(Id + 1), On = Math.Abs(TargetValue - 1d) < Tolerance };
                try {
                    Logger.Trace($"Trying to set value {TargetValue}, for Power port {Id}");
                    await Sdk.SendCommand<SetPowerResponse>(command);
                } catch (InvalidDeviceResponseException ex) {
                    Logger.Error($"Invalid response from Ultimate Powerbox V2. " +
                                 $"Command was: {command} Response was: {ex.Message}.");
                } catch (SerialPortClosedException ex) {
                    Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
                }
            });
        }
    }

    public sealed class PegasusAstroUsbSwitch : BinarySwitch {

        public PegasusAstroUsbSwitch(short switchNumber) {
            Id = switchNumber;
        }

        public override async Task<bool> Poll() {
            return await Task.Run(async () => {
                var command = new StatusCommand();
                try {
                    var response = await Sdk.SendCommand<StatusResponse>(command);
                    Value = response.UsbPortOn[Id] ? 1d : 0d;
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
                var command = new SetUsbPowerCommand { SwitchNumber = (short)(Id + 1), On = Math.Abs(TargetValue - 1d) < Tolerance };
                try {
                    Logger.Trace($"Trying to set value {TargetValue}, for Power port {Id}");
                    _ = await Sdk.SendCommand<SetUsbPowerResponse>(command);
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