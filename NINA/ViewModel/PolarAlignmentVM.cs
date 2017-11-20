using NINA.Model;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using NINA.Utility.Astrometry;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.Utility.Notification;

namespace NINA.ViewModel {
    class PolarAlignmentVM : DockableVM {

        public PolarAlignmentVM() : base() {
            Title = "LblPolarAlignment";
            ContentId = nameof(PolarAlignmentVM);

            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PolarAlignSVG"];

            _updateValues = new DispatcherTimer();
            _updateValues.Interval = TimeSpan.FromSeconds(10);
            _updateValues.Tick += UpdateValues_Tick;
            _updateValues.Start();

            MeasureAzimuthErrorCommand = new AsyncCommand<bool>(
                () => MeasurePolarError(new Progress<string>(p => AzimuthPolarErrorStatus = p), Direction.AZIMUTH),
                (p) => (Telescope?.Connected == true && Cam?.Connected == true));
            MeasureAltitudeErrorCommand = new AsyncCommand<bool>(
                () => MeasurePolarError(new Progress<string>(p => AltitudePolarErrorStatus = p), Direction.ALTITUDE),
                (p) => (Telescope?.Connected == true && Cam?.Connected == true));
            SlewToMeridianOffsetCommand = new RelayCommand(
                SlewToMeridianOffset,
                (p) => (Telescope?.Connected == true));
            DARVSlewCommand = new AsyncCommand<bool>(
                () => Darvslew(new Progress<string>(p => Status = p), new Progress<string>(p => DarvStatus = p)),
                (p) => (Telescope?.Connected == true && Cam?.Connected == true));
            CancelDARVSlewCommand = new RelayCommand(
                Canceldarvslew,
                (p) => _cancelDARVSlewToken != null);
            CancelMeasureAltitudeErrorCommand = new RelayCommand(
                CancelMeasurePolarError,
                (p) => _cancelMeasureErrorToken != null);
            CancelMeasureAzimuthErrorCommand = new RelayCommand(
                CancelMeasurePolarError,
                (p) => _cancelMeasureErrorToken != null);

            MeridianOffset = 0;
            Declination = 0;
            DARVSlewDuration = 60;
            DARVSlewRate = 0.01;
            SnapExposureDuration = 2;

            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);

            Mediator.Instance.Register((object o) => {
                Telescope = (ITelescope)o;
            }, MediatorMessages.TelescopeChanged);

            Mediator.Instance.Register((object o) => {
                IsExposing = (bool)o;
            }, MediatorMessages.IsExposingUpdate);

            Mediator.Instance.Register((object o) => {
                _autoStretch = (bool)o;
            }, MediatorMessages.AutoStrechChanged);
            Mediator.Instance.Register((object o) => {
                _detectStars = (bool)o;
            }, MediatorMessages.DetectStarsChanged);

            Mediator.Instance.Register((object o) => {
                _plateSolveResult = (PlateSolving.PlateSolveResult)o;
            }, MediatorMessages.PlateSolveResultChanged);
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

        private ITelescope _telescope;
        public ITelescope Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
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

        private PlateSolving.PlateSolveResult _plateSolveResult;
        public PlateSolving.PlateSolveResult PlateSolveResult {
            get {
                return _plateSolveResult;
            }
            set {
                _plateSolveResult = value;
                RaisePropertyChanged();
            }
        }

        private double _rotation;
        public double Rotation {
            get {
                return _rotation;
            }
            set {
                _rotation = value;
                RaisePropertyChanged();
            }
        }

