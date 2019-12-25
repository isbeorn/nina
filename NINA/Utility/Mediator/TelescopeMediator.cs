#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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

        public bool Sync(double ra, double dec) {
            return handler.Sync(ra, dec);
        }

        public Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
            return handler.SlewToCoordinatesAsync(coords);
        }

        public Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coords) {
            return handler.SlewToCoordinatesAsync(coords);
        }

        public bool MeridianFlip(Coordinates targetCoordinates) {
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

        public bool Sync(Coordinates coordinates) {
            return handler.Sync(coordinates);
        }

        public Coordinates GetCurrentPosition() {
            return handler.GetCurrentPosition();
        }
    }
}