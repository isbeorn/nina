using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    internal interface ITelescopeVM {

        Task<bool> ChooseTelescope();

        Task<bool> SlewToCoordinatesAsync(Coordinates coords);

        void MoveAxis(ASCOM.DeviceInterface.TelescopeAxes axis, double rate);

        bool Sync(double ra, double dec);

        bool MeridianFlip(Coordinates targetCoordinates);

        bool SetTracking(bool tracking);

        bool SendToSnapPort(bool start);

        void Disconnect();
    }
}