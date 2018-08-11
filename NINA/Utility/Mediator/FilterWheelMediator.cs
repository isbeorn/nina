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

    internal class FilterWheelMediator : DeviceMediator<IFilterWheelVM, IFilterWheelConsumer, FilterWheelInfo> {

        internal Task<FilterInfo> ChangeFilter(FilterInfo inputFilter, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = null) {
            return handlerVM.ChangeFilter(inputFilter, token, progress);
        }

        internal ICollection<FilterInfo> GetAllFilters() {
            return handlerVM.GetAllFilters();
        }

        /// <summary>
        /// Updates all consumers with the current filterwheel info
        /// </summary>
        /// <param name="filterWheelInfo"></param>
        override internal void BroadcastInfo(FilterWheelInfo filterWheelInfo) {
            foreach (IFilterWheelConsumer vm in vms) {
                vm.UpdateFilterWheelInfo(filterWheelInfo);
            }
        }
    }
}