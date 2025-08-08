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
using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces.Mediator {

    public interface IRotatorMediator : IDeviceMediator<IRotatorVM, IRotatorConsumer, RotatorInfo> {

        void Sync(float skyAngle);

        Task<float> MoveMechanical(float position, CancellationToken ct);

        Task<float> Move(float position, CancellationToken ct);

        Task<float> MoveRelative(float position, CancellationToken ct);

        float GetTargetPosition(float position);

        float GetTargetMechanicalPosition(float position);

        event EventHandler<RotatorEventArgs> Synced;

        event Func<object, RotatorEventArgs, Task> Moved;

        event Func<object, RotatorEventArgs, Task> MovedMechanical;
    }

    public class RotatorEventArgs : EventArgs {
        public RotatorEventArgs(float from, float to) {
            From = from;
            To = to;
        }

        public float From { get; }
        public float To { get; }
    }
}