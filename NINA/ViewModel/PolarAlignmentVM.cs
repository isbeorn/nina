using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class PolarAlignmentVM : DockableVM, ICameraConsumer, ITelescopeConsumer {

        public PolarAlignmentVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator
        ) : base(profileService) {
            Title = "LblPolarAlignment";
            ContentId = nameof(PolarAlignmentVM);

            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PolarAlignSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;

            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.applicationStatusMediator = applicationStatusMediator;

            _updateValues = new DispatcherTimer();
            _updateValues.Interval = TimeSpan.FromSeconds(10);
            _updateValues.Tick += UpdateValues_Tick;
            _updateValues.Start();

            MeasureAzimuthErrorCommand = new AsyncCommand<bool>(
                () => MeasurePolarError(new Progress<ApplicationStatus>(p => AzimuthPolarErrorStatus = p), Direction.AZIMUTH),
                (p) => (TelescopeInfo?.Connected == true && CameraInfo?.Connected == true));
            MeasureAltitudeErrorCommand = new AsyncCommand<bool>(
                () => MeasurePolarError(new Progress<ApplicationStatus>(p => AltitudePolarErrorStatus = p), Direction.ALTITUDE),
                (p) => (TelescopeInfo?.Connected == true && CameraInfo?.Connected == true));
            SlewToAltitudeMeridianOffsetCommand = new AsyncCommand<bool>(
                () => SlewToMeridianOffset(AltitudeMeridianOffset, AltitudeDeclination),
                (p) => (TelescopeInfo?.Connected == true));
            SlewToAzimuthMeridianOffsetCommand = new AsyncCommand<bool>(
                () => SlewToMeridianOffset(AzimuthMeridianOffset, AzimuthDeclination),
                (p) => (TelescopeInfo?.Connected == true));
            DARVSlewCommand = new AsyncCommand<bool>(
                () => Darvslew(new Progress<ApplicationStatus>(p => Status = p), new Progress<string>(p => DarvStatus = p)),
                (p) => (TelescopeInfo?.Connected == true && CameraInfo?.Connected == true));
            CancelDARVSlewCommand = new RelayCommand(
                Canceldarvslew,
                (p) => _cancelDARVSlewToken != null);
            CancelMeasureAltitudeErrorCommand = new RelayCommand(
                CancelMeasurePolarError,
                (p) => _cancelMeasureErrorToken != null);
            CancelMeasureAzimuthErrorCommand = new RelayCommand(
                CancelMeasurePolarError,
                (p) => _cancelMeasureErrorToken != null);

            DARVSlewDuration = 60;
            DARVSlewRate = 0.01;
            SnapExposureDuration = 2;

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RaisePropertyChanged(nameof(AzimuthMeridianOffset));
                RaisePropertyChanged(nameof(AzimuthDeclination));
                RaisePropertyChanged(nameof(AltitudeMeridianOffset));
                RaisePropertyChanged(nameof(AltitudeDeclination));
            };
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Title;
                _status.Status = _status.Status + " " + _darvStatus;
                RaisePropertyChanged();

                this.applicationStatusMediator.StatusUpdate(_status);
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

        public IAsyncCommand SlewToAzimuthMeridianOffsetCommand { get; private set; }
        public IAsyncCommand SlewToAltitudeMeridianOffsetCommand { get; private set; }

        public double AzimuthMeridianOffset {
            get {
                return profileService.ActiveProfile.PolarAlignmentSettings.AzimuthMeridianOffset;
            }

            set {
                profileService.ActiveProfile.PolarAlignmentSettings.AzimuthMeridianOffset = value;
                RaisePropertyChanged();
            }
        }

        public double AzimuthDeclination {
            get {
                return profileService.ActiveProfile.PolarAlignmentSettings.AzimuthDeclination;
            }

            set {
                profileService.ActiveProfile.PolarAlignmentSettings.AzimuthDeclination = value;
                RaisePropertyChanged();
            }
        }

        public double AltitudeMeridianOffset {
            get {
                return profileService.ActiveProfile.PolarAlignmentSettings.AltitudeMeridianOffset;
            }

            set {
                profileService.ActiveProfile.PolarAlignmentSettings.AltitudeMeridianOffset = value;
                RaisePropertyChanged();
            }
        }

        public double AltitudeDeclination {
            get {
                return profileService.ActiveProfile.PolarAlignmentSettings.AltitudeDeclination;
            }

            set {
                profileService.ActiveProfile.PolarAlignmentSettings.AltitudeDeclination = value;
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
        private ICameraMediator cameraMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private DispatcherTimer _updateValues;

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

        private short _snapGain = -1;

        public short SnapGain {
            get {
                return _snapGain;
            }

            set {
                _snapGain = value;
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

        private ApplicationStatus _altitudepolarErrorStatus;

        public ApplicationStatus AltitudePolarErrorStatus {
            get {
                return _altitudepolarErrorStatus;
            }

            set {
                _altitudepolarErrorStatus = value;
                _altitudepolarErrorStatus.Source = Title;
                RaisePropertyChanged();
                this.applicationStatusMediator.StatusUpdate(_altitudepolarErrorStatus);
            }
        }

        private ApplicationStatus _azimuthpolarErrorStatus;

        public ApplicationStatus AzimuthPolarErrorStatus {
            get {
                return _azimuthpolarErrorStatus;
            }

            set {
                _azimuthpolarErrorStatus = value;
                _azimuthpolarErrorStatus.Source = Title;
                RaisePropertyChanged();
                this.applicationStatusMediator.StatusUpdate(_azimuthpolarErrorStatus);
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

        private async Task<bool> DarvTelescopeSlew(IProgress<string> progress, CancellationToken canceltoken) {
            return await Task.Run<bool>(async () => {
                Coordinates startPosition = new Coordinates(TelescopeInfo.RightAscension, TelescopeInfo.Declination, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);
                try {
                    //wait 5 seconds for camera to have a starting indicator
                    await Task.Delay(TimeSpan.FromSeconds(5), canceltoken);

                    double rate = DARVSlewRate;
                    progress.Report("Slewing...");

                    //duration = half of user input minus 2 seconds for settle time
                    TimeSpan duration = TimeSpan.FromSeconds((int)(DARVSlewDuration / 2) - 2);

                    telescopeMediator.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, rate);

                    await Task.Delay(duration, canceltoken);

                    telescopeMediator.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);

                    await Task.Delay(TimeSpan.FromSeconds(1), canceltoken);

                    progress.Report("Slewing back...");

                    telescopeMediator.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, -rate);

                    await Task.Delay(duration, canceltoken);

                    telescopeMediator.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);

                    await Task.Delay(TimeSpan.FromSeconds(1), canceltoken);
                } catch (OperationCanceledException) {
                } finally {
                    progress.Report("Restoring start position...");
                    await telescopeMediator.SlewToCoordinatesAsync(startPosition);
                }

                progress.Report(string.Empty);

                return true;
            });
        }

        private async Task<bool> Darvslew(IProgress<ApplicationStatus> cameraprogress, IProgress<string> slewprogress) {
            if (CameraInfo?.Connected == true) {
                _cancelDARVSlewToken = new CancellationTokenSource();
                try {
                    var oldAutoStretch = imagingMediator.SetAutoStretch(true);
                    var oldDetectStars = imagingMediator.SetDetectStars(false);

                    var seq = new CaptureSequence(DARVSlewDuration + 5, CaptureSequence.ImageTypes.SNAP, SnapFilter, SnapBin, 1);
                    var capture = imagingMediator.CaptureAndPrepareImage(seq, _cancelDARVSlewToken.Token, cameraprogress);
                    var slew = DarvTelescopeSlew(slewprogress, _cancelDARVSlewToken.Token);

                    await Task.WhenAll(capture, slew);

                    imagingMediator.SetAutoStretch(oldAutoStretch);
                    imagingMediator.SetDetectStars(oldDetectStars);
                } catch (OperationCanceledException) {
                }
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblNoCameraConnected"]);
            }
            cameraprogress.Report(new ApplicationStatus() { Status = string.Empty });
            return true;
        }

        private void Canceldarvslew(object o) {
            _cancelDARVSlewToken?.Cancel();
        }

        private void CancelMeasurePolarError(object o) {
            _cancelMeasureErrorToken?.Cancel();
        }

        private AltitudeSite _altitudeSiteType;

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get {
                return cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            }
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public AltitudeSite AltitudeSiteType {
            get {
                return _altitudeSiteType;
            }
            set {
                _altitudeSiteType = value;
                RaisePropertyChanged();
            }
        }

        private TelescopeInfo telescopeInfo;
        private IImagingMediator imagingMediator;
        private ITelescopeMediator telescopeMediator;

        public TelescopeInfo TelescopeInfo {
            get {
                return telescopeInfo ?? DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
            }
            private set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> MeasurePolarError(IProgress<ApplicationStatus> progress, Direction direction) {
            if (CameraInfo?.Connected == true) {
                _cancelMeasureErrorToken = new CancellationTokenSource();
                try {
                    double poleErr = await CalculatePoleError(progress, _cancelMeasureErrorToken.Token);
                    string poleErrString = Deg2str(Math.Abs(poleErr), 4);
                    _cancelMeasureErrorToken.Token.ThrowIfCancellationRequested();
                    if (double.IsNaN(poleErr)) {
                        /* something went wrong */
                        progress.Report(new ApplicationStatus() { Status = string.Empty });
                        return false;
                    }

                    string msg = "";

                    if (direction == Direction.ALTITUDE) {
                        if (profileService.ActiveProfile.AstrometrySettings.HemisphereType == Hemisphere.NORTHERN) {
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
                        if (profileService.ActiveProfile.AstrometrySettings.HemisphereType == Hemisphere.NORTHERN) {
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

                    progress.Report(new ApplicationStatus() { Status = msg });
                } catch (OperationCanceledException) {
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
                Notification.ShowWarning(Locale.Loc.Instance["LblNoCameraConnected"]);
            }

            return true;
        }

        private async Task<double> CalculatePoleError(IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            Coordinates startPosition = new Coordinates(TelescopeInfo.RightAscension, TelescopeInfo.Declination, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);
            double poleError = double.NaN;
            try {
                double movementdeg = 0.5d;
                double movement = (movementdeg / 360) * 24;

                progress.Report(new ApplicationStatus() { Status = "Solving image..." });

                var seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAP, SnapFilter, SnapBin, 1);
                seq.Gain = SnapGain;

                var solver = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
                PlateSolveResult = await solver.SolveWithCapture(seq, progress, canceltoken);

                canceltoken.ThrowIfCancellationRequested();

                PlateSolving.PlateSolveResult startSolveResult = PlateSolveResult;
                if (!startSolveResult.Success) {
                    return double.NaN;
                }

                Coordinates startSolve = PlateSolveResult.Coordinates;
                startSolve = startSolve.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);

                Coordinates targetPosition = new Coordinates(startPosition.RA - movement, startPosition.Dec, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);
                progress.Report(new ApplicationStatus() { Status = "Slewing..." });
                await telescopeMediator.SlewToCoordinatesAsync(targetPosition);

                canceltoken.ThrowIfCancellationRequested();

                progress.Report(new ApplicationStatus() { Status = "Settling..." });
                await Task.Delay(3000);

                progress.Report(new ApplicationStatus() { Status = "Solving image..." });

                canceltoken.ThrowIfCancellationRequested();

                seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAP, SnapFilter, SnapBin, 1);
                seq.Gain = SnapGain;

                solver = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
                PlateSolveResult = await solver.SolveWithCapture(seq, progress, canceltoken);

                canceltoken.ThrowIfCancellationRequested();

                PlateSolving.PlateSolveResult targetSolveResult = PlateSolveResult;
                if (!targetSolveResult.Success) {
                    return double.NaN;
                }

                Coordinates targetSolve = PlateSolveResult.Coordinates;
                targetSolve = targetSolve.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);

                var decError = startSolve.Dec - targetSolve.Dec;
                // Calculate pole error
                poleError = 3.81 * 3600.0 * decError / (4 * movementdeg * Math.Cos(Astrometry.ToRadians(startPosition.Dec)));
                // Convert pole error from arcminutes to degrees
                poleError = Astrometry.ArcminToDegree(poleError);
            } catch (OperationCanceledException) {
            } finally {
                //progress.Report("Slewing back to origin...");
                await telescopeMediator.SlewToCoordinatesAsync(startPosition);
                //progress.Report("Done");
            }

            return poleError;
        }

        public async Task<bool> SlewToMeridianOffset(double meridianOffset, double declination) {
            double curSiderealTime = TelescopeInfo.SiderealTime;

            double slew_ra = curSiderealTime + (meridianOffset * 24.0 / 360.0);
            if (slew_ra >= 24.0) {
                slew_ra -= 24.0;
            } else if (slew_ra < 0.0) {
                slew_ra += 24.0;
            }

            var coords = new Coordinates(slew_ra, declination, Epoch.JNOW, Coordinates.RAType.Hours);
            return await telescopeMediator.SlewToCoordinatesAsync(coords);
        }

        private void UpdateValues_Tick(object sender, EventArgs e) {
            try {
                var ascomutil = Utility.Utility.AscomUtil;

                var polaris = new Coordinates(ascomutil.HMSToHours("02:31:49.09456"), ascomutil.DMSToDegrees("89:15:50.7923"), Epoch.J2000, Coordinates.RAType.Hours);
                polaris = polaris.Transform(Epoch.JNOW);

                var lst = Astrometry.GetLocalSiderealTimeNow(profileService.ActiveProfile.AstrometrySettings.Longitude);
                var hour_angle = Astrometry.GetHourAngle(lst, polaris.RA);

                Rotation = -Astrometry.HoursToDegrees(hour_angle);
                HourAngleTime = ascomutil.HoursToHMS(hour_angle);
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            this.CameraInfo = cameraInfo;
        }

        public void UpdateDeviceInfo(TelescopeInfo telescopeInfo) {
            this.TelescopeInfo = telescopeInfo;
        }
    }
}