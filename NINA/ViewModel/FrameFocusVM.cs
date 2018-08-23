using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Profile;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class FrameFocusVM : DockableVM {

        public FrameFocusVM(IProfileService profileService, ImagingMediator imagingMediator, ApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFrameNFocus";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];
            CancelSnapCommand = new RelayCommand(CancelCaptureImage);
            SnapCommand = new AsyncCommand<bool>(() => Snap(new Progress<ApplicationStatus>(p => Status = p)));

            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            Zoom = 1;
            SnapExposureDuration = 1;
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
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

        private async Task<bool> Snap(IProgress<ApplicationStatus> progress) {
            do {
                _captureImageToken = new CancellationTokenSource();
                var seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAP, SnapFilter, SnapBin, 1);

                await imagingMediator.CaptureAndPrepareImage(seq, _captureImageToken.Token, progress);

                _captureImageToken.Token.ThrowIfCancellationRequested();
            } while (Loop);
            return true;
        }

        private CancellationTokenSource _captureImageToken;

        private void CancelCaptureImage(object o) {
            _captureImageToken?.Cancel();
        }

        public IAsyncCommand SnapCommand { get; private set; }

        private ImagingMediator imagingMediator;
        private ApplicationStatusMediator applicationStatusMediator;

        public IAsyncCommand AutoStretchCommand { get; private set; }

        public ICommand CancelSnapCommand { get; private set; }
    }
}