using NINA.Model.MyTelescope;
using NINA.Utility.Astrometry;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface ITelescopeMediator : IDeviceMediator<ITelescopeVM, ITelescopeConsumer, TelescopeInfo> {

        void MoveAxis(TelescopeAxes axis, double rate);

        bool Sync(double ra, double dec);

        Task<bool> SlewToCoordinatesAsync(Coordinates coords);

        bool MeridianFlip(Coordinates targetCoordinates);

        bool SetTracking(bool tracking);

        bool SendToSnapPort(bool start);
    }
}