#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.WPF.Base.Mediator {

    public class RotatorMediator : DeviceMediator<IRotatorVM, IRotatorConsumer, RotatorInfo>, IRotatorMediator {

        public void Sync(float skyAngle) {
            handler.Sync(skyAngle);
        }

        public Task<float> MoveMechanical(float position, CancellationToken ct) {
            return handler.MoveMechanical(position, ct);
        }

        public Task<float> Move(float position, CancellationToken ct) {
            return handler.Move(position, ct);
        }

        public Task<float> MoveRelative(float position, CancellationToken ct) {
            return handler.MoveRelative(position, ct);
        }

        public float GetTargetPosition(float position) {
            return handler.GetTargetPosition(position);
        }

        public float GetTargetMechanicalPosition(float position) {
            return handler.GetTargetMechanicalPosition(position);
        }
    }
}