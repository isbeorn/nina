using NINA.Model;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class PolarAlignmentVM : BaseVM {
        TelescopeModel _telescope;
        public TelescopeModel Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
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
            } set {
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
            } set {
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

        public PolarAlignmentVM() {
            _updateValues = new DispatcherTimer();
            _updateValues.Interval = TimeSpan.FromSeconds(1);
            _updateValues.Tick += _updateValues_Tick;
            _updateValues.Start();

            MeasureAzimuthErrorCommand = new AsyncCommand<bool>(() => measurePolarError(new Progress<string>(p => PolarErrorStatus = p), Direction.Azimuth));
            MeasureAltitudeErrorCommand = new AsyncCommand<bool>(() => measurePolarError(new Progress<string>(p => PolarErrorStatus = p), Direction.Altitude));
            SlewToMeridianOffsetCommand = new RelayCommand(slewToMeridianOffset);
            DARVSlewCommand = new AsyncCommand<bool>(() => darvslew(new Progress<string>(p => ImagingVM.ExpStatus = p), new Progress<string>(p => DarvStatus = p)));

            Zoom = 1;
            MeridianOffset = 0;
            Declination = 0;
            DARVSlewDuration = 60;
            DARVSlewRate = 0.1;
        }

        public enum Direction {
            Altitude,
            Azimuth
        }

        public enum Hemisphere {
            North, 
            South
        }

        private string _polarErrorStatus;
        public string PolarErrorStatus {
            get {
                return _polarErrorStatus;
            }

            set {
                _polarErrorStatus = value;
                RaisePropertyChanged();
            }
        }

        

        private string deg2str(double deg, int precision) {
            if (Math.Abs(deg) > 1) {
                return deg.ToString("N" + precision) + "°";
            }
            var amin = deg * 60;
            if(Math.Abs(amin) > 1) {
                return amin.ToString("N" + precision) + "'";
            }
            var asec = deg * 3600;
            return asec.ToString("N" + precision) + "''";
        }

        private string _darvStatus;
        public string DarvStatus {
            get {
                return _darvStatus;
            } set {
                _darvStatus = value;
                RaisePropertyChanged();
            }

        }

        private async Task<bool> darvTelescopeSlew(IProgress<string> progress) {
            return await Task.Run<bool>(async () => {

                //wait 5 seconds for camera to have a starting indicator
                await Task.Delay(TimeSpan.FromSeconds(5));

                Coordinates startPosition = new Coordinates(Telescope.RightAscension, Telescope.Declination);

                double rate = DARVSlewRate;

                progress.Report("Slewing...");

                //duration = half of user input minus 2 seconds for settle time
                TimeSpan duration = TimeSpan.FromSeconds((int)(DARVSlewDuration / 2) - 2);

                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, rate);

                await Task.Delay(duration);

                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);

                await Task.Delay(TimeSpan.FromSeconds(1));

                progress.Report("Slewing back...");

                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, -rate);

                await Task.Delay(duration);

                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);

                await Task.Delay(TimeSpan.FromSeconds(1));

                progress.Report("Restoring start position...");

                Telescope.slewToCoordinates(startPosition.RA, startPosition.Dec);

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
            if(ImagingVM.Cam.Connected) {
                if (!ImagingVM.IsExposing) {
                    var oldAutoStretch = ImagingVM.AutoStretch;
                    ImagingVM.AutoStretch = true;
                    var capture = ImagingVM.captureImage(DARVSlewDuration + 5, cameraprogress);
                    var slew = darvTelescopeSlew(slewprogress);

                    await Task.WhenAll(capture, slew);
                    ImagingVM.AutoStretch = oldAutoStretch;
                } else {
                    Notification.ShowWarning("Camera is busy - Cannot start DARV alignment!");
                }
            } else {
                Notification.ShowError("No camera connected for DARV alignment!");
            }
            return true;         
        }

        private async Task<bool> measurePolarError(IProgress<string> progress, Direction direction, Hemisphere hem = Hemisphere.North ) {
            if (ImagingVM.Cam.Connected) {

                if(!ImagingVM.IsExposing) { 

                    double poleErr = await calculatePoleError(progress);

                    string poleErrString = deg2str(Math.Abs(poleErr), 4);

                    if (double.IsNaN(poleErr)) {
                        /* something went wrong */
                        progress.Report("Something went wrong.");
                        return false;
                    }

                    string msg = "";

                    if (direction == Direction.Altitude) {
                        if (hem == Hemisphere.North) {
                            //if east
                            if (poleErr < 0) {
                                msg = poleErrString + " too low";
                            }
                            else {
                                msg = poleErrString + " too high";
                            }
                        }
                        else {
                            //if east
                            if (poleErr < 0) {
                                msg = poleErrString + " too high";
                            }
                            else {
                                msg = poleErrString + " too low";
                            }
                        }

                    }
                    else if (direction == Direction.Azimuth) {
                        //if northern
                        if (hem == Hemisphere.North) {
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
                    Notification.ShowWarning("Camera is busy - Cannot measure alignment error!");
                }

            }
            else {
                Notification.ShowError("No camera connected to measure alignment error!");
            }

                return true;
        }

        private async Task<double> calculatePoleError(IProgress<string> progress) {
            
            var prg = new Progress<string>(p => PolarErrorStatus = p);

            double movement = 0.5d;

            progress.Report("Solving image...");

            await PlatesolveVM.blindSolveWithCapture(prg);

            PlateSolving.PlateSolveResult startSolveResult = PlatesolveVM.PlateSolveResult;
            if(!startSolveResult.Success) {
                return double.NaN;
            }

            Coordinates startSolve = new Coordinates(startSolveResult.Ra, startSolveResult.Dec);
            startSolve = startSolve.transformToJNOW();

            Coordinates startPosition = new Coordinates(Telescope.RightAscension, Telescope.Declination);


            Coordinates targetPosition = new Coordinates(startPosition.RA - movement, startPosition.Dec);
            progress.Report("Slewing...");
            Telescope.slewToCoordinates(targetPosition.RA, targetPosition.Dec);

            progress.Report("Settling...");
            await Task.Delay(3000);

            progress.Report("Solving image...");

            await PlatesolveVM.blindSolveWithCapture(prg);
            PlateSolving.PlateSolveResult targetSolveResult = PlatesolveVM.PlateSolveResult;
            if (!targetSolveResult.Success) {
                return double.NaN;
            }

            Coordinates targetSolve = new Coordinates(targetSolveResult.Ra, targetSolveResult.Dec);
            targetSolve = targetSolve.transformToJNOW();

            progress.Report("Slewing back to origin...");
            Telescope.slewToCoordinates(startPosition.RA, startPosition.Dec);


            var decError = startSolve.Dec - targetSolve.Dec;
            // Calculate pole error
            var poleError = 3.81 * 3600.0 * decError / (4 * movement * Math.Cos(ToRadians(startPosition.Dec)));
            // Convert pole error from arcminutes to degrees
            poleError = poleError / 60.0;
            return poleError;
        }


        public static double ToRadians(double val) {
            return (Math.PI / 180) * val;
        }

        public class Coordinates {
            public double RA;
            public double Dec;
            public string RAString {
                get {
                    return Utility.Utility.AscomUtil.DegreesToHMS(RA);
                }
            }
            public string DecString {
                get {
                    return Utility.Utility.AscomUtil.DegreesToDMS(Dec);
                }
            }
            public Epoch Epoch;

            public Coordinates(double ra, double dec, Epoch epoch = Epoch.J2000) {
                this.RA = ra;
                this.Dec = dec;
                this.Epoch = epoch;
            }

            public Coordinates transformToJNOW() {
                if(Epoch == Epoch.JNOW) {
                    return this;
                }
                var transform = new ASCOM.Astrometry.Transform.Transform();
                transform.SetJ2000(RA, Dec);

                return new Coordinates((transform.RAApparent / 24) * 360, transform.DECApparent, Epoch.JNOW);
            }

        }

        public enum Epoch {
            J2000,
            JNOW
        }


        public void slewToMeridianOffset (object o) {
            double curSiderealTime = Telescope.SiderealTime;

            double slew_ra = curSiderealTime + (MeridianOffset * 24.0 / 360.0);
            if (slew_ra >= 24.0) { 
                slew_ra -= 24.0;
            }
            else if (slew_ra < 0.0) { 
                slew_ra += 24.0;
            }

            Telescope.slewToCoordinatesAsync(slew_ra, Declination);
            


        }



        private void _updateValues_Tick(object sender, EventArgs e) {
            if(Telescope.Connected) {
                var ascomutil = Utility.Utility.AscomUtil;


                var polaris = new Coordinates(ascomutil.HMSToHours("02:31:49.09"), ascomutil.DMSToDegrees("89:15:50.8"));

                

                var NOVAS31 = Utility.Utility.NOVAS31;
                double[] vector = new double[4];
                NOVAS31.RaDec2Vector(polaris.RA, polaris.Dec, 1000, ref vector);
                double[] translatedvector = new double [4];

                var util = Utility.Utility.AstroUtils;
                var jd = util.JulianDateUtc;

                NOVAS31.Precession(2451545.0, vector, jd, ref translatedvector);
                double newRA = 0.0, newDec = 0.0;
                NOVAS31.Vector2RaDec(translatedvector, ref newRA, ref newDec);
                polaris = new Coordinates(newRA, newDec, Epoch.JNOW);

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
