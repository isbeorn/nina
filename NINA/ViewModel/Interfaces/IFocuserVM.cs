using NINA.Model.MyFocuser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.Interfaces {

    internal interface IFocuserVM : IDeviceVM<FocuserInfo> {

        Task<int> MoveFocuser(int position);

        Task<int> MoveFocuserRelative(int position);
    }
}