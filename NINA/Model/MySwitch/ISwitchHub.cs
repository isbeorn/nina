using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal interface ISwitchHub : IDevice {
        ICollection<ISwitch> Switches { get; }
    }
}