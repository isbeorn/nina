#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;

namespace NINA.WPF.Base.Interfaces.Utility {

    public interface IAllDeviceConsumer : ICameraConsumer, IFocuserConsumer, IRotatorConsumer, ITelescopeConsumer, IDomeConsumer, IFilterWheelConsumer, IGuiderConsumer, ISwitchConsumer {
        CameraInfo CameraInfo { get; }
        DomeInfo DomeInfo { get; }
        FilterWheelInfo FilterWheelInfo { get; }
        FocuserInfo FocuserInfo { get; }
        GuiderInfo GuiderInfo { get; }
        RotatorInfo RotatorInfo { get; }
        SwitchInfo SwitchInfo { get; }
        TelescopeInfo TelescopeInfo { get; }
    }
}