using NINA.Model;
using NINA.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IApplicationStatusMediator : IMediator<IApplicationStatusVM> {

        void StatusUpdate(ApplicationStatus status);
    }
}