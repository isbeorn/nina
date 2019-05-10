using NINA.Model.MySwitch;
using NINA.ViewModel.Equipment.Switch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface ISwitchMediator : IDeviceMediator<ISwitchVM, ISwitchConsumer, SwitchInfo> {
    }
}