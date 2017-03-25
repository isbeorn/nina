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

namespace NINA.ViewModel {
    class PolarAlignmentVM : ChildVM {

        public PolarAlignmentVM(ApplicationVM root) : base(root) {

            this.TelescopeVM = root.TelescopeVM;
            this.PlatesolveVM = root.PlatesolveVM;
            this.ImagingVM = root.ImagingVM;

            _updateValues = new DispatcherTimer();
            _updateValues.Interval = TimeSpan.FromSeconds(1);
            _updateValues.Tick += UpdateValues_Tick;
            _updateValues.Start();

            MeasureAzimuthErrorCommand = new AsyncCommand<bool>(() => MeasurePolarError(new Progress<string>(p => AzimuthPolarErrorStatus = p), Direction.AZIMUTH));
            MeasureAltitudeErrorCommand = new AsyncCommand<bool>(() => MeasurePolarError(new Progress<string>(p => AltitudePolarErrorStatus = p), Direction.ALTITUDE));
            SlewToMeridianOffsetCommand = new RelayCommand(SlewToMeridianOffset);
            DARVSlewCommand = new AsyncCommand<bool>(() => darvslew(new Progress<string>(p => RootVM.Status = p), new Progress<string>(p => DarvStatus = p)));
            CancelDARVSlewCommand = new RelayCommand(Canceldarvslew);
            CancelMeasureAltitudeErrorCommand = new RelayCommand(CancelMeasurePolarError);
            CancelMeasureAzimuthErrorCommand = new RelayCommand(CancelMeasurePolarError);

            Zoom = 1;
            MeridianOffset = 0;
            Declination = 0;
            DARVSlewDuration = 60;
            DARVSlewRate = 0.01;
            SnapExposureDuration = 2;
        }

        private TelescopeVM _telescopeVM;
        public TelescopeVM TelescopeVM {
            get {
                return _telescopeVM;
            }
            set {
                _telescopeVM = value;
                RaisePropertyChanged();
            }
        }

        public TelescopeModel Telescope {
            get {
                return TelescopeVM.Telescope;
            }
        }

        private PlatesolveVM _platesolveVM;
        public PlatesolveVM PlatesolveVM {
            get {
                return _platesolveVM;
            }

            set {
                _platesolveVM = value;
                RaisePropertyChanged();
            }
        }

        private ImagingVM _imagingVM;
        public ImagingVM ImagingVM {
            get {
                return _imagingVM;
            }
            set {
                _imagingVM = value;
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

        private IAsyncCommand _measureAzimuthErrorCommand;
        public IAsyncCommand MeasureAzimuthErrorCommand {
            get {
                return _measureAzimuthErrorCommand;
            }
            private set {
                _measureAzimuthErrorCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _cancelMeasureAzimuthErrorCommand;
        public ICommand CancelMeasureAzimuthErrorCommand {
            get {
                return _cancelMeasureAzimuthErrorCommand;
            }
            private set {
                _cancelMeasureAzimuthErrorCommand = value;
                RaisePropertyChanged();
            }
        }


        private CancellationTokenSource _cancelMeasureErrorToken;
        private IAsyncCommand _measureAltitudeErrorCommand;
        public IAsyncCommand MeasureAltitudeErrorCommand {
            get {
                return _measureAltitudeErrorCommand;
            }
            private set {
                _measureAltitudeErrorCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _cancelMeasureAltitudeErrorCommand;
        public ICommand CancelMeasureAltitudeErrorCommand {
            get {
                return _cancelMeasureAltitudeErrorCommand;
            }
            private set {
                _cancelMeasureAltitudeErrorCommand = value;
                RaisePropertyChanged();
            }
        }

        private IAsyncCommand _dARVSlewCommand;
        public IAsyncCommand DARVSlewCommand {
            get {
                return _dARVSlewCommand;
            }
            private set {
                _dARVSlewCommand = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource _cancelDARVSlewToken;
        private ICommand _cancelDARVSlewCommand;
        public ICommand CancelDARVSlewCommand {
            get {
                return _cancelDARVSlewCommand;
            }
            private set {
                _cancelDARVSlewCommand = value;
                RaisePropertyChanged();
            }
        }

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



        private ICommand _slewToMeridianOffsetCommand;
        public ICommand SlewToMeridianOffsetCommand {
            get {
                return _slewToMeridianOffsetCommand;
            }
            set {
                _slewToMeridianOffsetCommand = value;
                RaisePropertyChanged();
            }
        }

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
            var amin = deg * 60;
            if (Math.Abs(amin) > 1) {
                return amin.ToString("N" + precision) + "' (arcmin)";
            }
            var asec = deg * 3600;
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
                    canceltoken.Token.ThrowIfCancellationRequested();
                    progress.Report("Slewing...");

                    //duration = half of user input minus 2 seconds for settle time
                    TimeSpan duration = TimeSpan.FromSeconds((int)(DARVSlewDuration / 2) - 2);

                    Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, rate);

                    canceltoken.Token.ThrowIfCancellationRequested();
                    await Task.Delay(duration, canceltoken.Token);
                    canceltoken.Token.ThrowIfCancellationRequested();

                    Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);

                    canceltoken.Token.ThrowIfCancellationRequested();
                    await Task.Delay(TimeSpan.FromSeconds(1), canceltoken.Token);
                    canceltoken.Token.ThrowIfCancellationRequested();

                    progress.Report("Slewing back...");

                    Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, -rate);

                    canceltoken.Token.ThrowIfCancellationRequested();
                    await Task.Delay(duration, canceltoken.Token);
                    canceltoken.Token.ThrowIfCancellationRequested();

                    Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);

                    canceltoken.Token.ThrowIfCancellationRequested();
                    await Task.Delay(TimeSpan.FromSeconds(1), canceltoken.Token);
                    canceltoken.Token.ThrowIfCancellationRequested();

                }
                catch (OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                }
                finally {
                    progress.Report("Restoring start position...");
                    Telescope.SlewToCoordinates(startPosition.RA, startPosition.Dec);
                }




                progress.Report("");
                /*double movement = DARVSlewDuration / 60;

                Coordinates startPosition = new Coordinates(Telescope.RightAscension, Telescope.Declination);
                var targetRA = startPosition.RA - movement;
                if (targetRA < 0) {
                    targetRA += 24;
                }
                else if (targetRA > 24) {
                    targetRA -= 24;
                }
                Coordinates targetPosition = new Coordinates(targetRA, startPosition.Dec);

                Telescope.slewToCoordinates(targetPosition.RA, targetPosition.Dec);

                await Task.Delay(TimeSpan.FromSeconds((int)(DARVSlewDuration/ 2)));

                Telescope.slewToCoordinates(startPosition.RA, startPosition.Dec);*/

                return true;
            });
        }

