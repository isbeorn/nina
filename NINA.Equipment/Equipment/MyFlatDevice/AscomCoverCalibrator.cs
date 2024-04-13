#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Alpaca.Discovery;
using ASCOM.Com.DriverAccess;
using ASCOM.Common;
using ASCOM.Common.DeviceInterfaces;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFlatDevice {

    public class AscomCoverCalibrator : AscomDevice<ICoverCalibratorV1>, IFlatDevice, IDisposable {

        public AscomCoverCalibrator(string id, string name) : base(id, name) {
        }
        public AscomCoverCalibrator(AscomDevice deviceMeta) : base(deviceMeta) {
        }

        private int lastBrightness = 0;

        public CoverState CoverState {
            get {
                var state = device.CoverState;
                switch (state) {
                    case ASCOM.Common.DeviceInterfaces.CoverStatus.Unknown:
                        return CoverState.Unknown;

                    case ASCOM.Common.DeviceInterfaces.CoverStatus.NotPresent:
                        return CoverState.NotPresent;

                    case ASCOM.Common.DeviceInterfaces.CoverStatus.Moving:
                        return CoverState.NeitherOpenNorClosed;

                    case ASCOM.Common.DeviceInterfaces.CoverStatus.Closed:
                        return CoverState.Closed;

                    case ASCOM.Common.DeviceInterfaces.CoverStatus.Open:
                        return CoverState.Open;

                    case ASCOM.Common.DeviceInterfaces.CoverStatus.Error:
                        return CoverState.Error;

                    default:
                        return CoverState.Unknown;
                }
            }
        }

        public int MaxBrightness { get; private set; }

        public int MinBrightness { get; private set; }

        public bool LightOn {
            get => SupportsOnOff ? GetProperty<int>(nameof(Brightness), 0) > 0 : false;
            set {
                try {
                    if (SupportsOnOff) {
                        if (value) {
                            Logger.Debug("Switching cover calibrator on");
                            // switch the light on with the last saved value, if any
                            device.CalibratorOn((lastBrightness != 0) ? lastBrightness : MaxBrightness);
                        } else {
                            Logger.Debug("Switching cover calibrator off");
                            device.CalibratorOff();
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        public int Brightness {
            get => SupportsOnOff ? GetProperty<int>(nameof(Brightness), 0) : 0;
            set {
                try {
                    if (SupportsOnOff) {
                        if (value < MinBrightness) {
                            value = MinBrightness;
                        }

                        if (value > MaxBrightness) {
                            value = MaxBrightness;
                        }
                        Logger.Debug($"Setting cover calibrator brightness to {value}");
                        device.CalibratorOn(value);
                        lastBrightness = value; // save brightness for next time the user toggles the light on
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        public string PortName { get => string.Empty; set { } }

        public bool SupportsOpenClose => device.CoverState != ASCOM.Common.DeviceInterfaces.CoverStatus.NotPresent;

        public bool SupportsOnOff => device.CalibratorState != ASCOM.Common.DeviceInterfaces.CalibratorStatus.NotPresent;

        protected override string ConnectionLostMessage => Loc.Instance["LblFlatDeviceConnectionLost"];

        private void Initialize() {
            if (device.CalibratorState == ASCOM.Common.DeviceInterfaces.CalibratorStatus.NotPresent) {
                MinBrightness = 0;
                MaxBrightness = 0;
            } else {
                try {
                    MinBrightness = 0;
                    MaxBrightness = device.MaxBrightness;
                } catch (ASCOM.NotImplementedException) {
                    MinBrightness = 0;
                    MaxBrightness = 0;
                }
            }
        }

        public async Task<bool> Open(CancellationToken ct, int delay = 300) {
            if (SupportsOpenClose) {
                if (CoverState == CoverState.Error) {
                    throw new FlatDeviceCoverErrorException();
                }

                await device.OpenCoverAsync(ct);
                InvalidatePropertyCache();
                if (CoverState == CoverState.Error) {
                    throw new FlatDeviceCoverErrorException();
                }
            }
            return CoverState == CoverState.Open;
        }

        public async Task<bool> Close(CancellationToken ct, int delay = 300) {
            if (SupportsOpenClose) {
                if (CoverState == CoverState.Error) {
                    throw new FlatDeviceCoverErrorException();
                }

                await device.CloseCoverAsync(ct);
                InvalidatePropertyCache();
                if (CoverState == CoverState.Error) {
                    throw new FlatDeviceCoverErrorException();
                }
            }
            return CoverState == CoverState.Closed;
        }

        protected override Task PreConnect() {
            lastBrightness = 0;
            return base.PreConnect();
        }

        protected override Task PostConnect() {
            Initialize();
            return Task.CompletedTask;
        }

        protected override ICoverCalibratorV1 GetInstance() {
            if (deviceMeta == null) {
                return new CoverCalibrator(Id);
            } else {
                return new ASCOM.Alpaca.Clients.AlpacaCoverCalibrator(deviceMeta.ServiceType, deviceMeta.IpAddress, deviceMeta.IpPort, deviceMeta.AlpacaDeviceNumber, false, null);
            }
        }
    }
}