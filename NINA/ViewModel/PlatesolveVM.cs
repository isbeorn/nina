#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NINA.Model.ImageData;
using NINA.Utility.Mediator;
using NINA.ViewModel.PlateSolver;
using System.Collections.Generic;
using NINA.Utility.WindowService;
using System.Linq;
using System.Collections.Immutable;

namespace NINA.ViewModel {

    internal class PlatesolveVM : DockableVM, ICameraConsumer, ITelescopeConsumer {

        public PlatesolveVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator
        ) : base(profileService) {
            Title = "LblPlateSolving";
            SyncScope = false;
            SlewToTarget = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            SolveCommand = new AsyncCommand<bool>(() => CaptureSolveSyncAndReslew(new Progress<ApplicationStatus>(p => Status = p)));
            CancelSolveCommand = new RelayCommand(CancelSolve);

            SnapExposureDuration = profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
            SnapFilter = profileService.ActiveProfile.PlateSolveSettings.Filter;
            Repeat = false;
            RepeatThreshold = profileService.ActiveProfile.PlateSolveSettings.Threshold;

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                SnapExposureDuration = profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
                SnapFilter = profileService.ActiveProfile.PlateSolveSettings.Filter;
                RepeatThreshold = profileService.ActiveProfile.PlateSolveSettings.Threshold;
            };
        }

        private IWindowService windowService;

        public IWindowService WindowService {
            get {
                if (windowService == null) {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set {
                windowService = value;
            }
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

        private bool _syncScope;

        public bool SyncScope {
            get {
                return _syncScope;
            }
            set {
                _syncScope = value;
                if (!_syncScope && SlewToTarget) {
                    SlewToTarget = false;
                }
                RaisePropertyChanged();
            }
        }

        private bool _slewToTarget;

        public bool SlewToTarget {
            get {
                return _slewToTarget;
            }
            set {
                _slewToTarget = value;
                if (_slewToTarget && !SyncScope) {
                    SyncScope = true;
                }
                if (!_slewToTarget && Repeat) {
                    Repeat = false;
                }
                RaisePropertyChanged();
            }
        }

        private bool _repeat;

        public bool Repeat {
            get {
                return _repeat;
            }
            set {
                _repeat = value;
                if (_repeat && !SlewToTarget) {
                    SlewToTarget = true;
                }
                RaisePropertyChanged();
            }
        }

        private double _repeatThreshold;

        public double RepeatThreshold {
            get {
                return _repeatThreshold;
            }
            set {
                _repeatThreshold = value;
                RaisePropertyChanged();
            }
        }

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

        private void ImageChanged(Object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Image") {
                this.PlateSolveResult = null;
            }
        }

        /// <summary>
        /// Syncs telescope to solved coordinates
        /// </summary>
        /// <returns></returns>
        private bool SynchronizeTelescope() {
            var success = false;

            if (TelescopeInfo.Connected != true) {
                Notification.ShowWarning(Locale.Loc.Instance["LblUnableToSync"]);
                Logger.Warning("Telescope is not connected. Unable to sync");
                return false;
            }

            if (PlateSolveResult != null && PlateSolveResult.Success) {
                Coordinates solved = PlateSolveResult.Coordinates;
                solved = solved.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);  //Transform to JNow if required
                Logger.Trace($"Trying to sync to coordinates - RA: {solved.RAString} Dec: {solved.DecString}");
                if (telescopeMediator.Sync(solved.RA, solved.Dec) == true) {
                    Notification.ShowSuccess(Locale.Loc.Instance["LblTelescopeSynced"]);
                    success = true;
                } else {
                    Logger.Warning("Sync to coordinates failed");
                    Notification.ShowWarning(Locale.Loc.Instance["LblSyncFailed"]);
                }
            } else {
                Logger.Warning("No coordinates available to sync to");
                Notification.ShowWarning(Locale.Loc.Instance["LblNoCoordinatesForSync"]);
            }
            return success;
        }

        private BitmapSource _image;

        public BitmapSource Image {
            get => _image;
            set {
                _image = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Thumbnail));
            }
        }

        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        public BitmapSource Thumbnail {
            get {
                BitmapSource scaledBitmap = null;
                if (Image != null) {
                    dispatcher.Invoke(() => {
                        var factor = 300 / _image.Width;
                        scaledBitmap = new WriteableBitmap(new TransformedBitmap(_image, new ScaleTransform(factor, factor)));
                        scaledBitmap.Freeze();
                    }, DispatcherPriority.Background);
                }
                return scaledBitmap;
            }
        }

        /// <summary>
        /// Captures an image and solves it
        /// </summary>
        /// <param name="duration">   </param>
        /// <param name="progress">   </param>
        /// <param name="canceltoken"></param>
        /// <param name="filter">     </param>
        /// <param name="binning">    </param>
        /// <returns></returns>
        public async Task<PlateSolveResult> SolveWithCapture(CaptureSequence seq, IProgress<ApplicationStatus> progress, CancellationToken canceltoken, bool silent = false) {
            var renderedImage = await imagingMediator.CaptureAndPrepareImage(seq, new PrepareImageParameters(), canceltoken, progress);
            Image = renderedImage.Image;

            canceltoken.ThrowIfCancellationRequested();

            var success = await Solve(renderedImage.RawImageData, progress, canceltoken, silent);
            Image = null;
            return success;
        }

        private async Task<bool> CaptureSolveSyncAndReslew(IProgress<ApplicationStatus> progress) {
            _solveCancelToken?.Dispose();
            _solveCancelToken = new CancellationTokenSource();
            var seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
            seq.Gain = SnapGain;
            return await this.CaptureSolveSyncAndReslew(seq, this.SyncScope, this.SlewToTarget, this.Repeat, _solveCancelToken.Token, progress, false, this.RepeatThreshold) != null;
        }

        public async Task<PlateSolveResult> CaptureSolveSyncReslewReattempt(
                SolveParameters solveParameters,
                CancellationToken token,
                IProgress<ApplicationStatus> progress) {
            bool repeatAll = false;
            int currentAttempt = 0;
            PlateSolveResult plateSolveResult = null;

            do {
                currentAttempt += 1;
                var solveseq = new CaptureSequence() {
                    ExposureTime = profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                    FilterType = profileService.ActiveProfile.PlateSolveSettings.Filter,
                    ImageType = CaptureSequence.ImageTypes.SNAPSHOT,
                    TotalExposureCount = 1
                };

                plateSolveResult = await CaptureSolveSyncAndReslew(solveseq, solveParameters.syncScope, solveParameters.slewToTarget, solveParameters.repeat, token, progress, solveParameters.silent, solveParameters.repeatThreshold);

                if (plateSolveResult == null || !plateSolveResult.Success) {
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlatesolveFailed"] });
                    if (currentAttempt < solveParameters.numberOfAttempts && !MeridianFlipVM.ShouldFlip(profileService, solveParameters.delayDuration.TotalSeconds, telescopeInfo)) {
                        repeatAll = true;
                        var delay = solveParameters.delayDuration.TotalSeconds;
                        while (delay > 0) {
                            await Task.Delay(TimeSpan.FromSeconds(1), token);
                            delay--;
                            progress.Report(new ApplicationStatus() { Status = string.Format(Locale.Loc.Instance["LblPlateSolveReattemptDelay"], delay) });
                        }
                    } else {
                        repeatAll = false;
                        Notification.ShowWarning(Locale.Loc.Instance["LblPlateSolveEnding"]);
                        Logger.Warning("Platesolve attempts exhausted, or Meridian Flip approaching. Aborting plate solve.");
                    }
                } else {
                    repeatAll = false;
                    Logger.Trace("Successful plate solve, no more reattempts needed");
                }
            } while (repeatAll);
            return plateSolveResult;
        }

        /// <summary>
        /// Calls "SolveWithCaputre" and syncs + reslews afterwards if set
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task<PlateSolveResult> CaptureSolveSyncAndReslew(
                CaptureSequence seq,
                bool syncScope,
                bool slewToTarget,
                bool repeat,
                CancellationToken token,
                IProgress<ApplicationStatus> progress,
                bool silent = false,
                double repeatThreshold = 1.0d) {
            PlateSolveResult solveresult = null;
            bool repeatPlateSolve = false;
            do {
                solveresult = await SolveWithCapture(seq, progress, token, silent);

                if (solveresult != null && solveresult.Success) {
                    Logger.Trace($"Solved successfully. Current Coordinates RA: {solveresult.Coordinates.RAString} Dec: {solveresult.Coordinates.DecString} Epoch: {solveresult.Coordinates.Epoch}");
                    if (syncScope) {
                        if (TelescopeInfo.Connected != true) {
                            Logger.Warning("Telescope not connected. Unable to sync");
                            Notification.ShowWarning(Locale.Loc.Instance["LblUnableToSync"]);
                            return null;
                        }

                        Coordinates coords = PlateSolveTarget;
                        if (coords == null) {
                            coords = new Coordinates(TelescopeInfo.RightAscension, TelescopeInfo.Declination, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);
                        }

                        var syncSuccess = SynchronizeTelescope();

                        if (syncSuccess && slewToTarget) {
                            if (!repeat || (repeat && Math.Abs(solveresult.Separation.Distance.ArcMinutes) > repeatThreshold)) {
                                Logger.Trace($"Slewing to target after sync. Target coordinates RA: {coords.RAString} Dec: {coords.DecString} Epoch: {coords.Epoch}");
                                await telescopeMediator.SlewToCoordinatesAsync(coords);
                            }
                        }
                    }
                }

                if (solveresult?.Success == true && repeat && Math.Abs(solveresult.Separation.Distance.ArcMinutes) > repeatThreshold) {
                    repeatPlateSolve = true;
                    Logger.Trace($"Telescope not inside tolerance. Tolerance: {repeatThreshold}; Error Distance: {Math.Abs(solveresult.Separation.Distance.ArcMinutes)} - Repeating...");
                    progress.Report(new ApplicationStatus() { Status = "Telescope not inside tolerance. Repeating..." });
                    //Let the scope settle
                    await Task.Delay(TimeSpan.FromSeconds(profileService.ActiveProfile.TelescopeSettings.SettleTime));
                } else {
                    repeatPlateSolve = false;
                }
            } while (repeatPlateSolve);

            RaiseAllPropertiesChanged();
            return solveresult;
        }

        /// <summary>
        /// Calculates the error based on the solved coordinates and the actual telescope coordinates
        /// and puts them into the PlateSolveResult
        /// </summary>
        private void CalculateError() {
            if (TelescopeInfo.Connected == true) {
                Coordinates solved = PlateSolveResult.Coordinates;

                var coords = PlateSolveTarget;
                if (coords == null) {
                    coords = new Coordinates(TelescopeInfo.RightAscension, TelescopeInfo.Declination, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);
                }

                PlateSolveResult.Separation = coords - solved;
            }
        }

        /// <summary>
        /// Validates general prerequisites that need to be set up to use the plate solvers
        /// </summary>
        private void ValidatePrerequisites() {
            double focalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength;

            // Check to make sure user has supplied the telescope's effective focal length (in mm)
            if (double.IsNaN(focalLength) || focalLength <= 0) {
                throw new Exception(Locale.Loc.Instance["LblPlateSolveNoFocalLength"]);
            }
        }

        /// <summary>
        /// Creates an instance of IPlatesolver, reads the image into memory and calls solve logic of platesolver
        /// </summary>
        /// <param name="source"></param>
        /// <param name="progress"></param>
        /// <param name="canceltoken"></param>
        /// <returns>true: success; false: fail</returns>
        public Task<PlateSolveResult> Solve(
            IImageData source,
            IProgress<ApplicationStatus> progress,
            CancellationToken canceltoken,
            bool silent = false,
            Coordinates coordinates = null) {
            return ValidateAndSolve(source, progress, canceltoken, silent, coordinates);
        }

        private async Task<PlateSolveResult> ValidateAndSolve(
            IImageData source,
            IProgress<ApplicationStatus> progress,
            CancellationToken canceltoken,
            bool silent,
            Coordinates coordinates) {
            try {
                ValidatePrerequisites();

                if (PlateSolveTarget != null) {
                    coordinates = PlateSolveTarget;
                } else {
                    coordinates = coordinates ?? TelescopeInfo.Coordinates;
                }

                var plateSolveParameter = PlateSolveParameter.FromImageData(source, this.profileService, coordinates);

                PlateSolveResult result = new PlateSolveResult() { Success = false };
                string failedMessage = Locale.Loc.Instance["LblPlatesolveFailed"];
                string failedTitleMessage = Locale.Loc.Instance["LblUseBlindSolveFailover"];

                /*
                 * Attempt a plate solve only if both the mount coordinates and optical focal length are available.
                 * If either one of those parameters are not specified, Success remains false and we then will fail the attempt
                 * completely (in the case of no focal length), or in the case of no mount coordinates, prompt the user to attempt a blind solve.
                 */
                if (coordinates != null) {
                    result = await SolveImpl(source, plateSolveParameter, progress, canceltoken, silent);
                } else {
                    failedMessage = Locale.Loc.Instance["LblPlatesolveNoCoordinates"];
                    failedTitleMessage = Locale.Loc.Instance["LblUseBlindSolveNoCoordinatesRollover"];
                }

                if (!result?.Success == true) {
                    MessageBoxResult dialog = MessageBoxResult.Yes;
                    if (!silent) {
                        dialog = MyMessageBox.MyMessageBox.Show(failedTitleMessage, failedMessage, MessageBoxButton.YesNo, MessageBoxResult.Yes);
                    }
                    if (dialog == MessageBoxResult.Yes) {
                        result = await BlindSolveImpl(source, plateSolveParameter, progress, canceltoken, silent);
                        if (!result?.Success == true) {
                            Notification.ShowError(Locale.Loc.Instance["LblPlatesolveFailed"]);
                        }
                    }
                }

                PlateSolveResult = result;
                progress.Report(new ApplicationStatus() { Status = string.Empty });
                return result;
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Performs a PlateSolve using raw ImageData, which the solver may prepare and render as it requires
        /// </summary>
        /// <param name="source"></param>
        /// <param name="progress"></param>
        /// <param name="canceltoken"></param>
        public Task<PlateSolveResult> BlindSolve(
            IImageData source,
            IProgress<ApplicationStatus> progress,
            CancellationToken cancelToken) {
            var parameter = PlateSolveParameter.FromImageData(source, this.profileService);
            return BlindSolveImpl(source, parameter, progress, cancelToken, silent: false);
        }

        private async Task<PlateSolveResult> BlindSolveImpl(
            IImageData source,
            PlateSolveParameter parameter,
            IProgress<ApplicationStatus> progress,
            CancellationToken cancelToken,
            bool silent) {
            var solver = GetBlindSolver();
            await ValidateAndUpdatePlateSolveParameter(solver, parameter, silent);

            Logger.Trace($"Blind solving with parameters: {Environment.NewLine + parameter.ToString()}");

            var result = await solver.SolveAsync(source, parameter, progress, cancelToken);
            progress.Report(new ApplicationStatus() { Status = string.Empty });
            return result;
        }

        private async Task ValidateAndUpdatePlateSolveParameter(IPlateSolver solver, PlateSolveParameter parameter, bool silent) {
            var missingProperties = solver.GetMissingProperties(parameter);
            if (missingProperties.Count > 0) {
                if (!silent) {
                    var missingParametersPrompt = new PlateSolverMissingParamsPromptVM(missingProperties);
                    await WindowService.ShowDialog(
                        missingParametersPrompt,
                        Locale.Loc.Instance["LblPlateSolveSetMissingProperties"],
                        ResizeMode.NoResize,
                        WindowStyle.ToolWindow);
                    if (missingParametersPrompt.Continue) {
                        var propertyValueMap = ImmutableDictionary.CreateRange(
                            missingParametersPrompt.Parameters
                                .Select(p => new KeyValuePair<string, double?>(p.Property, p.Value)));
                        parameter.Update(propertyValueMap);
                    }
                }
            }
        }

        private async Task<PlateSolveResult> SolveImpl(
            IImageData source,
            PlateSolveParameter parameter,
            IProgress<ApplicationStatus> progress,
            CancellationToken canceltoken,
            bool silent) {
            var solver = GetPlateSolver();
            await ValidateAndUpdatePlateSolveParameter(solver, parameter, silent);

            Logger.Trace($"Solving with parameters: {Environment.NewLine + parameter.ToString()}");

            var result = await solver.SolveAsync(source, parameter, progress, canceltoken);
            progress.Report(new ApplicationStatus() { Status = string.Empty });
            return result;
        }

        private CancellationTokenSource _solveCancelToken;

        private void CancelSolve(object o) {
            _solveCancelToken?.Cancel();
        }

        private IPlateSolver GetPlateSolver() {
            return PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
        }

        private IPlateSolver GetBlindSolver() {
            return PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);
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

        private ICameraMediator cameraMediator;
        private ITelescopeMediator telescopeMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public IAsyncCommand SolveCommand { get; private set; }

        public ICommand CancelSolveCommand { get; private set; }

        private AsyncObservableLimitedSizedStack<PlateSolveResult> _plateSolveResultList;

        public AsyncObservableLimitedSizedStack<PlateSolveResult> PlateSolveResultList {
            get {
                if (_plateSolveResultList == null) {
                    _plateSolveResultList = new AsyncObservableLimitedSizedStack<PlateSolveResult>(15);
                }
                return _plateSolveResultList;
            }
            set {
                _plateSolveResultList = value;
                RaisePropertyChanged();
            }
        }

        private PlateSolveResult _plateSolveResult;
        private CameraInfo cameraInfo;
        private TelescopeInfo telescopeInfo;

        public TelescopeInfo TelescopeInfo {
            get {
                return telescopeInfo ?? DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
            }
            private set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        public CameraInfo CameraInfo {
            get {
                return cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            }
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolveResult PlateSolveResult {
            get {
                return _plateSolveResult;
            }

            set {
                _plateSolveResult = value;

                if (_plateSolveResult.Success) {
                    CalculateError();
                }
                PlateSolveResultList.Add(_plateSolveResult);

                RaisePropertyChanged();
            }
        }

        public Coordinates PlateSolveTarget { get; set; }

        public struct SolveParameters {
            public bool syncScope;
            public bool slewToTarget;
            public bool repeat;
            public bool silent;
            public double repeatThreshold;
            public int numberOfAttempts;
            public TimeSpan delayDuration;
        }
    }
}