using NINA.Model.MyTelescope;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class TelescopeMediator : DeviceMediator<ITelescopeVM, ITelescopeConsumer, TelescopeInfo>, ITelescopeMediator {

        public void MoveAxis(TelescopeAxes axis, double rate) {
            handler.MoveAxis(axis, rate);
        }

        public bool Sync(double ra, double dec) {
            return handler.Sync(ra, dec);
        }

        public Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
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
    }
}