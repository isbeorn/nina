#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IEquipmentVM {
        ICameraVM CameraVM { get; }
        IDomeVM DomeVM { get; }
        IFilterWheelVM FilterWheelVM { get; }
        IFlatDeviceVM FlatDeviceVM { get; }
        IFocuserVM FocuserVM { get; }
        IGuiderVM GuiderVM { get; }
        IRotatorVM RotatorVM { get; }
        ISwitchVM SwitchVM { get; }
        ITelescopeVM TelescopeVM { get; }
        IWeatherDataVM WeatherDataVM { get; }
    }
}