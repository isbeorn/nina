using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IFilterWheelMediator : IDeviceMediator<IFilterWheelVM, IFilterWheelConsumer, FilterWheelInfo> {

        Task<FilterInfo> ChangeFilter(FilterInfo inputFilter, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = null);

        ICollection<FilterInfo> GetAllFilters();
    }
}