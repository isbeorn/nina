#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyTelescope;
using NINA.Utility.Astrometry;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Interfaces;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    public interface ITelescopeMediator : IDeviceMediator<ITelescopeVM, ITelescopeConsumer, TelescopeInfo> {

        void MoveAxis(TelescopeAxes axis, double rate);

        void PulseGuide(GuideDirections direction, int duration);

        Task<bool> Sync(Coordinates coordinates);

        Task<bool> SlewToCoordinatesAsync(Coordinates coords);

        Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coords);

        Task<bool> MeridianFlip(Coordinates targetCoordinates);

        bool SetTracking(bool tracking);

        bool SendToSnapPort(bool start);

        Coordinates GetCurrentPosition();

        Task<bool> ParkTelescope();

        void UnparkTelescope();
    }
}