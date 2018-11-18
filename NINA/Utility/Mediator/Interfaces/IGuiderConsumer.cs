using NINA.Model.MyGuider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IGuiderConsumer : IDeviceConsumer<GuiderInfo> {
    }
}