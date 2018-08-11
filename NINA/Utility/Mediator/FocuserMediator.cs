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

    internal class FocuserMediator : DeviceMediator<IFocuserVM, IFocuserConsumer, FocuserInfo> {

        internal Task<int> MoveFocuser(int position) {
            return handlerVM.MoveFocuser(position);
        }

        internal Task<int> MoveFocuserRelative(int position) {
            return handlerVM.MoveFocuserRelative(position);
        }

        /// <summary>
        /// Updates all consumers with the current focuser info
        /// </summary>
        /// <param name="focuserInfo"></param>
        override internal void BroadcastInfo(FocuserInfo focuserInfo) {
            foreach (IFocuserConsumer vm in vms) {
                vm.UpdateFocuserInfo(focuserInfo);
            }
        }
    }
}