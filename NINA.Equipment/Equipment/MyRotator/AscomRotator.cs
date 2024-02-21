#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Com.DriverAccess;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using ASCOM.Common.DeviceInterfaces;
using ASCOM.Common;
using ASCOM.Alpaca.Discovery;

namespace NINA.Equipment.Equipment.MyRotator {

    internal class AscomRotator : AscomDevice<IRotatorV3>, IRotator, IDisposable {

        public AscomRotator(string id, string name) : base(id, name) {
        }
        public AscomRotator(AscomDevice deviceMeta) : base(deviceMeta) {
        }

        public bool CanReverse => GetProperty(nameof(Rotator.CanReverse), false);

        public bool Reverse {
            get {
                if (CanReverse) {
                    return GetProperty(nameof(Rotator.Reverse), false);
                }
                return false;
            }
            set {
                if (CanReverse) {
                    SetProperty(nameof(Rotator.Reverse), value);
                }
            }
        }

        public bool IsMoving => GetProperty(nameof(Rotator.IsMoving), false);

        private bool synced;

        public bool Synced {
            get => synced;
            private set {
                synced = value;
                RaisePropertyChanged();
            }
        }

        private float offset = 0;

        public float Position => AstroUtil.EuclidianModulus(MechanicalPosition + offset, 360);

        public float MechanicalPosition => GetProperty(nameof(Rotator.Position), float.NaN);

        public float StepSize => GetProperty(nameof(Rotator.StepSize), float.NaN);

        protected override string ConnectionLostMessage => Loc.Instance["LblRotatorConnectionLost"];

        public void Halt() {
            if (IsMoving) {
                device?.Halt();
            }
        }

        public void Sync(float skyAngle) {
            offset = skyAngle - MechanicalPosition;
            RaisePropertyChanged(nameof(Position));
            Synced = true;
            Logger.Debug($"ASCOM - Mechanical Position is {MechanicalPosition}° - Sync Position to Sky Angle {skyAngle}° using offset {offset}");
        }

        public async Task<bool> Move(float angle, CancellationToken ct) {
            if (Connected) {
                if (angle >= 360) {
                    angle = AstroUtil.EuclidianModulus(angle, 360);
                }
                if (angle <= -360) {
                    angle = AstroUtil.EuclidianModulus(angle, -360);
                }

                Logger.Debug($"ASCOM - Move relative by {angle}° - Mechanical Position reported by rotator {MechanicalPosition}° and offset {offset}");
                await device.MoveAsync(angle, ct);

                return true;
            }
            return false;
        }

        public async Task<bool> MoveAbsoluteMechanical(float targetPosition, CancellationToken ct) {
            if (Connected) {
                var movement = targetPosition - MechanicalPosition;
                return await Move(movement, ct);
            }
            return false;
        }

        public async Task<bool> MoveAbsolute(float targetPosition, CancellationToken ct) {
            if (Connected) {
                return await Move(targetPosition - Position, ct);
            }
            return false;
        }

        protected override Task PreConnect() {
            offset = 0;
            Synced = false;
            return Task.CompletedTask;
        }

        protected override IRotatorV3 GetInstance() {
            if (deviceMeta == null) {
                return new Rotator(Id);
            } else {
                return new ASCOM.Alpaca.Clients.AlpacaRotator(deviceMeta.ServiceType, deviceMeta.IpAddress, deviceMeta.IpPort, deviceMeta.AlpacaDeviceNumber, false, null);
            }
        }
    }
}