        private async Task<bool> darvslew(IProgress<string> cameraprogress, IProgress<string> slewprogress) {
            if (ImagingVM.Cam.Connected) {
                if (!ImagingVM.IsExposing) {
                    _cancelDARVSlewToken = new CancellationTokenSource();
                    try {
                        var oldAutoStretch = ImagingVM.ImageControl.AutoStretch;
                        var oldDetectStars = ImagingVM.ImageControl.DetectStars;
                        ImagingVM.ImageControl.AutoStretch = true;
                        ImagingVM.ImageControl.DetectStars = false;
                        var capture = ImagingVM.CaptureImage(DARVSlewDuration + 5, false, cameraprogress, _cancelDARVSlewToken);
                        var slew = DarvTelescopeSlew(slewprogress, _cancelDARVSlewToken);

                        await Task.WhenAll(capture, slew);
                        ImagingVM.ImageControl.AutoStretch = oldAutoStretch;
                        ImagingVM.ImageControl.DetectStars = oldDetectStars;
                    }
                    catch (OperationCanceledException ex) {
                        Logger.Trace(ex.Message);
                    }

                }
                else {
                    Notification.ShowWarning("Camera is busy - Cannot start DARV alignment!");
                }
            }
            else {
                Notification.ShowError("No camera connected for DARV alignment!");
            }
            return true;
        }

        private void Canceldarvslew(object o) {
            if (_cancelDARVSlewToken != null) {
                _cancelDARVSlewToken.Cancel();
            }
        }

        private void CancelMeasurePolarError(object o) {
            if (_cancelMeasureErrorToken != null) {
                _cancelMeasureErrorToken.Cancel();
            }
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
            if (ImagingVM.Cam != null && ImagingVM.Cam.Connected) {

                if (!ImagingVM.IsExposing) {

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
                                    }
                                    else {
                                        msg = poleErrString + " too high";
                                    }
                                }
                                else {
                                    if (poleErr < 0) {
                                        msg = poleErrString + " too high";
                                    }
                                    else {
                                        msg = poleErrString + " too low";
                                    }
                                }

                            }
                            else {
                                if (AltitudeSiteType == AltitudeSite.EAST) {
                                    if (poleErr < 0) {
                                        msg = poleErrString + " too high";
                                    }
                                    else {
                                        msg = poleErrString + " too low";
                                    }
                                }
                                else {
                                    if (poleErr < 0) {
                                        msg = poleErrString + " too low";
                                    }
                                    else {
                                        msg = poleErrString + " too high";
                                    }
                                }
                            }

                        }
                        else if (direction == Direction.AZIMUTH) {
                            //if northern
                            if (Settings.HemisphereType == Hemisphere.NORTHERN) {
                                if (poleErr < 0) {
                                    msg = poleErrString + " too east";
                                }
                                else {
                                    msg = poleErrString + " too west";
                                }
                            }
                            else {
                                if (poleErr < 0) {
                                    msg = poleErrString + " too west";
                                }
                                else {
                                    msg = poleErrString + " too east";
                                }
                            }

                        }

