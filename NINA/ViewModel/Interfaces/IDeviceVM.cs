using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.Interfaces {

    internal interface IDeviceVM<TInfo> {

        Task<bool> Connect();

        void Disconnect();

        TInfo GetDeviceInfo();
    }
}