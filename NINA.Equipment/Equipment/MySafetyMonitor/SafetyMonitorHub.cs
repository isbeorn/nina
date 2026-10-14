#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.Hubs;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NINA.Equipment.Equipment.MySafetyMonitor {
    public class SafetyMonitorHub : EquipmentHub, ISafetyMonitor {
        public const string DeviceId = "NINA.SafetyMonitor.Hub";
        private bool sessionConnected;

        public SafetyMonitorHub(string name) : base(DeviceId, name, "N.I.N.A.") {
        }

        public void Configure(IReadOnlyList<HubDevice> devices) {
            ConfigureDevices(devices);
        }

        protected override bool IsSupportedDevice(IDevice device) => device is ISafetyMonitor;

        protected override void SessionConnected() {
            sessionConnected = true;
        }

        protected override void SessionEnded() {
            sessionConnected = false;
            RaisePropertyChanged(nameof(IsSafe));
        }

        public override bool Connected => IsSessionActive && sessionConnected && Devices.All(x => ReadConnected(x.Device));

        public bool IsSafe {
            get {
                if (!IsSessionActive || !sessionConnected) {
                    return false;
                }

                bool safe = true;
                foreach (var child in SessionDevices) {
                    var status = GetStatus(child);
                    if (!RefreshConnectionStatus(child)) {
                        safe = false;
                        continue;
                    }

                    try {
                        bool childSafe = ((ISafetyMonitor)child.Device).IsSafe;
                        Report(child);
                        status.IsSafe = status.Connected && childSafe;
                        safe &= status.IsSafe;
                    } catch (Exception ex) {
                        Report(child, ex.Message);
                        status.IsSafe = false;
                        safe = false;
                    }
                }

                return safe && IsSessionActive && sessionConnected && SessionDevices.Count == Devices.Count;
            }
        }

        protected override void ChildChanged(HubDevice child, PropertyChangedEventArgs e) {
            base.ChildChanged(child, e);
            RaisePropertyChanged(nameof(IsSafe));
        }
    }
}
