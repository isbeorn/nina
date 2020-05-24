#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using NINA.Utility.FlatDeviceSDKs.AllPro;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    internal class AllProSpikeAFlat : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;
        private volatile IntPtr _usbdHandle = (IntPtr)0;
        private object _handleLock = new object();

        public AllProSpikeAFlat(IProfileService profileService) {
            this._profileService = profileService;
        }

        public CoverState CoverState {
            get => CoverState.NeitherOpenNorClosed;
        }

        public int MaxBrightness {
            get => 1000;
        }

        public int MinBrightness {
            get => 0;
        }

        public bool LightOn {
            get {
                lock (this._handleLock) {
                    if (!Connected) return false;
                    return USBD.USBD_IsLightOn(this._usbdHandle) != 0;
                }
            }
            set {
                lock (this._handleLock) {
                    if (Connected) {
                        if (USBD.USBD_LightOn(this._usbdHandle, value) != 0) {
                            Logger.Error($"Failed to turn LightOn status to {value}");
                            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }
                    }
                }
                RaisePropertyChanged();
            }
        }

        public double Brightness {
            get {
                lock (this._handleLock) {
                    if (!Connected) return 0.0;
                    int brightness = USBD.USBD_GetBrightness(this._usbdHandle);
                    return (double)brightness / (double)MaxBrightness;
                }
            }
            set {
                lock (this._handleLock) {
                    if (Connected) {
                        if (value < 0) {
                            value = 0;
                        }
                        if (value > 1) {
                            value = 1;
                        }
                        uint brightness = (uint)Math.Round(value * MaxBrightness);
                        if (USBD.USBD_SetBrightness(this._usbdHandle, brightness) != 0) {
                            Logger.Error($"Failed to set brightness to {value}");
                            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }
                    }
                }
            }
        }

        public string PortName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool SupportsOpenClose {
            get => false;
        }

        public bool HasSetupDialog {
            get => false;
        }

        public string Id {
            get => "AFE4C45C-47DB-471B-B570-9044FF6F6374";
        }

        public string Name {
            get => $"{Locale.Loc.Instance["LblAllProSpikeAFlatPanel"]}";
        }

        public string Category {
            get => "AllPro Software";
        }

        private bool _connected;

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        private string _description;

        public string Description {
            get => _description;
            set {
                _description = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo {
            get => "AllPro-provided USB driver";
        }

        public string DriverVersion {
            get => "Last modified 2015/01/13";
        }

        public async Task<bool> Connect(CancellationToken ct) {
            return await Task.Run(() => {
                lock (this._handleLock) {
                    if (this._usbdHandle == (IntPtr)0) {
                        this._usbdHandle = USBD.USBD_Open();
                    }
                    if (this._usbdHandle == (IntPtr)0) {
                        Logger.Error("Unable to open AllPro Spike-a-Flat device");
                        Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        return false;
                    }
                    if (USBD.USBD_Connect(this._usbdHandle) != 0) {
                        Logger.Error("Unable to connect AllPro Spike-a-Flat device");
                        Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        return false;
                    }
                    this.Connected = true;
                    return true;
                }
            }, ct);
        }

        public void Disconnect() {
            this.LightOn = false;
            lock (this._handleLock) {
                if (this._usbdHandle != (IntPtr)0) {
                    USBD.USBD_Disconnect(this._usbdHandle);
                    USBD.USBD_Close(this._usbdHandle);
                    this._usbdHandle = (IntPtr)0;
                }
                this.Connected = false;
            }
        }

        public Task<bool> Close(CancellationToken ct, int delay = 300) {
            throw new NotImplementedException();
        }

        public Task<bool> Open(CancellationToken ct, int delay = 300) {
            throw new NotImplementedException();
        }

        public void SetupDialog() {
            throw new NotImplementedException();
        }
    }
}