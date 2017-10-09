using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    public class AutoFocusVM : DockableVM {
        public AutoFocusVM() {
            Title = "LblAutoFocus";
            ContentId = nameof(AutoFocusVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["AutoFocusSVG"];
            StartAutoFocusCommand = new AsyncCommand<bool>(StartAutoFocus);
            CancelAutoFocusCommand = new RelayCommand(CancelAutoFocus);
        }

        private CancellationTokenSource _autoFocusCancelToken;
        private AsyncObservableCollection<FocusPoint> _focusPoints;
        public AsyncObservableCollection<FocusPoint> FocusPoints {
            get {
                return _focusPoints;
            }
            set {
                _focusPoints = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> StartAutoFocus() {
            _autoFocusCancelToken = new CancellationTokenSource();

            
            return true;
        }        

        private void CancelAutoFocus(object obj) {
            _autoFocusCancelToken?.Cancel();
        }

        public ICommand StartAutoFocusCommand { get; private set; }
        public ICommand CancelAutoFocusCommand { get; private set; }
    }

    public class FocusPoint {
        public int FocusPosition { get; set; }
        public int HFR { get; set; }
    }
}
