#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.SDK.FlatDeviceSDKs.AllPro;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyFlatDevice {

    public class AllProSpikeAFlat : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;
        private volatile IntPtr _usbdHandle = (IntPtr)0;
        private object _handleLock = new object();

        public AllProSpikeAFlat(IProfileService profileService) {
            this._profileService = profileService;
        }

        public CoverState CoverState => CoverState.NeitherOpenNorClosed;

        public int MaxBrightness => 1000;

        public int MinBrightness => 0;

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
                            Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }
                    }
                }
                RaisePropertyChanged();
            }
        }

        public int Brightness {
            get {
                lock (this._handleLock) {
                    if (!Connected) return 0;
                    int brightness = USBD.USBD_GetBrightness(this._usbdHandle);
                    return brightness;
                }
            }
            set {
                lock (this._handleLock) {
                    if (Connected) {
                        if (value < MinBrightness) {
                            value = MinBrightness;
                        }
                        if (value > MaxBrightness) {
                            value = MaxBrightness;
                        }
                        if (USBD.USBD_SetBrightness(this._usbdHandle, (uint)value) != 0) {
                            Logger.Error($"Failed to set brightness to {value}");
                            Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }
                    }
                }
            }
        }

        public string PortName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool SupportsOpenClose => false;

        public bool SupportsOnOff => true;

        public bool HasSetupDialog => false;

        public string Id => "AFE4C45C-47DB-471B-B570-9044FF6F6374";

        public string Name => "AllPro Spike-a-Flat Panel";

        public string DisplayName => $"{Loc.Instance["LblAllProSpikeAFlatPanel"]}";

        public string Category => "AllPro Software";

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

        public string DriverInfo => "AllPro-provided USB driver";

        public string DriverVersion => "Last modified 2015/01/13";

        public async Task<bool> Connect(CancellationToken ct) {
            return await Task.Run(() => {
                lock (this._handleLock) {
                    if (this._usbdHandle == (IntPtr)0) {
                        this._usbdHandle = USBD.USBD_Open();
                    }
                    if (this._usbdHandle == (IntPtr)0) {
                        Logger.Error("Unable to open AllPro Spike-a-Flat device");
                        Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        return false;
                    }
                    if (USBD.USBD_Connect(this._usbdHandle) != 0) {
                        Logger.Error("Unable to connect AllPro Spike-a-Flat device");
                        Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
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

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}