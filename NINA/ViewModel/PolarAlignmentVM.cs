#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using NINA.Utility.Mediator;
using NINA.PlateSolving;

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

            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PolarAlignSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;

            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.applicationStatusMediator = applicationStatusMediator;

            updateValues = new DispatcherTimer();
            updateValues.Interval = TimeSpan.FromSeconds(10);
            updateValues.Tick += UpdateValues_Tick;

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
                (p) => cancelDARVSlewToken != null);
            CancelMeasureAltitudeErrorCommand = new RelayCommand(
                CancelMeasurePolarError,
                (p) => cancelMeasureErrorToken != null);
            CancelMeasureAzimuthErrorCommand = new RelayCommand(
                CancelMeasurePolarError,
                (p) => cancelMeasureErrorToken != null);

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

        public override void Hide(object o) {
            this.IsVisible = !IsVisible;
            if (IsVisible) {
                UpdateValues_Tick(null, null);
                updateValues.Start();
            } else {
                updateValues.Stop();
            }
        }

        private ApplicationStatus status;

        public ApplicationStatus Status {
            get {
                return status;
            }
            set {
                status = value;
                status.Source = Title;
                status.Status = status.Status + " " + darvStatus;
                RaisePropertyChanged();

                this.applicationStatusMediator.StatusUpdate(status);
            }
        }

        private PlateSolving.PlateSolveResult plateSolveResult;

        public PlateSolving.PlateSolveResult PlateSolveResult {
            get {
                return plateSolveResult;
            }
            set {
                plateSolveResult = value;
                RaisePropertyChanged();
            }
        }

        private double rotation;

        public double Rotation {
            get {
                return rotation;
            }
            set {
                rotation = value;
                RaisePropertyChanged();
            }
        }

        public string HourAngleTime {
            get {
                return hourAngleTime;
            }

            set {
                hourAngleTime = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand MeasureAzimuthErrorCommand { get; private set; }

        public ICommand CancelMeasureAzimuthErrorCommand { get; private set; }

        private CancellationTokenSource cancelMeasureErrorToken;

        public IAsyncCommand MeasureAltitudeErrorCommand { get; private set; }

        public ICommand CancelMeasureAltitudeErrorCommand { get; private set; }

        public IAsyncCommand DARVSlewCommand { get; private set; }

        private CancellationTokenSource cancelDARVSlewToken;

        public ICommand CancelDARVSlewCommand { get; private set; }

        private double dARVSlewRate;

        public double DARVSlewRate {
            get {
                return dARVSlewRate;
            }
            set {
                dARVSlewRate = value;
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

        private double dARVSlewDuration;

        public double DARVSlewDuration {
            get {
                return dARVSlewDuration;
            }
            set {
                dARVSlewDuration = value;
                RaisePropertyChanged();
            }
        }

        private string hourAngleTime;
        private ICameraMediator cameraMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private DispatcherTimer updateValues;

        private BinningMode snapBin;
        private Model.MyFilterWheel.FilterInfo snapFilter;
        private double snapExposureDuration;

        public BinningMode SnapBin {
            get {
                return snapBin;
            }

            set {
                snapBin = value;
                RaisePropertyChanged();
            }
        }

        private int snapGain = -1;

        public int SnapGain {
            get {
                return snapGain;
            }

            set {
                snapGain = value;
                RaisePropertyChanged();
            }
        }

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return snapFilter;
            }

            set {
                snapFilter = value;
                RaisePropertyChanged();
            }
        }

        public double SnapExposureDuration {
            get {
                return snapExposureDuration;
            }

            set {
                snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private ApplicationStatus altitudepolarErrorStatus;

        public ApplicationStatus AltitudePolarErrorStatus {
            get {
                return altitudepolarErrorStatus;
            }

            set {
                altitudepolarErrorStatus = value;
                altitudepolarErrorStatus.Source = Title;
                RaisePropertyChanged();
                this.applicationStatusMediator.StatusUpdate(altitudepolarErrorStatus);
            }
        }

        private ApplicationStatus azimuthpolarErrorStatus;

        public ApplicationStatus AzimuthPolarErrorStatus {
            get {
                return azimuthpolarErrorStatus;
            }

            set {
                azimuthpolarErrorStatus = value;
                azimuthpolarErrorStatus.Source = Title;
                RaisePropertyChanged();
                this.applicationStatusMediator.StatusUpdate(azimuthpolarErrorStatus);
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

        private string darvStatus;

        public string DarvStatus {
            get {
                return darvStatus;
            }
            set {
                darvStatus = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> DarvTelescopeSlew(IProgress<string> progress, CancellationToken canceltoken) {
            return await Task.Run<bool>(async () => {
                Coordinates startPosition = new Coordinates(TelescopeInfo.RightAscension, TelescopeInfo.Declination, TelescopeInfo.EquatorialSystem, Coordinates.RAType.Hours);
                try {
                    //wait 5 seconds for camera to have a starting indicator
                    await Task.Delay(TimeSpan.FromSeconds(5), canceltoken);

                    double rate = DARVSlewRate;
                    progress.Report("Slewing...");

                    //duration = half of user input minus 2 seconds for settle time
                    TimeSpan duration = TimeSpan.FromSeconds((int)(DARVSlewDuration / 2) - 2);

                    telescopeMediator.MoveAxis(TelescopeAxes.Primary, rate);

                    await Task.Delay(duration, canceltoken);

                    telescopeMediator.MoveAxis(TelescopeAxes.Primary, 0);

                    await Task.Delay(TimeSpan.FromSeconds(1), canceltoken);

                    progress.Report("Slewing back...");

                    telescopeMediator.MoveAxis(TelescopeAxes.Primary, -rate);

                    await Task.Delay(duration, canceltoken);

                    telescopeMediator.MoveAxis(TelescopeAxes.Primary, 0);

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
                cancelDARVSlewToken?.Dispose();
                cancelDARVSlewToken = new CancellationTokenSource();
                try {
                    var seq = new CaptureSequence(DARVSlewDuration + 5, CaptureSequence.ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
                    var prepareParameters = new PrepareImageParameters(autoStretch: true, detectStars: false);
                    var capture = imagingMediator.CaptureAndPrepareImage(seq, prepareParameters, cancelDARVSlewToken.Token, cameraprogress);
                    var slew = DarvTelescopeSlew(slewprogress, cancelDARVSlewToken.Token);

                    await Task.WhenAll(capture, slew);
                } catch (OperationCanceledException) {
                }
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblNoCameraConnected"]);
            }
            cameraprogress.Report(new ApplicationStatus() { Status = string.Empty });
            return true;
        }

        private void Canceldarvslew(object o) {
            cancelDARVSlewToken?.Cancel();
        }

        private void CancelMeasurePolarError(object o) {
            cancelMeasureErrorToken?.Cancel();
        }

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
                cancelMeasureErrorToken?.Dispose();
                cancelMeasureErrorToken = new CancellationTokenSource();
                Task moveBackTask = Task.CompletedTask;
                try {
                    var siderealTime = Astrometry.GetLocalSiderealTimeNow(profileService.ActiveProfile.AstrometrySettings.Longitude);
                    var latitude = Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude);
                    var dec = Angle.ByDegree(TelescopeInfo.Declination);
                    var hourAngle = Astrometry.GetHourAngle(Angle.ByHours(siderealTime), Angle.ByHours(TelescopeInfo.Coordinates.RA));
                    var altitude = Astrometry.GetAltitude(hourAngle, latitude, dec);
                    var azimuth = Astrometry.GetAzimuth(hourAngle, altitude, latitude, dec);
                    var altitudeSide = azimuth.Degree < 180 ? AltitudeSite.EAST : AltitudeSite.WEST;

                    Coordinates startPosition = telescopeMediator.GetCurrentPosition();
                    double poleErr = await CalculatePoleError(startPosition, progress, cancelMeasureErrorToken.Token);
                    moveBackTask = telescopeMediator.SlewToCoordinatesAsync(startPosition);

                    string poleErrString = Deg2str(Math.Abs(poleErr), 4);
                    cancelMeasureErrorToken.Token.ThrowIfCancellationRequested();
                    if (double.IsNaN(poleErr)) {
                        /* something went wrong */
                        progress.Report(new ApplicationStatus() { Status = string.Empty });
                        return false;
                    }

                    string msg = "";

                    if (direction == Direction.ALTITUDE) {
                        if (profileService.ActiveProfile.AstrometrySettings.HemisphereType == Hemisphere.NORTHERN) {
                            if (altitudeSide == AltitudeSite.EAST) {
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
                            if (altitudeSide == AltitudeSite.EAST) {
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
                } finally {
                    await moveBackTask;
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

        private async Task<double> CalculatePoleError(Coordinates startPosition, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);
            var solver = new CaptureSolver(plateSolver, blindSolver, imagingMediator);
            var parameter = new CaptureSolverParameter() {
                Attempts = 1,
                Binning = SnapBin?.X ?? CameraInfo.BinX,
                DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius
            };

            double poleError = double.NaN;
            try {
                double driftAmount = 0.5d;

                progress.Report(new ApplicationStatus() { Status = "Solving image..." });

                var seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
                seq.Gain = SnapGain;

                parameter.Coordinates = startPosition;
                PlateSolveResult = await solver.Solve(seq, parameter, default, progress, canceltoken);

                canceltoken.ThrowIfCancellationRequested();

                PlateSolveResult startSolveResult = PlateSolveResult;
                if (!startSolveResult.Success) {
                    return double.NaN;
                }

                Coordinates startSolve = PlateSolveResult.Coordinates.Transform(Epoch.JNOW);

                Coordinates targetPosition = new Coordinates(startPosition.RADegrees - driftAmount, startPosition.Dec, TelescopeInfo.EquatorialSystem, Coordinates.RAType.Degrees);
                progress.Report(new ApplicationStatus() { Status = "Slewing..." });
                await telescopeMediator.SlewToCoordinatesAsync(targetPosition);

                canceltoken.ThrowIfCancellationRequested();

                progress.Report(new ApplicationStatus() { Status = "Settling..." });
                await Task.Delay(3000);

                progress.Report(new ApplicationStatus() { Status = "Solving image..." });

                canceltoken.ThrowIfCancellationRequested();

                seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
                seq.Gain = SnapGain;

                parameter.Coordinates = telescopeMediator.GetCurrentPosition();
                PlateSolveResult = await solver.Solve(seq, parameter, default, progress, canceltoken);

                canceltoken.ThrowIfCancellationRequested();

                PlateSolveResult targetSolveResult = PlateSolveResult;
                if (!targetSolveResult.Success) {
                    return double.NaN;
                }

                Coordinates targetSolve = PlateSolveResult.Coordinates.Transform(Epoch.JNOW);

                var decError = startSolve.Dec - targetSolve.Dec;

                poleError = Astrometry.DetermineDriftAlignError(startSolve.Dec, driftAmount, decError);
            } catch (OperationCanceledException) {
            }

            return poleError;
        }

        public async Task<bool> SlewToMeridianOffset(double meridianOffset, double declination) {
            var lst = Astrometry.GetLocalSiderealTimeNow(profileService.ActiveProfile.AstrometrySettings.Longitude);

            double slew_ra = lst + (Astrometry.DegreesToHours(meridianOffset));
            if (slew_ra >= 24.0) {
                slew_ra -= 24.0;
            } else if (slew_ra < 0.0) {
                slew_ra += 24.0;
            }

            var coords = new Coordinates(slew_ra, declination, Epoch.JNOW, Coordinates.RAType.Hours);
            return await telescopeMediator.SlewToCoordinatesAsync(coords);
        }

        private const double polarisRA = 2.5303040444444442;
        private const double polarisDec = 89.264108972222218;

        private void UpdateValues_Tick(object sender, EventArgs e) {
            try {
                var polaris = new Coordinates(polarisRA, polarisDec, Epoch.J2000, Coordinates.RAType.Hours);
                polaris = polaris.Transform(Epoch.JNOW);

                var lst = Astrometry.GetLocalSiderealTimeNow(profileService.ActiveProfile.AstrometrySettings.Longitude);
                var hour_angle = Astrometry.GetHourAngle(lst, polaris.RA);

                Rotation = -Astrometry.HoursToDegrees(hour_angle);
                HourAngleTime = Astrometry.HoursToHMS(hour_angle);
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

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
            this.telescopeMediator.RemoveConsumer(this);
        }
    }
}