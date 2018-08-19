using NINA.Model;
using NINA.Model.MyFilterWheel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Interfaces {

    internal interface IFilterWheelVM : IDeviceVM<FilterWheelInfo> {

        Task<FilterInfo> ChangeFilter(FilterInfo inputFilter, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = null);

        ICollection<FilterInfo> GetAllFilters();
    }
}