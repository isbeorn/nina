using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    public class DockableVM : BaseVM {
        public DockableVM() : base() {
            this.CanClose = true;
            this.IsClosed = false;

            Mediator.Instance.Register((object o) => {
                RaisePropertyChanged(nameof(Title));
            }, MediatorMessages.LocaleChanged);
        }

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

        private string _titleLabel;
        public string Title {
            get {
                return Locale.Loc.Instance[_titleLabel]; ;
            }
            set {
                _titleLabel = value;
                RaisePropertyChanged();

            }
        }
        
        public ICommand CloseCommand { get; private set; }

        public void Close() {
            this.IsClosed = true;
        }

    }
}
