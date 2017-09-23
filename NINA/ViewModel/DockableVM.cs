using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.ViewModel {
    public class DockableVM : BaseVM {
        public DockableVM() : base() {
            this.CanClose = true;
            this.IsClosed = false;
            IsVisible = true;

            HideCommand = new RelayCommand(Hide);

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

        protected bool _isVisible;
        public bool IsVisible {
            get {
                return _isVisible;
            }
            set {
                _isVisible = value;
                RaisePropertyChanged();
            }
        }

        private GeometryGroup _imageGeometry;
        public GeometryGroup ImageGeometry {
            get {
                return _imageGeometry;
            }
            set {
                _imageGeometry = value;
                RaisePropertyChanged();
            }
        }

        public ICommand HideCommand { get; private set; }

        public void Hide(object o) {
            this.IsVisible = !IsVisible;
        }

    }
}
