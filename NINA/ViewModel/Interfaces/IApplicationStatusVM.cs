using NINA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.Interfaces {

    internal interface IApplicationStatusVM {

        void StatusUpdate(ApplicationStatus status);
    }
}