using NINA.Model.MyTelescope;
using NINA.Utility.Astrometry;
using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class TelescopeMediator {
        private ITelescopeVM telescopeVM;
        private List<ITelescopeConsumer> vms = new List<ITelescopeConsumer>();

        internal void RegisterTelescopeVM(ITelescopeVM telescopeVM) {
            this.telescopeVM = telescopeVM;
        }

        internal void RegisterConsumer(ITelescopeConsumer vm) {
            vms.Add(vm);
        }

        internal void RemoveConsumer(ITelescopeConsumer vm) {
            vms.Remove(vm);
        }

        internal Task<bool> Connect() {
            return telescopeVM.ChooseTelescope();
        }

        internal void Disconnect() {
            telescopeVM.Disconnect();
        }

        internal void MoveAxis(ASCOM.DeviceInterface.TelescopeAxes axis, double rate) {
            telescopeVM.MoveAxis(axis, rate);
        }

        internal bool Sync(double ra, double dec) {
            return telescopeVM.Sync(ra, dec);
        }

        internal Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
            return telescopeVM.SlewToCoordinatesAsync(coords);
        }

        internal bool MeridianFlip(Coordinates targetCoordinates) {
            return telescopeVM.MeridianFlip(targetCoordinates);
        }

        internal bool SetTracking(bool tracking) {
            return telescopeVM.SetTracking(tracking);
        }

        internal bool SendToSnapPort(bool start) {
            return telescopeVM.SendToSnapPort(start);
        }

        /// <summary>
        /// Updates all consumers with the current camera info
        /// </summary>
        /// <param name="telescopeInfo"></param>
        internal void UpdateTelescopeInfo(TelescopeInfo telescopeInfo) {
            foreach (ITelescopeConsumer vm in vms) {
                vm.UpdateTelescopeInfo(telescopeInfo);
            }
        }
    }
}