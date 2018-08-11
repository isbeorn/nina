using NINA.Model.MyFocuser;
using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class FocuserMediator {
        private IFocuserVM focuserVM;
        private List<IFocuserConsumer> vms = new List<IFocuserConsumer>();

        internal void RegisterFocuserVM(IFocuserVM focuserVM) {
            this.focuserVM = focuserVM;
        }

        internal void RegisterConsumer(IFocuserConsumer vm) {
            vms.Add(vm);
        }

        internal void RemoveConsumer(IFocuserConsumer vm) {
            vms.Remove(vm);
        }

        internal Task<bool> Connect() {
            return focuserVM.ChooseFocuser();
        }

        internal void Disconnect() {
            focuserVM.Disconnect();
        }

        internal Task<int> MoveFocuser(int position) {
            return focuserVM.MoveFocuser(position);
        }

        internal Task<int> MoveFocuserRelative(int position) {
            return focuserVM.MoveFocuserRelative(position);
        }

        /// <summary>
        /// Updates all consumers with the current focuser info
        /// </summary>
        /// <param name="focuserInfo"></param>
        internal void UpdateFocuserInfo(FocuserInfo focuserInfo) {
            foreach (IFocuserConsumer vm in vms) {
                vm.UpdateFocuserInfo(focuserInfo);
            }
        }
    }
}