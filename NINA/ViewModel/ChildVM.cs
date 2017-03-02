using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {
    class ChildVM : BaseVM {

        ApplicationVM _rootVM;
        public ApplicationVM RootVM {
            get {
                return _rootVM;
            }
            set {
                _rootVM = value;
                RaisePropertyChanged();
            }
        }

        private ChildVM() {

        }

        public ChildVM(ApplicationVM rootVM) : base() {
            this.RootVM = rootVM;

        }
    }
}
