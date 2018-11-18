using NINA.Model.MyFocuser;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IFocuserMediator : IDeviceMediator<IFocuserVM, IFocuserConsumer, FocuserInfo> {

        Task<int> MoveFocuser(int position);

        Task<int> MoveFocuserRelative(int position);
    }
}