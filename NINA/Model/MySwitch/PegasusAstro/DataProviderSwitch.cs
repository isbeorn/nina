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
using System.Collections.Generic;
using System.Threading.Tasks;
using NINA.Utility.SerialCommunication;

namespace NINA.Model.MySwitch.PegasusAstro {

    public class DataProviderSwitch : PegasusAstroSwitch {
        private double _voltage;
        private double _current;
        private int _power;
        private double _temperature;
        private double _humidity;
        private double _dewPoint;
        private double _averagePower;
        private double _ampereHours;
        private double _wattHours;
        private string _upTime;
        private const int MAX_SIZE = 200;

        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> VoltageHistory { get; } =
            new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(MAX_SIZE);

        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> AmpereHistory { get; } =
            new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(MAX_SIZE);

        public double Voltage {
            get => _voltage;
            protected set {
                if (Math.Abs(_voltage - value) < Tolerance) return;
                _voltage = value;
                RaisePropertyChanged();
            }
        }

        public double Current {
            get => _current;
            protected set {
                if (Math.Abs(_current - value) < Tolerance) return;
                _current = value;
                RaisePropertyChanged();
            }
        }

        public int Power {
            get => _power;
            protected set {
                if (_power == value) return;
                _power = value;
                RaisePropertyChanged();
            }
        }

        public double Temperature {
            get => _temperature;
            protected set {
                if (Math.Abs(_temperature - value) < Tolerance) return;
                _temperature = value;
                RaisePropertyChanged();
            }
        }

        public double Humidity {
            get => _humidity;
            protected set {
                if (Math.Abs(_humidity - value) < Tolerance) return;
                _humidity = value;
                RaisePropertyChanged();
            }
        }

        public double DewPoint {
            get => _dewPoint;
            protected set {
                if (Math.Abs(_dewPoint - value) < Tolerance) return;
                _dewPoint = value;
                RaisePropertyChanged();
            }
        }

        public double AveragePower {
            get => _averagePower;
            protected set {
                if (Math.Abs(_averagePower - value) < Tolerance) return;
                _averagePower = value;
                RaisePropertyChanged();
            }
        }

        public double AmpereHours {
            get => _ampereHours;
            protected set {
                if (Math.Abs(_ampereHours - value) < Tolerance) return;
                _ampereHours = value;
                RaisePropertyChanged();
            }
        }

        public double WattHours {
            get => _wattHours;
            protected set {
                if (Math.Abs(_wattHours - value) < Tolerance) return;
                _wattHours = value;
                RaisePropertyChanged();
            }
        }

        public string UpTime {
            get => _upTime;
            protected set {
                if (string.Equals(_upTime, value)) return;
                _upTime = value;
                RaisePropertyChanged();
            }
        }

        public override async Task<bool> Poll() {
            return await Task.Run(async () => {
                var command = new StatusCommand();
                try {
                    var statusResponse = await Sdk.SendCommand<StatusResponse>(command);
                    VoltageHistory.Add(
                        new KeyValuePair<DateTime, double>(DateTime.Now, statusResponse.DeviceInputVoltage));
                    AmpereHistory.Add(
                        new KeyValuePair<DateTime, double>(DateTime.Now, statusResponse.DeviceCurrentAmpere));
                    RaisePropertyChanged(nameof(VoltageHistory));
                    RaisePropertyChanged(nameof(AmpereHistory));

                    Voltage = statusResponse.DeviceInputVoltage;
                    Current = statusResponse.DeviceCurrentAmpere;
                    Power = statusResponse.DevicePower;
                    Temperature = statusResponse.Temperature;
                    Humidity = statusResponse.Humidity;
                    DewPoint = statusResponse.DewPoint;

                    var powerConsumptionResponse = await Sdk.SendCommand<PowerConsumptionResponse>(new PowerConsumptionCommand());
                    AveragePower = powerConsumptionResponse.AveragePower;
                    AmpereHours = powerConsumptionResponse.AmpereHours;
                    WattHours = powerConsumptionResponse.WattHours;
                    UpTime = $"{powerConsumptionResponse.UpTime.Days} {Loc.Instance["LblDays"]}, " +
                             $"{powerConsumptionResponse.UpTime.Hours} {Loc.Instance["LblHours"]}, " +
                             $"{powerConsumptionResponse.UpTime.Minutes} {Loc.Instance["LblMinutes"]}";
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

        public override double Maximum { get; protected set; }
        public override double Minimum { get; protected set; }
        public override double StepSize { get; protected set; }
        public override double TargetValue { get; set; }

        public override Task SetValue() {
            Logger.Error("Something was trying to set the value for the data provider switch.");
            throw new NotImplementedException();
        }
    }
}