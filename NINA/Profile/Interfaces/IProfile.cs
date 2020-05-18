#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.ComponentModel;

namespace NINA.Profile {

    public interface IProfile : IDisposable, INotifyPropertyChanged {
        Guid Id { get; set; }
        string Name { get; set; }
        string Location { get; }
        DateTime LastUsed { get; }
        IApplicationSettings ApplicationSettings { get; set; }
        IAstrometrySettings AstrometrySettings { get; set; }
        ICameraSettings CameraSettings { get; set; }
        IColorSchemaSettings ColorSchemaSettings { get; set; }
        IFilterWheelSettings FilterWheelSettings { get; set; }
        IFlatWizardSettings FlatWizardSettings { get; set; }
        IFocuserSettings FocuserSettings { get; set; }
        IFramingAssistantSettings FramingAssistantSettings { get; set; }
        IGuiderSettings GuiderSettings { get; set; }
        IImageFileSettings ImageFileSettings { get; set; }
        IImageSettings ImageSettings { get; set; }
        IMeridianFlipSettings MeridianFlipSettings { get; set; }
        IPlanetariumSettings PlanetariumSettings { get; set; }
        IPlateSolveSettings PlateSolveSettings { get; set; }
        IPolarAlignmentSettings PolarAlignmentSettings { get; set; }
        IRotatorSettings RotatorSettings { get; set; }
        IFlatDeviceSettings FlatDeviceSettings { get; set; }
        ISequenceSettings SequenceSettings { get; set; }
        ISwitchSettings SwitchSettings { get; set; }
        ITelescopeSettings TelescopeSettings { get; set; }
        IWeatherDataSettings WeatherDataSettings { get; set; }
        IExposureCalculatorSettings ExposureCalculatorSettings { get; set; }
        ISnapShotControlSettings SnapShotControlSettings { get; set; }

        void Save();
    }
}
