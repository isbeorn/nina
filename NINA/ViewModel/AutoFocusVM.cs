using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Notification;
using OxyPlot;
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

            FocusPoints = new AsyncObservableCollection<DataPoint>();

            StartAutoFocusCommand = new AsyncCommand<bool>(
                () =>
                    Task.Run(
                        
                        async () => {
                            _autoFocusCancelToken = new CancellationTokenSource();
                            return await StartAutoFocus(_autoFocusCancelToken.Token, new Progress<string>(p => Status = p));
                        }
                    ),
                (p) => { return _focuserConnected && _cameraConnected; }
            );
            CancelAutoFocusCommand = new RelayCommand(CancelAutoFocus);

            Mediator.Instance.Register((object o) => _imageStatistics = (ImageStatistics)o, MediatorMessages.ImageStatisticsChanged);
            Mediator.Instance.Register((object o) => _focusPosition = (int)o, MediatorMessages.FocuserPositionChanged);
            Mediator.Instance.Register((object o) => _focuserConnected = (bool)o, MediatorMessages.FocuserConnectedChanged);
            Mediator.Instance.Register((object o) => _cameraConnected = (bool)o, MediatorMessages.CameraConnectedChanged);
            Mediator.Instance.Register((object o) => _temperature = (double)o, MediatorMessages.FocuserTemperatureChanged);

            Mediator.Instance.RegisterAsync(async (object o) => {
                var args = (object[])o;
                var token = (CancellationToken) args[0];
                var progress = (IProgress<string>)args[1];
                await StartAutoFocus(token, progress);
            }, AsyncMediatorMessages.StartAutoFocus);
            
        }

        private CancellationTokenSource _autoFocusCancelToken;
        private AsyncObservableCollection<DataPoint> _focusPoints;
        public AsyncObservableCollection<DataPoint> FocusPoints {
            get {
                return _focusPoints;
            }
            set {
                _focusPoints = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _minimum;

        private string _status;
        public string Status {
            get {
                return _status;
            }
            private set {
                _status = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.StatusUpdate, _status);
            }
        }

        private TrendLine _leftTrend;
        public TrendLine LeftTrend {
            get {
                return _leftTrend;
            }
            set {
                _leftTrend = value;
                RaisePropertyChanged();
            }
        }

        private TrendLine _rightTrend;
        public TrendLine RightTrend {
            get {
                return _rightTrend;
            }
            set {
                _rightTrend = value;
                RaisePropertyChanged();
            }
        }



        private ImageStatistics _imageStatistics;
        private int _focusPosition;
        private bool _focuserConnected;
        private bool _cameraConnected;
        private double _temperature;

        private async Task GetFocusPoints(int nrOfSteps, IProgress<string> progress, CancellationToken token, int offset = 0) {
            var stepSize = Settings.FocuserAutoFocusStepSize;
            if (offset != 0) {
                //Move to initial position
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserRelative, offset * stepSize);
            }



            var comparer = new FocusPointComparer();
            for (int i = 0; i < nrOfSteps; i++) {

                token.ThrowIfCancellationRequested();

                Logger.Trace("Starting Exposure for autofocus");
                var seq = new CaptureSequence(Settings.FocuserAutoFocusExposureTime, CaptureSequence.ImageTypes.SNAP, null, null, 1);
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.CaptureImage, new object[] { seq, false, progress, token });

                token.ThrowIfCancellationRequested();

                FocusPoints.AddSorted(new DataPoint(_focusPosition, _imageStatistics.HFR), comparer);
                if (i < nrOfSteps - 1) {
                    Logger.Trace("Moving focuser to next autofocus position");
                    await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserRelative, -stepSize);
                }

                token.ThrowIfCancellationRequested();
                CalculateTrends();
            }
        }

        private void CalculateTrends() {
            _minimum = FocusPoints.Aggregate((l, r) => l.Y < r.Y ? l : r);
            IEnumerable<DataPoint> leftTrendPoints = FocusPoints.Where((x) => x.X < _minimum.X && x.Y > (_minimum.Y + 0.1));
            IEnumerable<DataPoint> rightTrendPoints = FocusPoints.Where((x) => x.X > _minimum.X && x.Y > (_minimum.Y + 0.1));
            LeftTrend = new TrendLine(leftTrendPoints);
            RightTrend = new TrendLine(rightTrendPoints);
        }
        
        private async Task<bool> StartAutoFocus(CancellationToken token, IProgress<string> progress) {
            if (!(_focuserConnected && _cameraConnected)) {
                Notification.ShowError(Locale.Loc.Instance["LblAutoFocusGearNotConnected"]);
                return false;
            }

            Logger.Trace("Starting Autofocus");
            FocusPoints.Clear();
            LeftTrend = null;
            RightTrend = null;
            _minimum = new DataPoint(0, 0);
            try {

                Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars, true);

                var offsetSteps = Settings.FocuserAutoFocusInitialOffsetSteps;
                var offset = offsetSteps;

                var nrOfSteps = offsetSteps + 1;

                await GetFocusPoints(nrOfSteps, progress, token, offset);

                var laststeps = offset;

                int leftcount = LeftTrend.DataPoints.Count(), rightcount = RightTrend.DataPoints.Count();
                //When datapoints are not sufficient analyze and take more
                do {
                    if (leftcount == 0 && rightcount == 0) {
                        Notification.ShowWarning(Locale.Loc.Instance["LblAutoFocusNotEnoughtSpreadedPoints"]);
                        progress.Report(Locale.Loc.Instance["LblAutoFocusNotEnoughtSpreadedPoints"]);
                        return false;
                    }

                    var remainingSteps = Math.Min(Math.Abs(leftcount - rightcount), offsetSteps);
                    if (leftcount == rightcount && leftcount < offsetSteps) {
                        remainingSteps = offsetSteps - leftcount;
                    }

                    if ((LeftTrend.DataPoints.Count() < offsetSteps && leftcount < rightcount)
                            || (leftcount == rightcount && remainingSteps > 0)) {
                        Logger.Trace("More datapoints needed to the left of the minimum");
                        //More points needed to the left
                        laststeps += remainingSteps;
                        await GetFocusPoints(remainingSteps, progress, token, -1);
                    } else if (RightTrend.DataPoints.Count() < offsetSteps && leftcount > rightcount) {
                        Logger.Trace("More datapoints needed to the right of the minimum");
                        //More points needed to the right                         
                        offset = laststeps + remainingSteps;  //todo
                        laststeps = remainingSteps - 1;
                        await GetFocusPoints(remainingSteps, progress, token, offset);
                    }

                    leftcount = LeftTrend.DataPoints.Count();
                    rightcount = RightTrend.DataPoints.Count();

                    token.ThrowIfCancellationRequested();
                } while (rightcount < offsetSteps || leftcount < offsetSteps);



                token.ThrowIfCancellationRequested();

                //Get Trendline Intersection
                var p = LeftTrend.Intersect(RightTrend);

                progress.Report((string.Format("Ideal Position: {0}, Theoretical HFR: {1}", p.X, Math.Round(p.Y, 2))));

                LastAutoFocusPoint = new AutoFocusPoint { Focuspoint = p, Temperature = _temperature, Timestamp = DateTime.Now };

                //Todo when data is too noisy for trend lines find something else


                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserAbsolute, (int)p.X);
            } catch (OperationCanceledException) {
                FocusPoints.Clear();
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);
            }

            return true;
        }

        private AutoFocusPoint _lastAutoFocusPoint;
        public AutoFocusPoint LastAutoFocusPoint {
            get {
                return _lastAutoFocusPoint;
            }
            set {
                _lastAutoFocusPoint = value;
                RaisePropertyChanged();
            }
        }

        private void CancelAutoFocus(object obj) {
            _autoFocusCancelToken?.Cancel();
        }

        public ICommand StartAutoFocusCommand { get; private set; }
        public ICommand CancelAutoFocusCommand { get; private set; }
    }

    public class AutoFocusPoint {
        public DataPoint Focuspoint { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
    }

    public class FocusPointComparer : IComparer<DataPoint> {
        public int Compare(DataPoint x, DataPoint y) {
            if (x.X < y.X) {
                return -1;
            } else if (x.X > y.X) {
                return 1;
            } else {
                return 0;
            }
        }
    }

    public class TrendLine {
        public TrendLine(IEnumerable<DataPoint> l) {
            DataPoints = l;

            var n = DataPoints.Count();
            var sumXY = DataPoints.Sum((x) => x.X * x.Y);
            var sumX = DataPoints.Sum((x) => x.X);
            var sumY = DataPoints.Sum((x) => x.Y);
            var sumXsumY = sumX * sumY;
            var sumXsquared = DataPoints.Sum((x) => Math.Pow(x.X, 2));

            var alpha = (n * sumXY - sumXsumY) / (n * sumXsquared - Math.Pow(sumX, 2));

            var beta = (sumY - alpha * sumX) / n;
            var beta2 = (sumY * sumXsquared - sumX * sumXY) / (n * sumXsquared - Math.Pow(sumX, 2));

            Slope = alpha;
            Offset = beta;

            // y = alpha * x + beta
        }


        public double Slope { get; set; }
        public double Offset { get; set; }

        public IEnumerable<DataPoint> DataPoints { get; set; }

        public double GetY(double x) {
            return Slope * x + Offset;
        }

        public DataPoint Intersect(TrendLine line) {
            if (this.Slope == line.Slope) {
                //Lines are parallel
                return new DataPoint(0, 0);
            }
            var x = (line.Offset - this.Offset) / (this.Slope - line.Slope);
            var y = this.Slope * x + this.Offset;

            return new DataPoint((int)Math.Round(x), y);
        }
    }
}
