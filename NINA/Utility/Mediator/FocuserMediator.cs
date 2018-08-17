using NINA.Model.MyFocuser;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class FocuserMediator : DeviceMediator<IFocuserVM, IFocuserConsumer, FocuserInfo>, IFocuserMediator {

        public Task<int> MoveFocuser(int position) {
            return handler.MoveFocuser(position);
        }

        public Task<int> MoveFocuserRelative(int position) {
            return handler.MoveFocuserRelative(position);
        }
    }
}