using NINA.Model;
using NINA.Model.MyCamera;
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

            FocusPoints = new AsyncObservableCollection<FocusPoint>();

            StartAutoFocusCommand = new AsyncCommand<bool>(() => StartAutoFocus(new Progress<string>(p => Status = p)));
            CancelAutoFocusCommand = new RelayCommand(CancelAutoFocus);

            Mediator.Instance.Register((object o) => _imageStatistics = (ImageStatistics)o,MediatorMessages.ImageStatisticsChanged);
            Mediator.Instance.Register((object o) => _focusPosition = (int)o,MediatorMessages.FocuserPositionChanged);
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

        private string _status;
        public string Status {
            get {
                return _status;
            }
            private set {
                _status = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.StatusUpdate,_status);
            }
        }

        private ImageStatistics _imageStatistics;
        private int _focusPosition;

        private async Task<bool> StartAutoFocus(IProgress<string> progress) {
            _autoFocusCancelToken = new CancellationTokenSource();
            FocusPoints.Clear();
            try {
                //Todo check if focuser and cam are connected

                Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars,true);

                var offsetSteps = Settings.FocuserAutoFocusInitialOffsetSteps;
                var stepSize = Settings.FocuserAutoFocusStepSize;
                var initialOffset = offsetSteps * stepSize;
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserRelative,initialOffset);


                var nrOfSteps = offsetSteps * 2 + 1;
                for (int i = 0; i < nrOfSteps; i++) {

                    _autoFocusCancelToken.Token.ThrowIfCancellationRequested();

                    var seq = new CaptureSequence(Settings.FocuserAutoFocusExposureTime,CaptureSequence.ImageTypes.SNAP,null,null,1);
                    await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.CaptureImage,new object[] { seq,false,progress,_autoFocusCancelToken });

                    _autoFocusCancelToken.Token.ThrowIfCancellationRequested();

                    FocusPoints.Add(new FocusPoint(_focusPosition,_imageStatistics.HFR));

                    await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserRelative, -stepSize);

                    _autoFocusCancelToken.Token.ThrowIfCancellationRequested();
                }

                //Todo - when datapoints are not sufficient analyze and take more

                var min = FocusPoints.Aggregate((l,r) => l.HFR < r.HFR ? l : r);

                var leftTrend = FocusPoints.Where((x) => x.FocusPosition < min.FocusPosition);
                var rightTrend = FocusPoints.Where((x) => x.FocusPosition > min.FocusPosition);

                //Todo calcualte trend lines and intersection

                //Todo when data is too noisy for trend lines find something else
            } catch (OperationCanceledException) {
                FocusPoints.Clear();
            }

            return true;
        }        

        private void CancelAutoFocus(object obj) {
            _autoFocusCancelToken?.Cancel();
        }

        public ICommand StartAutoFocusCommand { get; private set; }
        public ICommand CancelAutoFocusCommand { get; private set; }
    }

    public class FocusPoint {
        public FocusPoint(int pos, double hfr) {
            this.FocusPosition = pos;
            this.HFR = hfr;
        }
        public int FocusPosition { get; private set; }
        public double HFR { get; private set; }
    }
}
