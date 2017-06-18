using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
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
    class FrameFocusVM : BaseVM {
        public FrameFocusVM() : base() {

            Name = "Frame & Focus";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];
            CancelSnapCommand = new RelayCommand(CancelCaptureImage);
            SnapCommand = new AsyncCommand<bool>(() => Snap(new Progress<string>(p => Status = p)));            
            Zoom = 1;
            SnapExposureDuration = 1;

            Mediator.Instance.Register((object o) => {
                IsExposing = (bool)o;
            }, MediatorMessages.IsExposingUpdate);

            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);
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

        private ICamera _cam;
        public ICamera Cam {
            get {
                return _cam;
            }
            set {
                _cam = value;
                RaisePropertyChanged();
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
                Notification.ShowWarning("Camera is busy");
                return false;
            } else {
                do {
                    _captureImageToken = new CancellationTokenSource();
                    List<SequenceModel> seq = new List<SequenceModel>();
                    seq.Add(new SequenceModel(SnapExposureDuration, SequenceModel.ImageTypes.SNAP, SnapFilter, SnapBin, 1));

                    await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.StartSequence, new object[] { seq, false, _captureImageToken, progress });

                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop);
                return true;
            }

        }

        private AsyncCommand<bool> _snapCommand;
        public AsyncCommand<bool> SnapCommand {
            get {
                return _snapCommand;
            }
            set {
                _snapCommand = value;
                RaisePropertyChanged();
            }
        }

        
        private CancellationTokenSource _captureImageToken;

        
        private AsyncCommand<bool> _autoStretchCommand;
        public AsyncCommand<bool> AutoStretchCommand {
            get {
                return _autoStretchCommand;
            } set {
                _autoStretchCommand = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand _cancelSnapCommand;
        public RelayCommand CancelSnapCommand {
            get {
                return _cancelSnapCommand;
            }

            set {
                _cancelSnapCommand = value;
                RaisePropertyChanged();
            }
        }

        private void CancelCaptureImage(object o) {
            _captureImageToken?.Cancel();
        }
    }
}