        public string HourAngleTime {
            get {
                return _hourAngleTime;
            }

            set {
                _hourAngleTime = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand MeasureAzimuthErrorCommand { get; private set; }

        public ICommand CancelMeasureAzimuthErrorCommand { get; private set; }

        private CancellationTokenSource _cancelMeasureErrorToken;

        public IAsyncCommand MeasureAltitudeErrorCommand { get; private set; }

        public ICommand CancelMeasureAltitudeErrorCommand { get; private set; }

        public IAsyncCommand DARVSlewCommand { get; private set; }

        private CancellationTokenSource _cancelDARVSlewToken;

        public ICommand CancelDARVSlewCommand { get; private set; }

        private double _dARVSlewRate;
        public double DARVSlewRate {
            get {
                return _dARVSlewRate;
            }
            set {
                _dARVSlewRate = value;
                RaisePropertyChanged();
            }
        }

        public ICommand SlewToMeridianOffsetCommand { get; private set; }

        private double _meridianOffset;
        private double _declination;

        public double MeridianOffset {
            get {
                return _meridianOffset;
            }

            set {
                _meridianOffset = value;
                RaisePropertyChanged();
            }
        }

        public double Declination {
            get {
                return _declination;
            }

            set {
                _declination = value;
                RaisePropertyChanged();
            }
        }

        private double _dARVSlewDuration;
        public double DARVSlewDuration {
            get {
                return _dARVSlewDuration;
            }
            set {
                _dARVSlewDuration = value;
                RaisePropertyChanged();
            }
        }


        private string _hourAngleTime;


        DispatcherTimer _updateValues;

        private BinningMode _snapBin;
        private Model.MyFilterWheel.FilterInfo _snapFilter;
        private double _snapExposureDuration;

        public BinningMode SnapBin {
            get {
                return _snapBin;
            }

            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }

            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }



        private string _altitudepolarErrorStatus;
        public string AltitudePolarErrorStatus {
            get {
                return _altitudepolarErrorStatus;
            }

            set {
                _altitudepolarErrorStatus = value;
                RaisePropertyChanged();
            }
        }

        private string _azimuthpolarErrorStatus;
        public string AzimuthPolarErrorStatus {
            get {
                return _azimuthpolarErrorStatus;
            }

            set {
                _azimuthpolarErrorStatus = value;
                RaisePropertyChanged();
            }
        }


        private string Deg2str(double deg, int precision) {
            if (Math.Abs(deg) > 1) {
                return deg.ToString("N" + precision) + "° (degree)";
            }
            var amin = Astrometry.DegreeToArcmin(deg);
            if (Math.Abs(amin) > 1) {
                return amin.ToString("N" + precision) + "' (arcmin)";
            }
            var asec = Astrometry.DegreeToArcsec(deg);
            return asec.ToString("N" + precision) + "'' (arcsec)";
        }

        private string _darvStatus;
        public string DarvStatus {
            get {
                return _darvStatus;
            }
            set {
                _darvStatus = value;
                RaisePropertyChanged();
            }

        }

        private async Task<bool> DarvTelescopeSlew(IProgress<string> progress, CancellationTokenSource canceltoken) {
            return await Task.Run<bool>(async () => {
                Coordinates startPosition = new Coordinates(Telescope.RightAscension, Telescope.Declination, Settings.EpochType, Coordinates.RAType.Hours);
                try {
                    //wait 5 seconds for camera to have a starting indicator
                    await Task.Delay(TimeSpan.FromSeconds(5), canceltoken.Token);

                    double rate = DARVSlewRate;
                    progress.Report("Slewing...");

                    //duration = half of user input minus 2 seconds for settle time
                    TimeSpan duration = TimeSpan.FromSeconds((int)(DARVSlewDuration / 2) - 2);

                    Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, rate);

                    await Task.Delay(duration, canceltoken.Token);

                    Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);

                    await Task.Delay(TimeSpan.FromSeconds(1), canceltoken.Token);

                    progress.Report("Slewing back...");

                    Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, -rate);

                    await Task.Delay(duration, canceltoken.Token);

                    Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);

                    await Task.Delay(TimeSpan.FromSeconds(1), canceltoken.Token);
                } catch (OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                } finally {
                    progress.Report("Restoring start position...");
                    await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SlewToCoordinates, startPosition);
                }

                progress.Report(string.Empty);

