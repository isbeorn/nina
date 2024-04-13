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
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyDome {

    public static class ShutterStateExtensions {

        public static ShutterState FromASCOM(this ASCOM.Common.DeviceInterfaces.ShutterState shutterState) {
            switch (shutterState) {
                case ASCOM.Common.DeviceInterfaces.ShutterState.Open:
                    return ShutterState.ShutterOpen;

                case ASCOM.Common.DeviceInterfaces.ShutterState.Closed:
                    return ShutterState.ShutterClosed;

                case ASCOM.Common.DeviceInterfaces.ShutterState.Opening:
                    return ShutterState.ShutterOpening;

                case ASCOM.Common.DeviceInterfaces.ShutterState.Closing:
                    return ShutterState.ShutterClosing;

                case ASCOM.Common.DeviceInterfaces.ShutterState.Error:
                    return ShutterState.ShutterError;
            }
            Logger.Error($"Invalid ASCOM shutter state {shutterState}. The driver is non-conformant and should be fixed. Treating it as shutterError.");
            return ShutterState.ShutterError;
        }

        public static ASCOM.Common.DeviceInterfaces.ShutterState ToASCOM(this ShutterState shutterState) {
            switch (shutterState) {
                case ShutterState.ShutterOpen:
                    return ASCOM.Common.DeviceInterfaces.ShutterState.Open;

                case ShutterState.ShutterClosed:
                    return ASCOM.Common.DeviceInterfaces.ShutterState.Closed;

                case ShutterState.ShutterOpening:
                    return ASCOM.Common.DeviceInterfaces.ShutterState.Opening;

                case ShutterState.ShutterClosing:
                    return ASCOM.Common.DeviceInterfaces.ShutterState.Closing;

                case ShutterState.ShutterError:
                    return ASCOM.Common.DeviceInterfaces.ShutterState.Error;

                case ShutterState.ShutterNone:
                    return ASCOM.Common.DeviceInterfaces.ShutterState.Error;
            }
            throw new ArgumentOutOfRangeException($"{shutterState} is not an expected value");
        }

        public static bool CanOpen(this ShutterState shutterState) {
            return !(shutterState == ShutterState.ShutterOpen || shutterState == ShutterState.ShutterOpening);
        }

        public static bool CanClose(this ShutterState shutterState) {
            return !(shutterState == ShutterState.ShutterClosed || shutterState == ShutterState.ShutterClosing);
        }
    }

    internal class AscomDome : AscomDevice<ASCOM.Common.DeviceInterfaces.IDomeV2>, IDome, IDisposable {

        public AscomDome(string domeId, string domeName) : base(domeId, domeName) {
        }
        public AscomDome(AscomDevice deviceMeta) : base(deviceMeta) {
        }

        public bool DriverCanFollow => GetProperty(nameof(Dome.CanSlave), false);

        public bool CanSetShutter => GetProperty(nameof(Dome.CanSetShutter), false);

        public bool CanSetPark => GetProperty(nameof(Dome.CanSetPark), false);

        public bool CanSetAzimuth => GetProperty(nameof(Dome.CanSetAzimuth), false);

        public bool CanPark => GetProperty(nameof(Dome.CanPark), false);

        public bool CanFindHome => GetProperty(nameof(Dome.CanFindHome), false);

        public double Azimuth => GetProperty(nameof(Dome.Azimuth), double.NaN);

        public bool AtPark => GetProperty(nameof(Dome.AtPark), false);

        public bool AtHome => GetProperty(nameof(Dome.AtHome), false);

        public bool DriverFollowing {
            get => GetProperty(nameof(Dome.Slaved), false);
            set => SetProperty(nameof(Dome.Slaved), value);
        }

        public bool Slewing => GetProperty(nameof(Dome.Slewing), false);

        public ShutterState ShutterStatus {
            get {
                if (!CanSetShutter) {
                    return ShutterState.ShutterNone;
                }
                var ascomState = GetProperty(nameof(Dome.ShutterStatus), ASCOM.Common.DeviceInterfaces.ShutterState.Error);
                return ascomState.FromASCOM();
            }
        }

        public bool CanSyncAzimuth => Connected && device.CanSyncAzimuth;

        protected override string ConnectionLostMessage => Loc.Instance["LblDomeConnectionLost"];

        private void Init() {
        }

        public async Task SlewToAzimuth(double azimuth, CancellationToken ct) {
            if (Connected) {
                if (CanSetAzimuth) {
                    using (ct.Register(async () => await StopSlewing())) {
                        await (device?.SlewToAzimuthAsync(azimuth, ct) ?? Task.CompletedTask);
                        InvalidatePropertyCache();
                    }
                } else {
                    Logger.Warning("Dome cannot slew");
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSlew"]);
                }
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public Task StopSlewing() {
            if (Connected) {
                // ASCOM only allows you to stop all movement, which includes both shutter and slewing. If the shutter was opening or closing
                // when this command is received, try and continue the operation afterwards
                return Task.Run(async () => {
                    var priorShutterStatus = ShutterStatus;
                    await (device?.AbortSlewAsync() ?? Task.CompletedTask);
                    if (priorShutterStatus == ShutterState.ShutterClosing) {
                        await (device?.CloseShutterAsync() ?? Task.CompletedTask);
                    } else if (priorShutterStatus == ShutterState.ShutterOpening) {
                        await (device?.OpenShutterAsync() ?? Task.CompletedTask);
                    }
                    InvalidatePropertyCache();
                });
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
            return Task.CompletedTask;
        }

        public Task StopShutter() {
            // ASCOM only allows you to stop both slew and shutter movement together. We also don't have a way of determining whether a
            // slew is in progress or what the target azimuth is, so we can't recover for a StopShutter operation
            return StopAll();
        }

        public Task StopAll() {
            if (Connected) {
                return Task.Run(() => device?.AbortSlew());
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
            return Task.CompletedTask;
        }

        public async Task OpenShutter(CancellationToken ct) {
            if (Connected) {
                if (CanSetShutter) {
                    if (ShutterStatus == ShutterState.ShutterOpen) {
                        return;
                    }

                    using (ct.Register(() => device?.AbortSlew())) {
                        if (ShutterStatus == ShutterState.ShutterError) {
                            // If shutter is in the error state, you must close it before re-opening
                            await CloseShutter(ct);
                            InvalidatePropertyCache();
                        }

                        if (ShutterStatus == ShutterState.ShutterOpening) {
                            Logger.Info($"Dome shutter already opening, so not sending another OpenShutter request");
                        } else {
                            Logger.Info($"Sending an OpenShutter request, since it is currently {ShutterStatus}");
                            await (device?.OpenShutterAsync(ct) ?? Task.CompletedTask);
                            InvalidatePropertyCache();
                            ct.ThrowIfCancellationRequested();
                        }

                        // Give the dome controller 3 seconds to react to the shutter open request, since OpenShutter can be an asynchronous operation
                        await Task.Delay(TimeSpan.FromSeconds(3), ct);
                        while (device != null && ShutterStatus == ShutterState.ShutterOpening && !ct.IsCancellationRequested) {
                            await Task.Delay(TimeSpan.FromSeconds(1), ct);
                        };
                        ct.ThrowIfCancellationRequested();

                        if (device != null && ShutterStatus == ShutterState.ShutterClosed) {
                            Logger.Error("ShutterStatus is still reported as closed after calling OpenShutter.");
                            Notification.ShowWarning(Loc.Instance["LblDomeShutterDidNotRespond"]);
                        }
                    }
                } else {
                    Logger.Warning("Dome cannot open");
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSetShutter"]);
                }
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task CloseShutter(CancellationToken ct) {
            if (Connected) {
                if (CanSetShutter) {
                    if (ShutterStatus == ShutterState.ShutterClosed) {
                        return;
                    }

                    using (ct.Register(() => device?.AbortSlew())) {
                        if (ShutterStatus == ShutterState.ShutterClosing) {
                            Logger.Info($"Dome shutter already closing, so not sending another CloseShutter request");
                        } else {
                            Logger.Info($"Sending a CloseShutter request, since it is currently {ShutterStatus}");
                            await (device?.CloseShutterAsync(ct) ?? Task.CompletedTask);
                            InvalidatePropertyCache();
                            ct.ThrowIfCancellationRequested();
                        }

                        // Give the dome controller 3 seconds to react to the shutter close request, since CloseShutter can be an asynchronous operation
                        await Task.Delay(TimeSpan.FromSeconds(3), ct);
                        while (device != null && ShutterStatus == ShutterState.ShutterClosing && !ct.IsCancellationRequested) {
                            await Task.Delay(TimeSpan.FromSeconds(1), ct);
                        };
                        ct.ThrowIfCancellationRequested();

                        if (device != null && ShutterStatus == ShutterState.ShutterOpen) {
                            Logger.Error("ShutterStatus is still reported as open after calling CloseShutter.");
                            Notification.ShowWarning(Loc.Instance["LblDomeShutterDidNotRespond"]);
                        }
                    }
                } else {
                    Logger.Warning("Dome cannot close shutter");
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSetShutter"]);
                }
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task FindHome(CancellationToken ct) {
            if (Connected) {
                if (CanFindHome) {
                    if (AtHome == true) {
                        Logger.Info("Dome already AtHome. Not submitting a FindHome request");
                        return;
                    }

                    // ASCOM domes make no promise that a slew operation can take place if one is already in progress, so we do a hard abort up front to ensure FindHome works
                    if (Slewing == true) {
                        await (device?.AbortSlewAsync(ct) ?? Task.CompletedTask);
                        InvalidatePropertyCache();
                        await Task.Delay(1000, ct);
                    }

                    using (ct.Register(() => device?.AbortSlew())) {
                        await (device?.FindHomeAsync(ct) ?? Task.CompletedTask);
                        InvalidatePropertyCache();
                        ct.ThrowIfCancellationRequested();

                        // Introduce an initial delay to give the dome a change to start slewing before we wait for it to complete
                        await Task.Delay(TimeSpan.FromSeconds(3), ct);
                        ct.ThrowIfCancellationRequested();

                        while (Slewing && !ct.IsCancellationRequested) {
                            await Task.Delay(TimeSpan.FromSeconds(1), ct);
                        }
                        ct.ThrowIfCancellationRequested();
                        // Introduce a final delay, in case the Dome driver settles after finding the home position by backtracking
                        await Task.Delay(TimeSpan.FromSeconds(2), ct);
                        ct.ThrowIfCancellationRequested();
                    }
                } else {
                    Logger.Warning("Dome cannot find home");
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotFindHome"]);
                }
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public async Task Park(CancellationToken ct) {
            if (Connected) {
                if (CanPark) {
                    // ASCOM domes make no promise that a slew operation can take place if one is already in progress, so we do a hard abort up front to ensure Park works
                    if (Slewing == true) {
                        Logger.Info("Dome shutter or rotator slewing when a park was requested. Aborting all movement");

                        await device?.AbortSlewAsync(ct);
                        InvalidatePropertyCache();
                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    }

                    ct.ThrowIfCancellationRequested();
                    using (ct.Register(() => device?.AbortSlew())) {
                        if (AtPark) {
                            Logger.Info("Dome already AtPark. Not sending a Park command");
                        } else {
                            await (device?.ParkAsync(ct) ?? Task.CompletedTask);
                            InvalidatePropertyCache();
                        }

                        if (CanSetShutter) {
                            if (ShutterStatus == ShutterState.ShutterClosed || ShutterStatus == ShutterState.ShutterClosing) {
                                Logger.Info($"Not closing dome shutter, since it is already {ShutterStatus}");
                            } else {
                                Logger.Info($"Closing shutter, since it is currently {ShutterStatus}");
                                await Task.Run(() => device?.CloseShutter(), ct);
                                ct.ThrowIfCancellationRequested();
                            }
                        }
                        await Task.Delay(TimeSpan.FromSeconds(3), ct);
                        while (Slewing && !ct.IsCancellationRequested) {
                            await Task.Delay(TimeSpan.FromSeconds(1), ct);
                        }
                        ct.ThrowIfCancellationRequested();
                    }
                } else {
                    Logger.Warning("Dome cannot find park");
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotPark"]);
                }
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void SetPark() {
            if (Connected) {
                if (CanSetPark) {
                    device.SetPark();
                } else {
                    Logger.Warning("Dome cannot set park");
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSetPark"]);
                }
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        public void SyncToAzimuth(double azimuth) {
            if (Connected) {
                if (CanSyncAzimuth) {
                    device.SyncToAzimuth(azimuth);
                } else {
                    Logger.Warning("Dome cannot sync azimuth");
                    Notification.ShowWarning(Loc.Instance["LblDomeCannotSyncAzimuth"]);
                }
            } else {
                Logger.Warning("Dome is not connected");
                Notification.ShowWarning(Loc.Instance["LblDomeNotConnected"]);
            }
        }

        protected override Task PostConnect() {
            Init();
            return Task.CompletedTask;
        }

        protected override ASCOM.Common.DeviceInterfaces.IDomeV2 GetInstance() {
            if (deviceMeta == null) {
                return new Dome(Id);
            } else {
                return new ASCOM.Alpaca.Clients.AlpacaDome(deviceMeta.ServiceType, deviceMeta.IpAddress, deviceMeta.IpPort, deviceMeta.AlpacaDeviceNumber, false, null);
            }
        }
    }
}