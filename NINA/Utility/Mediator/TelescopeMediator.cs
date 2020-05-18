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
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Interfaces;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class TelescopeMediator : DeviceMediator<ITelescopeVM, ITelescopeConsumer, TelescopeInfo>, ITelescopeMediator {

        public void MoveAxis(TelescopeAxes axis, double rate) {
            handler.MoveAxis(axis, rate);
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            handler.PulseGuide(direction, duration);
        }

        public async Task<bool> Sync(Coordinates coordinates) {
            return await handler.Sync(coordinates);
        }

        public Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
            return handler.SlewToCoordinatesAsync(coords);
        }

        public Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coords) {
            return handler.SlewToCoordinatesAsync(coords);
        }

        public Task<bool> MeridianFlip(Coordinates targetCoordinates) {
            return handler.MeridianFlip(targetCoordinates);
        }

        public bool SetTracking(bool tracking) {
            return handler.SetTracking(tracking);
        }

        public bool SendToSnapPort(bool start) {
            return handler.SendToSnapPort(start);
        }

        public Task<bool> ParkTelescope() {
            return handler.ParkTelescope();
        }

        public void UnparkTelescope() {
            handler.UnparkTelescope();
        }

        public Coordinates GetCurrentPosition() {
            return handler.GetCurrentPosition();
        }
    }
}