                        progress.Report(msg);

                    }
                    catch (OperationCanceledException ex) {
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
                }
                else {
                    Notification.ShowWarning("Camera is busy - Cannot measure alignment error!");
                }

            }
            else {
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

                await PlatesolveVM.BlindSolveWithCapture(SnapExposureDuration, progress, canceltoken, SnapFilter, SnapBin);

                canceltoken.Token.ThrowIfCancellationRequested();


                PlateSolving.PlateSolveResult startSolveResult = PlatesolveVM.PlateSolveResult;
                if (!startSolveResult.Success) {
                    return double.NaN;
                }

                Coordinates startSolve = new Coordinates(startSolveResult.Ra, startSolveResult.Dec, startSolveResult.Epoch, Coordinates.RAType.Degrees);
                startSolve = startSolve.Transform(Settings.EpochType);




                Coordinates targetPosition = new Coordinates(startPosition.RA - movement, startPosition.Dec, Settings.EpochType, Coordinates.RAType.Hours);
                progress.Report("Slewing...");
                Telescope.SlewToCoordinates(targetPosition.RA, targetPosition.Dec);


                canceltoken.Token.ThrowIfCancellationRequested();


                progress.Report("Settling...");
                await Task.Delay(3000);

                progress.Report("Solving image...");

                canceltoken.Token.ThrowIfCancellationRequested();


                await PlatesolveVM.BlindSolveWithCapture(SnapExposureDuration, progress, canceltoken, SnapFilter, SnapBin);

                canceltoken.Token.ThrowIfCancellationRequested();


                PlateSolving.PlateSolveResult targetSolveResult = PlatesolveVM.PlateSolveResult;
                if (!targetSolveResult.Success) {
                    return double.NaN;
                }

                Coordinates targetSolve = new Coordinates(targetSolveResult.Ra, targetSolveResult.Dec, targetSolveResult.Epoch, Coordinates.RAType.Degrees);
                targetSolve = targetSolve.Transform(Settings.EpochType);

                var decError = startSolve.Dec - targetSolve.Dec;
                // Calculate pole error
                poleError = 3.81 * 3600.0 * decError / (4 * movementdeg * Math.Cos(ToRadians(startPosition.Dec)));
                // Convert pole error from arcminutes to degrees
                poleError = poleError / 60.0;
            }
            catch (OperationCanceledException ex) {
                Logger.Trace(ex.Message);
            }
            finally {
                progress.Report("Slewing back to origin...");
                Telescope.SlewToCoordinates(startPosition.RA, startPosition.Dec);
                progress.Report("Done");
            }

            return poleError;
        }


        public static double ToRadians(double val) {
            return (Math.PI / 180) * val;
        }




        public void SlewToMeridianOffset(object o) {
            double curSiderealTime = Telescope.SiderealTime;

            double slew_ra = curSiderealTime + (MeridianOffset * 24.0 / 360.0);
            if (slew_ra >= 24.0) {
                slew_ra -= 24.0;
            }
            else if (slew_ra < 0.0) {
                slew_ra += 24.0;
            }

            Telescope.SlewToCoordinatesAsync(slew_ra, Declination);



        }



        private void UpdateValues_Tick(object sender, EventArgs e) {
            if (Telescope.Connected) {

                var ascomutil = Utility.Utility.AscomUtil;


                var polaris = new Coordinates(ascomutil.HMSToHours("02:31:49.09"), ascomutil.DMSToDegrees("89:15:50.8"), Epoch.J2000, Coordinates.RAType.Hours);
                polaris = polaris.Transform(Epoch.JNOW);

                /*var NOVAS31 = Astrometry.NOVAS31;
                
                double[] vector = new double[4];
                NOVAS31.RaDec2Vector(polaris.RA, polaris.Dec, 1000, ref vector);
                double[] translatedvector = new double [4];

                var util = Astrometry.AstroUtils;
                var jd = util.JulianDateUtc;

                NOVAS31.Precession(2451545.0, vector, jd, ref translatedvector);
                double newRA = 0.0, newDec = 0.0;
                NOVAS31.Vector2RaDec(translatedvector, ref newRA, ref newDec);
                polaris = new Coordinates(newRA, newDec, Epoch.JNOW);*/

                var hour_angle = Math.Abs(Telescope.SiderealTime - polaris.RA);
                if (hour_angle < 0) {
                    hour_angle += 24;
                }
                Rotation = -(hour_angle / 24) * 360;
                HourAngleTime = ascomutil.HoursToHMS(hour_angle);

            }
        }
    }
}
