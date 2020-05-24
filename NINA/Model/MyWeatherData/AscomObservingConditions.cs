#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyWeatherData {

    internal class AscomObservingConditions : BaseINPC, IWeatherData, IDisposable {
        private const string _category = "ASCOM";

        public AscomObservingConditions(string weatherDataId, string weatherDataName) {
            Id = weatherDataId;
            Name = weatherDataName;
        }

        private ObservingConditions _obscond;

        private string _id;

        public string Id {
            get => _id;
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name {
            get => _name;
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public string Category { get => _category; }

        private bool _hasAveragePeriod;

        public double AveragePeriod {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasAveragePeriod) {
                        wxstat = _obscond.AveragePeriod;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasAveragePeriod = false;
                }
                return wxstat;
            }
            set {
                try {
                    if (Connected && _hasAveragePeriod) {
                        _obscond.AveragePeriod = value;
                    }
                } catch (InvalidValueException ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        private bool _hasCloudCover;

        public double CloudCover {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasCloudCover) {
                        wxstat = _obscond.CloudCover;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasCloudCover = false;
                }
                return wxstat;
            }
        }

        private bool _hasDewPoint;

        public double DewPoint {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasDewPoint) {
                        wxstat = _obscond.DewPoint;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasDewPoint = false;
                }
                return wxstat;
            }
        }

        private bool _hasHumidity;

        public double Humidity {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasHumidity) {
                        wxstat = _obscond.Humidity;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasHumidity = false;
                }
                return wxstat;
            }
        }

        private bool _hasPressure;

        public double Pressure {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasPressure) {
                        wxstat = _obscond.Pressure;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasPressure = false;
                }
                return wxstat;
            }
        }

        private bool _hasRainRate;

        public double RainRate {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasRainRate) {
                        wxstat = _obscond.RainRate;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasRainRate = false;
                }
                return wxstat;
            }
        }

        private bool _hasSkyBrightness;

        public double SkyBrightness {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasSkyBrightness) {
                        wxstat = _obscond.SkyBrightness;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasSkyBrightness = false;
                }
                return wxstat;
            }
        }

        private bool _hasSkyQuality;

        public double SkyQuality {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasSkyQuality) {
                        wxstat = _obscond.SkyQuality;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasSkyQuality = false;
                }
                return wxstat;
            }
        }

        private bool _hasSkyTemperature;

        public double SkyTemperature {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasSkyTemperature) {
                        wxstat = _obscond.SkyTemperature;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasSkyTemperature = false;
                }
                return wxstat;
            }
        }

        private bool _hasStarFWHM;

        public double StarFWHM {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasStarFWHM) {
                        wxstat = _obscond.StarFWHM;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasStarFWHM = false;
                }
                return wxstat;
            }
        }

        private bool _hasTemperature;

        public double Temperature {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasTemperature) {
                        wxstat = _obscond.Temperature;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasTemperature = false;
                }
                return wxstat;
            }
        }

        private bool _hasWindDirection;

        public double WindDirection {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasWindDirection) {
                        wxstat = _obscond.WindDirection;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasWindDirection = false;
                }
                return wxstat;
            }
        }

        private bool _hasWindGust;

        public double WindGust {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasWindGust) {
                        wxstat = _obscond.WindGust;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasWindGust = false;
                }
                return wxstat;
            }
        }

        private bool _hasWindSpeed;

        public double WindSpeed {
            get {
                double wxstat = double.NaN;
                try {
                    if (Connected && _hasWindSpeed) {
                        wxstat = _obscond.WindSpeed;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasWindSpeed = false;
                }
                return wxstat;
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _obscond.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblWeatherConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception) {
                        Disconnect();
                    }
                    return val;
                } else {
                    return false;
                }
            }
            private set {
                try {
                    _obscond.Connected = value;
                    _connected = value;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblWeatherReconnect"] + Environment.NewLine + ex.Message);
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }

        public string Description => Connected ? _obscond?.Description ?? string.Empty : string.Empty;
        public string DriverInfo => Connected ? _obscond?.DriverInfo ?? string.Empty : string.Empty;
        public string DriverVersion => Connected ? _obscond?.DriverVersion ?? string.Empty : string.Empty;

        public bool HasSetupDialog => true;

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (_obscond == null) {
                        _obscond = new ObservingConditions(Id);
                    }
                    _obscond.SetupDialog();
                    if (dispose) {
                        _obscond.Dispose();
                        _obscond = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                try {
                    _obscond = new ObservingConditions(Id);
                    Connected = true;
                    if (Connected) {
                        Init();
                        RaiseAllPropertiesChanged();
                    }
                } catch (DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblWeatherASCOMConnectFailed"] + ex.Message);
                }
                return Connected;
            });
        }

        public void Dispose() {
            _obscond?.Dispose();
        }

        public void Disconnect() {
            Connected = false;
            _obscond?.Dispose();
            _obscond = null;
        }

        private void Init() {
            _hasAveragePeriod = true;
            _hasCloudCover = true;
            _hasDewPoint = true;
            _hasHumidity = true;
            _hasPressure = true;
            _hasRainRate = true;
            _hasSkyBrightness = true;
            _hasSkyQuality = true;
            _hasSkyTemperature = true;
            _hasStarFWHM = true;
            _hasTemperature = true;
            _hasWindDirection = true;
            _hasWindGust = true;
            _hasWindSpeed = true;
        }
    }
}