                return true;
            });
        }

        private bool _autoStretch;
        private bool _detectStars;

        private async Task<bool> Darvslew(IProgress<string> cameraprogress, IProgress<string> slewprogress) {
            if (Cam?.Connected == true) {
                if (!IsExposing) {
                    _cancelDARVSlewToken = new CancellationTokenSource();
                    try {
                        var oldAutoStretch = _autoStretch;
                        var oldDetectStars = _detectStars;
                        Mediator.Instance.Notify(MediatorMessages.ChangeAutoStretch, true);
                        Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars, false);

                        var seq = new CaptureSequence(DARVSlewDuration + 5, CaptureSequence.ImageTypes.SNAP, null, null, 1);
                        var capture = Mediator.Instance.NotifyAsync(AsyncMediatorMessages.CaptureImage, new object[] { seq, false, cameraprogress, _cancelDARVSlewToken.Token });
                        var slew = DarvTelescopeSlew(slewprogress, _cancelDARVSlewToken);

                        await Task.WhenAll(capture, slew);

                        Mediator.Instance.Notify(MediatorMessages.ChangeAutoStretch, oldAutoStretch);
                        Mediator.Instance.Notify(MediatorMessages.ChangeDetectStars, oldDetectStars);
                    } catch (OperationCanceledException ex) {
                        Logger.Trace(ex.Message);
                    }

                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblCameraBusy"]);
                }
            } else {
                Notification.ShowError("No camera connected for DARV alignment!");
            }
            return true;
        }

        private void Canceldarvslew(object o) {
            _cancelDARVSlewToken?.Cancel();
        }

        private void CancelMeasurePolarError(object o) {
            _cancelMeasureErrorToken?.Cancel();
        }

        private AltitudeSite _altitudeSiteType;
        public AltitudeSite AltitudeSiteType {
            get {
                return _altitudeSiteType;
            }
            set {
                _altitudeSiteType = value;
                RaisePropertyChanged();
            }
        }



        private async Task<bool> MeasurePolarError(IProgress<string> progress, Direction direction) {
            if (Cam?.Connected == true) {

                if (!IsExposing) {

                    _cancelMeasureErrorToken = new CancellationTokenSource();
                    try {

                        double poleErr = await CalculatePoleError(progress, _cancelMeasureErrorToken);
                        string poleErrString = Deg2str(Math.Abs(poleErr), 4);
                        _cancelMeasureErrorToken.Token.ThrowIfCancellationRequested();
                        if (double.IsNaN(poleErr)) {
                            /* something went wrong */
                            progress.Report("Something went wrong.");
                            return false;
                        }

                        string msg = "";

                        if (direction == Direction.ALTITUDE) {
                            if (Settings.HemisphereType == Hemisphere.NORTHERN) {

                                if (AltitudeSiteType == AltitudeSite.EAST) {
                                    if (poleErr < 0) {
                                        msg = poleErrString + " too low";
                                    } else {
                                        msg = poleErrString + " too high";
                                    }
                                } else {
                                    if (poleErr < 0) {
                                        msg = poleErrString + " too high";
                                    } else {
                                        msg = poleErrString + " too low";
                                    }
                                }

                            } else {
                                if (AltitudeSiteType == AltitudeSite.EAST) {
                                    if (poleErr < 0) {
                                        msg = poleErrString + " too high";
                                    } else {
                                        msg = poleErrString + " too low";
                                    }
                                } else {
                                    if (poleErr < 0) {
                                        msg = poleErrString + " too low";
                                    } else {
                                        msg = poleErrString + " too high";
                                    }
                                }
                            }

                        } else if (direction == Direction.AZIMUTH) {
                            //if northern
                            if (Settings.HemisphereType == Hemisphere.NORTHERN) {
                                if (poleErr < 0) {
                                    msg = poleErrString + " too east";
                                } else {
                                    msg = poleErrString + " too west";
                                }
                            } else {
                                if (poleErr < 0) {
                                    msg = poleErrString + " too west";
                                } else {
                                    msg = poleErrString + " too east";
                                }
                            }

                        }

                        progress.Report(msg);

                    } catch (OperationCanceledException ex) {
                        Logger.Trace(ex.Message);
                        progress.Report("Canceled");
                    }

                    /*  Altitude
                     *      Northern
                     *          East side
                     *              poleError < 0 -> too low
                     *              poleError > 0 -> too high
                     *  Azimuth
                     *      Northern
                     *          South side
                     *              poleError < 0 -> too east
                     *              poleError > 0 -> too west
                     */
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblCameraBusy"]);
                }

            } else {
                Notification.ShowError("No camera connected to measure alignment error!");
            }

            return true;
        }

        private async Task<double> CalculatePoleError(IProgress<string> progress, CancellationTokenSource canceltoken) {


            Coordinates startPosition = new Coordinates(Telescope.RightAscension, Telescope.Declination, Settings.EpochType, Coordinates.RAType.Hours);
            double poleError = double.NaN;
            try {

                double movementdeg = 0.5d;
                double movement = (movementdeg / 360) * 24;

                progress.Report("Solving image...");

                var seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAP, SnapFilter, SnapBin, 1);
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SolveWithCapture, new object[] { seq, progress, canceltoken });

                canceltoken.Token.ThrowIfCancellationRequested();


                PlateSolving.PlateSolveResult startSolveResult = PlateSolveResult;
                if (!startSolveResult.Success) {
                    return double.NaN;
                }

                Coordinates startSolve = PlateSolveResult.Coordinates;
                startSolve = startSolve.Transform(Settings.EpochType);




                Coordinates targetPosition = new Coordinates(startPosition.RA - movement, startPosition.Dec, Settings.EpochType, Coordinates.RAType.Hours);
                progress.Report("Slewing...");
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SlewToCoordinates, targetPosition);


                canceltoken.Token.ThrowIfCancellationRequested();


                progress.Report("Settling...");
                await Task.Delay(3000);

                progress.Report("Solving image...");

                canceltoken.Token.ThrowIfCancellationRequested();

                seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAP, SnapFilter, SnapBin, 1);
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SolveWithCapture, new object[] { seq, progress, canceltoken });

                canceltoken.Token.ThrowIfCancellationRequested();


                PlateSolving.PlateSolveResult targetSolveResult = PlateSolveResult;
                if (!targetSolveResult.Success) {
                    return double.NaN;
                }

                Coordinates targetSolve = PlateSolveResult.Coordinates;
                targetSolve = targetSolve.Transform(Settings.EpochType);

                var decError = startSolve.Dec - targetSolve.Dec;
                // Calculate pole error
                poleError = 3.81 * 3600.0 * decError / (4 * movementdeg * Math.Cos(Astrometry.ToRadians(startPosition.Dec)));
                // Convert pole error from arcminutes to degrees
                poleError = Astrometry.ArcminToDegree(poleError);
            } catch (OperationCanceledException ex) {
                Logger.Trace(ex.Message);
            } finally {
                //progress.Report("Slewing back to origin...");
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SlewToCoordinates, startPosition);
                //progress.Report("Done");
            }

            return poleError;
        }

        public void SlewToMeridianOffset(object o) {
            double curSiderealTime = Telescope.SiderealTime;

            double slew_ra = curSiderealTime + (MeridianOffset * 24.0 / 360.0);
            if (slew_ra >= 24.0) {
                slew_ra -= 24.0;
            } else if (slew_ra < 0.0) {
                slew_ra += 24.0;
            }

            var coords = new Coordinates(slew_ra, Declination, Epoch.JNOW, Coordinates.RAType.Hours);

            Mediator.Instance.Notify(MediatorMessages.SlewToCoordinates, coords);
        }

        private void UpdateValues_Tick(object sender, EventArgs e) {

            var ascomutil = Utility.Utility.AscomUtil;

            var polaris = new Coordinates(ascomutil.HMSToHours("02:31:49.09456"), ascomutil.DMSToDegrees("89:15:50.7923"), Epoch.J2000, Coordinates.RAType.Hours);
            polaris = polaris.Transform(Epoch.JNOW);

            var lst = Astrometry.GetLocalSiderealTimeNow(Settings.Longitude);
            var hour_angle = Astrometry.GetHourAngle(lst, polaris.RA);

            Rotation = -Astrometry.HoursToDegrees(hour_angle);
            HourAngleTime = ascomutil.HoursToHMS(hour_angle);


        }
    }
}
