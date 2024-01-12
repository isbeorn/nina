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
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.SDK.CameraSDKs.AtikSDK;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    public class AtikFilterWheel : AtikFilterWheelBase {
        private readonly int filterWheelDeviceId;
        private IntPtr filterWheelDevice;

        public AtikFilterWheel(int deviceId, IProfileService profileService) : base(profileService) {
            filterWheelDeviceId = deviceId;
        }

        public override short Position {
            get {
                if (AtikCameraDll.GetCurrentEfwMoving(filterWheelDevice)) return -1;
                else return AtikCameraDll.GetCurrentEfwPosition(filterWheelDevice);
            }
            set => AtikCameraDll.SetCurrentEfwPosition(filterWheelDevice, value);
        }

        public override string Id => AtikCameraDll.GetArtemisEfwType(filterWheelDeviceId).ToString() + " (" + AtikCameraDll.GetArtemisEfwSerial(filterWheelDeviceId) + ")";

        public override string Name => AtikCameraDll.GetArtemisEfwType(filterWheelDeviceId).ToString() + " (" + AtikCameraDll.GetArtemisEfwSerial(filterWheelDeviceId) + ")";

        public override bool Connected => AtikCameraDll.IsConnectedEfw(filterWheelDevice);

        public override string Description => "Native Atik " + AtikCameraDll.GetConnectedArtemisEfwType(filterWheelDevice).ToString() + " (Serial Nr " + AtikCameraDll.GetArtemisEfwSerial(filterWheelDeviceId) + ")";

        public override Task<bool> Connect(CancellationToken token) {
            filterWheelDevice = AtikCameraDll.ConnectEfw(filterWheelDeviceId);
            return Task.FromResult(filterWheelDevice != IntPtr.Zero);
        }

        public override void Disconnect() {
            AtikCameraDll.DisconnectEfw(filterWheelDevice);
        }

        protected override int GetEfwPositions() => AtikCameraDll.GetEfwPositions(filterWheelDevice);
    }
}