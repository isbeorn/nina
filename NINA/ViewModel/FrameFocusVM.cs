using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {
    class FrameFocusVM : DockableVM {
        public FrameFocusVM() : base() {

            Title = "LblFrameNFocus";
            ContentId = nameof(FrameFocusVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];
            CancelSnapCommand = new RelayCommand(CancelCaptureImage);
            SnapCommand = new AsyncCommand<bool>(() => Snap(new Progress<string>(p => Status = p)));
            Zoom = 1;
            SnapExposureDuration = 1;

            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                IsExposing = (bool)o;
            }, MediatorMessages.IsExposingUpdate);

        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();

                Mediator.Instance.Notify(MediatorMessages.StatusUpdate, _status);
            }
        }

        private bool _isExposing;
        public bool IsExposing {
            get {
                return _isExposing;
            }
            set {
                _isExposing = value;
                RaisePropertyChanged();
            }
        }

        private bool _loop;
        public bool Loop {
            get {
                return _loop;
            }
            set {
                _loop = value;
                RaisePropertyChanged();
            }

        }

        private bool _calcHFR;
        public bool CalcHFR {
            get {
                return _calcHFR;
            }
            set {
                _calcHFR = value;
                RaisePropertyChanged();
            }

        }

        private double _zoom;
        public double Zoom {
            get {
                return _zoom;
            }
            set {
                _zoom = value;
                RaisePropertyChanged();
            }
        }

        private double _snapExposureDuration;
        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }
            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private Model.MyFilterWheel.FilterInfo _snapFilter;
        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }
            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        private BinningMode _snapBin;
        public BinningMode SnapBin {
            get {
                if (_snapBin == null) {
                    _snapBin = new BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }



        private async Task<bool> Snap(IProgress<string> progress) {
            if (IsExposing) {
                Notification.ShowWarning(Locale.Loc.Instance["LblCameraBusy"]);
                return false;
            } else {
                do {
                    _captureImageToken = new CancellationTokenSource();
                    CaptureSequenceList seq = new CaptureSequenceList(new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAP, SnapFilter, SnapBin, 1));

                    await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.StartSequence, new object[] { seq, false, _captureImageToken, progress });

                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop);
                return true;
            }

        }

        private CancellationTokenSource _captureImageToken;

        private void CancelCaptureImage(object o) {
            _captureImageToken?.Cancel();
        }

        public IAsyncCommand SnapCommand { get; private set; }

        public IAsyncCommand AutoStretchCommand { get; private set; }

        public ICommand CancelSnapCommand { get; private set; }
    }
}
