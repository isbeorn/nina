using NINA.Model.MySwitch;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Switch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class SwitchMediator : DeviceMediator<ISwitchVM, ISwitchConsumer, SwitchInfo>, ISwitchMediator {
    }
}