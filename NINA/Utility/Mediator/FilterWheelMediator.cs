using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class FilterWheelMediator {
        private IFilterWheelVM filterWheelVM;
        private List<IFilterWheelConsumer> vms = new List<IFilterWheelConsumer>();

        internal void RegisterFilterWheelVM(IFilterWheelVM filterWheelVM) {
            this.filterWheelVM = filterWheelVM;
        }

        internal void RegisterConsumer(IFilterWheelConsumer vm) {
            vms.Add(vm);
        }

        internal void RemoveConsumer(IFilterWheelConsumer vm) {
            vms.Remove(vm);
        }

        internal Task<bool> Connect() {
            return filterWheelVM.ChooseFW();
        }

        internal void Disconnect() {
            filterWheelVM.Disconnect();
        }

        internal Task<FilterInfo> ChangeFilter(FilterInfo inputFilter, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = null) {
            return filterWheelVM.ChangeFilter(inputFilter, token, progress);
        }

        internal ICollection<FilterInfo> GetAllFilters() {
            return filterWheelVM.GetAllFilters();
        }

        /// <summary>
        /// Updates all consumers with the current filterwheel info
        /// </summary>
        /// <param name="filterWheelInfo"></param>
        internal void UpdateFilterWheelInfo(FilterWheelInfo filterWheelInfo) {
            foreach (IFilterWheelConsumer vm in vms) {
                vm.UpdateFilterWheelInfo(filterWheelInfo);
            }
        }
    }
}