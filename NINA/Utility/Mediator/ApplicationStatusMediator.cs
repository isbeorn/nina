using NINA.Model;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class ApplicationStatusMediator : IApplicationStatusMediator {
        protected IApplicationStatusVM handler;

        public void RegisterHandler(IApplicationStatusVM handler) {
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;
        }

        public void StatusUpdate(ApplicationStatus status) {
            handler.StatusUpdate(status);
        }
    }
}