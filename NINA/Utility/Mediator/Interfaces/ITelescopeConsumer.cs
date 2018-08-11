using NINA.Model.MyTelescope;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface ITelescopeConsumer {

        void UpdateTelescopeInfo(TelescopeInfo telescopeInfo);
    }
}