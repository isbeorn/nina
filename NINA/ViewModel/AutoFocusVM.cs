using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Notification;
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

            StartAutoFocusCommand = new AsyncCommand<bool>(() => Task.Run(async () => await StartAutoFocus(new Progress<string>(p => Status = p))));
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

        private async Task GetFocusPoints(int nrOfSteps, int stepSize, IProgress<string> progress, CancellationToken token, int initialOffset = 0) {
            
            if(initialOffset > 0) { 
                //Move to initial position
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserRelative,initialOffset);
            }

            

            var comparer = new FocusPointComparer();
            for (int i = 0;i < nrOfSteps;i++) {

                token.ThrowIfCancellationRequested();

                Logger.Trace("Starting Exposure for autofocus");
                var seq = new CaptureSequence(Settings.FocuserAutoFocusExposureTime,CaptureSequence.ImageTypes.SNAP,null,null,1);
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.CaptureImage,new object[] { seq,false,progress,_autoFocusCancelToken });

                token.ThrowIfCancellationRequested();

                FocusPoints.AddSorted(new FocusPoint(_focusPosition,_imageStatistics.HFR),comparer);                

                Logger.Trace("Moving focuser to next autofocus position");
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserRelative,-stepSize);

                token.ThrowIfCancellationRequested();
            }            
        }
        
        private async Task<bool> StartAutoFocus(IProgress<string> progress) {
            Logger.Trace("Starting Autofocus");
            _autoFocusCancelToken = new CancellationTokenSource();
            FocusPoints.Clear();
            try {
                //Todo check if focuser and cam are connected

                Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars,true);

                var offsetSteps = Settings.FocuserAutoFocusInitialOffsetSteps;
                var stepSize = Settings.FocuserAutoFocusStepSize;
                var initialOffset = offsetSteps * stepSize;
                
                var nrOfSteps = offsetSteps * 2 + 1;

                await GetFocusPoints(nrOfSteps,stepSize,progress,_autoFocusCancelToken.Token,initialOffset);



                //Todo - when datapoints are not sufficient analyze and take more
                FocusPoint minimum = FocusPoints.Aggregate((l,r) => l.HFR < r.HFR ? l : r);
                IEnumerable<FocusPoint> leftTrendPoints = FocusPoints.Where((x) => x.FocusPosition < minimum.FocusPosition);
                IEnumerable<FocusPoint> rightTrendPoints = FocusPoints.Where((x) => x.FocusPosition > minimum.FocusPosition);
                do {
                    var remainingSteps = Math.Abs(leftTrendPoints.Count() - rightTrendPoints.Count());

                    if (rightTrendPoints.Count() < offsetSteps || leftTrendPoints.Count() > rightTrendPoints.Count()) {
                        Logger.Trace("More datapoints needed to the right of the minimum");
                        //More points needed to the right                    
                        var offset = initialOffset * 2 + stepSize + remainingSteps * stepSize;  //todo
                        await GetFocusPoints(remainingSteps,stepSize,progress,_autoFocusCancelToken.Token,offset);
                    }
                    else if (leftTrendPoints.Count() < offsetSteps || leftTrendPoints.Count() < rightTrendPoints.Count()) {
                        Logger.Trace("More datapoints needed to the left of the minimum");
                        //More points needed to the left
                        //todo offset calc
                        await GetFocusPoints(remainingSteps,stepSize,progress,_autoFocusCancelToken.Token);
                    }

                    minimum = FocusPoints.Aggregate((l,r) => l.HFR < r.HFR ? l : r);

                    leftTrendPoints = FocusPoints.Where((x) => x.FocusPosition < minimum.FocusPosition);
                    rightTrendPoints = FocusPoints.Where((x) => x.FocusPosition > minimum.FocusPosition);

                    _autoFocusCancelToken.Token.ThrowIfCancellationRequested();
                } while (rightTrendPoints.Count() < offsetSteps || leftTrendPoints.Count() < offsetSteps);

                

                _autoFocusCancelToken.Token.ThrowIfCancellationRequested();




                //Todo calcualte trend lines and intersection
                var trend1 = new TrendLine(leftTrendPoints);
                var trend2 = new TrendLine(rightTrendPoints);

                var p = trend1.Intersect(trend2);

                progress.Report((string.Format("Ideal Position: {0}, Theretical HFR: {1}", p.FocusPosition,p.HFR)));

                //Todo when data is too noisy for trend lines find something else


                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserAbsolute, p.FocusPosition);
            } catch (OperationCanceledException) {
                FocusPoints.Clear();
            } catch(Exception ex) {
                Notification.ShowError(ex.Message);
                Logger.Error(ex.Message,ex.StackTrace);
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

    public class FocusPointComparer:IComparer<FocusPoint> {
        public int Compare(FocusPoint x,FocusPoint y) {
            if(x.FocusPosition < y.FocusPosition) {
                return -1;
            } else if(x.FocusPosition > y.FocusPosition) {
                return 1;
            } else {
                return 0;
            }
        }
    }

    public class TrendLine {        
        public TrendLine(IEnumerable<FocusPoint> l) {
            _dataPoints = l;

            var n = _dataPoints.Count();
            var sumXY = _dataPoints.Sum((x) => x.FocusPosition * x.HFR);
            var sumX = _dataPoints.Sum((x) => x.FocusPosition);
            var sumY = _dataPoints.Sum((x) => x.HFR);
            var sumXsumY = sumX * sumY;
            var sumXsquared = _dataPoints.Sum((x) => Math.Pow(x.FocusPosition,2));

            var alpha = (n * sumXY - sumXsumY) / (n * sumXsquared - Math.Pow(sumX,2));

            var beta = (sumY - alpha * sumX) / n;
            var beta2 = (sumY * sumXsquared - sumX * sumXY) / (n * sumXsquared - Math.Pow(sumX,2));

            Slope = alpha;
            Offset = beta;

            // y = alpha * x + beta
        }

        
        public double Slope { get; set; }
        public double Offset { get; set; }

        IEnumerable<FocusPoint> _dataPoints;

        public double GetY(double x) {
            return Slope * x + Offset;
        }

        public FocusPoint Intersect(TrendLine line) {
            if(this.Slope == line.Slope) {
                //Lines are parallel
                return null;
            }
            var x = (line.Offset - this.Offset) / (this.Slope - line.Slope);
            var y = this.Slope * x + this.Offset;

            return new FocusPoint((int)Math.Round(x),y);
        }


    }
}
