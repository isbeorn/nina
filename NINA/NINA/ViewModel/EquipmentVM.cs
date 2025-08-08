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
using NINA.Profile.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.ViewModel {

    internal class EquipmentVM : BaseVM, IEquipmentVM {

        public EquipmentVM(IProfileService profileService, ICameraVM cameraVM, IFilterWheelVM filterWheelVM, IFocuserVM focuserVM,
            IRotatorVM rotatorVM, ITelescopeVM telescopeVM, IDomeVM domeVM, IGuiderVM guiderVM, ISwitchVM switchVM,
            IFlatDeviceVM flatDeviceVM, IWeatherDataVM weatherDataVM, ISafetyMonitorVM safetyMonitorVM) : base(profileService) {
            CameraVM = cameraVM;
            FilterWheelVM = filterWheelVM;
            FocuserVM = focuserVM;
            RotatorVM = rotatorVM;
            TelescopeVM = telescopeVM;
            DomeVM = domeVM;
            GuiderVM = guiderVM;
            SwitchVM = switchVM;
            FlatDeviceVM = flatDeviceVM;
            WeatherDataVM = weatherDataVM;
            SafetyMonitorVM = safetyMonitorVM;
        }

        public ICameraVM CameraVM { get; }
        public IFilterWheelVM FilterWheelVM { get; }
        public IFocuserVM FocuserVM { get; }
        public IRotatorVM RotatorVM { get; }
        public ITelescopeVM TelescopeVM { get; }
        public IDomeVM DomeVM { get; }
        public IGuiderVM GuiderVM { get; }
        public ISwitchVM SwitchVM { get; }
        public IFlatDeviceVM FlatDeviceVM { get; }
        public IWeatherDataVM WeatherDataVM { get; }
        public ISafetyMonitorVM SafetyMonitorVM { get; }
    }
}