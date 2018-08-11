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
            handlerVM.MoveAxis(axis, rate);
        }

        internal bool Sync(double ra, double dec) {
            return handlerVM.Sync(ra, dec);
        }

        internal Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
            return handlerVM.SlewToCoordinatesAsync(coords);
        }

        internal bool MeridianFlip(Coordinates targetCoordinates) {
            return handlerVM.MeridianFlip(targetCoordinates);
        }

        internal bool SetTracking(bool tracking) {
            return handlerVM.SetTracking(tracking);
        }

        internal bool SendToSnapPort(bool start) {
            return handlerVM.SendToSnapPort(start);
        }

        /// <summary>
        /// Updates all consumers with the current telescope info
        /// </summary>
        /// <param name="telescopeInfo"></param>
        override internal void BroadcastInfo(TelescopeInfo telescopeInfo) {
            foreach (ITelescopeConsumer vm in vms) {
                vm.UpdateTelescopeInfo(telescopeInfo);
            }
        }
    }
}