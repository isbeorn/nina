using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.WindowService {

    internal class WindowServiceFactory : IWindowServiceFactory {

        public IWindowService Create() {
            return new WindowService();
        }
    }

    internal interface IWindowServiceFactory {

        IWindowService Create();
    }
}