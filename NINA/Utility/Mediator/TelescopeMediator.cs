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

    internal class TelescopeMediator : DeviceMediator<ITelescopeVM, ITelescopeConsumer, TelescopeInfo> {

        internal void MoveAxis(ASCOM.DeviceInterface.TelescopeAxes axis, double rate) {
            handler.MoveAxis(axis, rate);
        }

        internal bool Sync(double ra, double dec) {
            return handler.Sync(ra, dec);
        }

        internal Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
            return handler.SlewToCoordinatesAsync(coords);
        }

        internal bool MeridianFlip(Coordinates targetCoordinates) {
            return handler.MeridianFlip(targetCoordinates);
        }

        internal bool SetTracking(bool tracking) {
            return handler.SetTracking(tracking);
        }

        internal bool SendToSnapPort(bool start) {
            return handler.SendToSnapPort(start);
        }

        /// <summary>
        /// Updates all consumers with the current telescope info
        /// </summary>
        /// <param name="telescopeInfo"></param>
        override internal void Broadcast(TelescopeInfo telescopeInfo) {
            foreach (ITelescopeConsumer consumer in consumers) {
                consumer.UpdateTelescopeInfo(telescopeInfo);
            }
        }
    }
}