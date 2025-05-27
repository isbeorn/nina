#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;

namespace NINA.Equipment.Interfaces.ViewModel {

    public interface ITelescopeVM : IDeviceVM<TelescopeInfo>, IDockableVM {

        Task<bool> SlewToCoordinatesAsync(Coordinates coords, CancellationToken token);
        
        [Obsolete]
        Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coordinates, CancellationToken token);

        Task<bool> SlewToTopocentricCoordinates(TopocentricCoordinates coordinates, CancellationToken token);

        void MoveAxis(TelescopeAxes axis, double rate);

        void PulseGuide(GuideDirections direction, int duration);

        Task<bool> Sync(Coordinates coordinates);

        Task<bool> MeridianFlip(Coordinates targetCoordinates, CancellationToken token);

        bool SendToSnapPort(bool start);

        Coordinates GetCurrentPosition();

        Task<bool> ParkTelescope(IProgress<ApplicationStatus> progress, CancellationToken token);

        Task<bool> UnparkTelescope(IProgress<ApplicationStatus> progress, CancellationToken token);

        Task WaitForSlew(CancellationToken cancellationToken);

        bool SetTrackingEnabled(bool tracking);

        bool SetTrackingMode(TrackingMode trackingMode);

        bool SetCustomTrackingRate(SiderealShiftTrackingRate rate);

        Task<bool> FindHome(IProgress<ApplicationStatus> progress, CancellationToken token);

        void StopSlew();

        PierSide DestinationSideOfPier(Coordinates coordinates);
        event Func<object, EventArgs, Task> Parked;
        event Func<object, EventArgs, Task> Unparked;
        event Func<object, EventArgs, Task> Homed;
        event Func<object, MountSlewedEventArgs, Task> Slewed;
    }
}