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

    internal class FilterWheelMediator : DeviceMediator<IFilterWheelVM, IFilterWheelConsumer, FilterWheelInfo>, IFilterWheelMediator {

        public Task<FilterInfo> ChangeFilter(FilterInfo inputFilter, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = null) {
            return handler.ChangeFilter(inputFilter, token, progress);
        }

        public ICollection<FilterInfo> GetAllFilters() {
            return handler.GetAllFilters();
        }
    }
}