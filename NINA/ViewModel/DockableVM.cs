using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    public class DockableVM : BaseVM {
        private bool _isClosed;
        public bool IsClosed {
            get {
                return _isClosed;
            }
            set {
                _isClosed = value;
                RaisePropertyChanged();
            }
        }


        private bool _canClose;
        public bool CanClose {
            get {
                return _canClose;
            }
            set {
                _canClose = value;
                RaisePropertyChanged();

            }
        }

        private string _contentId;
        public string ContentId {
            get {
                return _contentId;
            }
            set {
                _contentId = value;
                RaisePropertyChanged();

            }
        }

        private string _title;
        public string Title {
            get {
                return _title;
            }
            set {
                _title = value;
                RaisePropertyChanged();

            }
        }

        private ICommand _CloseCommand;
        public ICommand CloseCommand {
            get {
                if (_CloseCommand == null)
                    _CloseCommand = new RelayCommand(call => Close());
                return _CloseCommand;
            }
        }

        public DockableVM() {
            this.CanClose = true;
            this.IsClosed = false;
        }

        public void Close() {
            this.IsClosed = true;
        }

    }
}
