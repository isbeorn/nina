#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.Hubs {

    public record HubDevice(int SourceNumber, IDevice Device);

    public partial class HubDeviceStatus : BaseINPC {
        private string error = string.Empty;

        public HubDeviceStatus(int sourceNumber, string deviceId) {
            SourceNumber = sourceNumber;
            DeviceId = deviceId;
            ConnectionError = string.Empty;
        }

        public int SourceNumber { get; }

        public string DeviceId { get; }

        [ObservableProperty]
        public partial bool Connected { get; internal set; }

        partial void OnConnectedChanged(bool value) {
            if (!value) {
                IsSafe = false;
            }
        }

        [ObservableProperty]
        public partial bool IsSafe { get; internal set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Error))]
        internal partial string ConnectionError { get; set; }

        public string Error {
            get => string.IsNullOrEmpty(ConnectionError) ? error : ConnectionError;
            internal set => SetProperty(ref error, value);
        }
    }
}
