using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class SequenceVM : DockableVM, ITelescopeConsumer, IFocuserConsumer, IFilterWheelConsumer, IRotatorConsumer {

        public SequenceVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IFocuserMediator focuserMediator,
                IFilterWheelMediator filterWheelMediator,
                IGuiderMediator guiderMediator,
                IRotatorMediator rotatorMediator,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator
        ) : base(profileService) {
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);

            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);

            this.rotatorMediator = rotatorMediator;
            this.rotatorMediator.RegisterConsumer(this);

            this.guiderMediator = guiderMediator;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            this.profileService = profileService;
            Title = "LblSequence";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current?.Resources["SequenceSVG"];

            AddSequenceRowCommand = new RelayCommand(AddSequenceRow);
            AddTargetCommand = new RelayCommand(AddTarget);
            RemoveTargetCommand = new RelayCommand(RemoveTarget, (object o) => this.Targets.Count > 1);
            RemoveSequenceRowCommand = new RelayCommand(RemoveSequenceRow);
            StartSequenceCommand = new AsyncCommand<bool>(() => StartSequencing(new Progress<ApplicationStatus>(p => Status = p)));
            SaveSequenceCommand = new RelayCommand(SaveSequence);
            LoadSequenceCommand = new RelayCommand(LoadSequence);
            CancelSequenceCommand = new RelayCommand(CancelSequence);
            PauseSequenceCommand = new RelayCommand(PauseSequence, (object o) => !_pauseTokenSource?.IsPaused == true);
            ResumeSequenceCommand = new RelayCommand(ResumeSequence);
            UpdateETACommand = new RelayCommand((object o) => CalculateETA());

            profileService.LocationChanged += (object sender, EventArgs e) => {
                foreach (var seq in this.Targets) {
                    var dso = new DeepSkyObject(seq.DSO.Name, seq.DSO.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                    dso.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                    seq.SetSequenceTarget(dso);
                }
            };
        }

        private void RemoveTarget(object obj) {
            if (this.Targets.Count > 1) {
                var l = (CaptureSequenceList)obj;
                var switchTab = false;
                if (Object.Equals(l, Sequence)) {
                    switchTab = true;
                }
                this.Targets.Remove(l);
                if (switchTab) {
                    Sequence = this.Targets.First();
                }
            }
        }

        private void AddTarget(object obj) {
            this.Targets.Add(GetTemplate());
        }

        private void LoadSequence(object obj) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Title = Locale.Loc.Instance["LblLoadSequence"];
            dialog.FileName = "Target";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";

            if (dialog.ShowDialog() == true) {
                foreach (var fileName in dialog.FileNames) {
                    using (var s = new FileStream(fileName, FileMode.Open)) {
                        var l = CaptureSequenceList.Load(
                            s,
                            profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                            profileService.ActiveProfile.AstrometrySettings.Latitude,
                            profileService.ActiveProfile.AstrometrySettings.Longitude
                        );
                        this.Targets.Add(l);
                    }
                }
            }
        }

        private void SaveSequence(object obj) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = Locale.Loc.Instance["LblSaveSequence"];

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var path = dialog.SelectedPath;

                foreach (var target in Targets) {
                    var filePath = Utility.Utility.GetUniqueFilePath(Path.Combine(path, target.TargetName + ".xml"));
                    target.Save(filePath);
                }
            }
        }

        private void ResumeSequence(object obj) {
            if (_pauseTokenSource != null) {
                _pauseTokenSource.IsPaused = false;
            }
        }

        private void PauseSequence(object obj) {
            if (_pauseTokenSource != null) {
                _pauseTokenSource.IsPaused = true;
            }
        }

        private void CancelSequence(object obj) {
            _canceltoken?.Cancel();
            RaisePropertyChanged(nameof(IsPaused));
        }

        private PauseTokenSource _pauseTokenSource;
        private CancellationTokenSource _canceltoken;

        private bool isPaused;

        public bool IsPaused {
            get {
                return isPaused;
            }
            set {
                isPaused = value;
                RaisePropertyChanged();
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

                if (Sequence.ActiveSequence != null) {
                    _status.Status2 = Locale.Loc.Instance["LblSequence"];
                    _status.ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue;
                    _status.Progress2 = Sequence.ActiveSequenceIndex;
                    _status.MaxProgress2 = Sequence.Count;

                    _status.Status3 = Locale.Loc.Instance["LblExposures"];
                    _status.ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue;
                    _status.Progress3 = Sequence.ActiveSequence.ProgressExposureCount;
                    _status.MaxProgress3 = Sequence.ActiveSequence.TotalExposureCount;
                }

                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public void AddDownloadTime(TimeSpan t) {
            _actualDownloadTimes.Add(t);
            double doubleAverageTicks = _actualDownloadTimes.Average(timeSpan => timeSpan.Ticks);
            long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
            EstimatedDownloadTime = new TimeSpan(longAverageTicks);
        }

        private void CalculateETA() {
            TimeSpan time = new TimeSpan();
            foreach (var seq in Targets) {
                foreach (CaptureSequence cs in seq) {
                    var exposureCount = cs.TotalExposureCount - cs.ProgressExposureCount;
                    time = time.Add(TimeSpan.FromSeconds(exposureCount * (cs.ExposureTime + EstimatedDownloadTime.TotalSeconds)));
                }
            }

            ETA = DateTime.Now.AddSeconds(time.TotalSeconds);
        }

        private List<TimeSpan> _actualDownloadTimes = new List<TimeSpan>();

        public TimeSpan EstimatedDownloadTime {
            get {
                return profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime;
            }
            set {
                profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime = value;
                RaisePropertyChanged();
                CalculateETA();
            }
        }

        private DateTime _eta;

        public DateTime ETA {
            get {
                return _eta;
            }
            private set {
                _eta = value;
                RaisePropertyChanged();
            }
        }

        private IWindowServiceFactory windowServiceFactory;

        public IWindowServiceFactory WindowServiceFactory {
            get {
                if (windowServiceFactory == null) {
                    windowServiceFactory = new WindowServiceFactory();
                }
                return windowServiceFactory;
            }
            set {
                windowServiceFactory = value;
            }
        }

        private async Task RotateEquipment(CaptureSequenceList csl, PlateSolveResult plateSolveResult, IProgress<ApplicationStatus> progress) {
            // Rotate to desired angle
            if (rotatorInfo?.Connected == true) {
                var solver = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
                var solveseq = new CaptureSequence() {
                    ExposureTime = profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                    FilterType = profileService.ActiveProfile.PlateSolveSettings.Filter,
                    ImageType = CaptureSequence.ImageTypes.SNAP,
                    TotalExposureCount = 1
                };
                var service = WindowServiceFactory.Create();

                service.Show(solver, this.Title + " - " + solver.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);

                var orientation = (float)(plateSolveResult?.Orientation ?? 0.0f);
                float position = 0.0f;
                do {
                    if (plateSolveResult == null) {
                        plateSolveResult = await solver.SolveWithCapture(solveseq, progress, _canceltoken.Token);
                    }

                    if (!plateSolveResult.Success) {
                        break;
                    }

                    orientation = (float)plateSolveResult.Orientation;

                    position = (float)((float)csl.DSO.Rotation - orientation);
                    if (Math.Abs(position) > profileService.ActiveProfile.PlateSolveSettings.RotationTolerance) {
                        await rotatorMediator.MoveRelative(position);
                    }
                    plateSolveResult = null;
                } while (Math.Abs(position) > profileService.ActiveProfile.PlateSolveSettings.RotationTolerance);
                service.DelayedClose(TimeSpan.FromSeconds(10));
            }
        }

        private async Task<PlateSolveResult> SlewToTarget(CaptureSequenceList csl, IProgress<ApplicationStatus> progress) {
            PlateSolveResult plateSolveResult = null;
            if (csl.SlewToTarget) {
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSlewToTarget"] });
                await telescopeMediator.SlewToCoordinatesAsync(csl.Coordinates);
                plateSolveResult = await CenterTarget(csl, progress);
            }
            return plateSolveResult;
        }

        private async Task<PlateSolveResult> CenterTarget(CaptureSequenceList csl, IProgress<ApplicationStatus> progress) {
            PlateSolveResult plateSolveResult = null;
            if (csl.CenterTarget) {
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblCenterTarget"] });

                var solver = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
                var solveseq = new CaptureSequence() {
                    ExposureTime = profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                    FilterType = profileService.ActiveProfile.PlateSolveSettings.Filter,
                    ImageType = CaptureSequence.ImageTypes.SNAP,
                    TotalExposureCount = 1
                };
                var service = WindowServiceFactory.Create();
                service.Show(solver, this.Title + " - " + solver.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
                plateSolveResult = await solver.CaptureSolveSyncAndReslew(solveseq, true, true, true, _canceltoken.Token, progress, false, profileService.ActiveProfile.PlateSolveSettings.Threshold);
                service.DelayedClose(TimeSpan.FromSeconds(10));

                if (plateSolveResult == null || !plateSolveResult.Success) {
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlatesolveFailed"] });
                }
            }
            return plateSolveResult;
        }

        private async Task DelaySequence(CaptureSequenceList csl, IProgress<ApplicationStatus> progress) {
            var delay = csl.Delay;
            while (delay > 0) {
                await Task.Delay(TimeSpan.FromSeconds(1), _canceltoken.Token);
                delay--;
                progress.Report(new ApplicationStatus() { Status = string.Format(Locale.Loc.Instance["LblSequenceDelayStatus"], delay) });
            }
        }

        private async Task AutoFocusOnStart(CaptureSequenceList csl, IProgress<ApplicationStatus> progress) {
            if (csl.AutoFocusOnStart) {
                await AutoFocus(csl.Items[0].FilterType, _canceltoken.Token, progress);
            }
        }

        private async Task StartGuiding(CaptureSequenceList csl, IProgress<ApplicationStatus> progress) {
            if (csl.StartGuiding) {
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStartGuiding"] });
                var guiderStarted = await this.guiderMediator.StartGuiding(_canceltoken.Token);
                if (!guiderStarted) {
                    Notification.ShowWarning(Locale.Loc.Instance["LblStartGuidingFailed"]);
                }
            }
        }

        private bool _isRunning;

        public bool IsRunning {
            get {
                return _isRunning;
            }
            set {
                _isRunning = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> StartSequencing(IProgress<ApplicationStatus> progress) {
            try {
                IsPaused = false;
                IsRunning = true;
                foreach (CaptureSequenceList csl in this.Targets) {
                    try {
                        csl.IsFinished = false;
                        csl.IsRunning = true;
                        Sequence = csl;
                        await StartSequence(csl, progress);
                    } finally {
                        csl.IsRunning = false;
                        csl.IsFinished = true;
                    }
                }
                return true;
            } finally {
                IsPaused = false;
                IsRunning = false;
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
        }

        private async Task<bool> StartSequence(CaptureSequenceList csl, IProgress<ApplicationStatus> progress) {
            try {
                if (csl.Count <= 0) {
                    return false;
                }
                _actualDownloadTimes.Clear();
                _canceltoken = new CancellationTokenSource();
                _pauseTokenSource = new PauseTokenSource();
                RaisePropertyChanged(nameof(IsPaused));

                CalculateETA();

                /* delay sequence start by given amount */
                await DelaySequence(csl, progress);

                //Slew and center
                PlateSolveResult plateSolveResult = await SlewToTarget(csl, progress);

                //Rotate for framing
                await RotateEquipment(csl, plateSolveResult, progress);

                await AutoFocusOnStart(csl, progress);

                await StartGuiding(csl, progress);

                return await ProcessSequence(csl, _canceltoken.Token, _pauseTokenSource.Token, progress);
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
        }

        private async Task AutoFocus(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            var autoFocus = new AutoFocusVM(profileService, focuserMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var service = WindowServiceFactory.Create();
            service.Show(autoFocus, this.Title + " - " + autoFocus.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
            await autoFocus.StartAutoFocus(filter, _canceltoken.Token, progress);
            service.DelayedClose(TimeSpan.FromSeconds(10));
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private async Task<bool> ProcessSequence(CaptureSequenceList csl, CancellationToken ct, PauseToken pt, IProgress<ApplicationStatus> progress) {
            return await Task.Run<bool>(async () => {
                try {
                    //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released
                    await semaphoreSlim.WaitAsync(ct);

                    /* Validate if preconditions are met */
                    if (!CheckPreconditions()) {
                        return false;
                    }

                    csl.IsRunning = true;

                    CaptureSequence seq;
                    var actualFilter = filterWheelInfo?.SelectedFilter;
                    short prevFilterPosition = actualFilter?.Position ?? -1;
                    var lastAutoFocusTime = DateTime.UtcNow;
                    var lastAutoFocusTemperature = focuserInfo?.Temperature ?? double.NaN;
                    var exposureCount = 0;
                    while ((seq = csl.Next()) != null) {
                        exposureCount++;

                        await CheckMeridianFlip(seq, ct, progress);

                        Stopwatch seqDuration = Stopwatch.StartNew();

                        //Check if autofocus should be done
                        if (ShouldAutoFocus(csl, seq, exposureCount, prevFilterPosition, lastAutoFocusTime, lastAutoFocusTemperature)) {
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblAutoFocus"] });
                            await AutoFocus(seq.FilterType, _canceltoken.Token, progress);
                            lastAutoFocusTime = DateTime.UtcNow;
                            lastAutoFocusTemperature = focuserInfo?.Temperature ?? double.NaN;
                            progress.Report(new ApplicationStatus() { Status = string.Empty });
                        }

                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPrepareExposure"] });

                        await imagingMediator.CaptureAndSaveImage(seq, true, ct, progress, csl.TargetName);

                        progress.Report(new ApplicationStatus() { Status = string.Empty });

                        seqDuration.Stop();

                        AddDownloadTime(seqDuration.Elapsed.Subtract(TimeSpan.FromSeconds(seq.ExposureTime)));

                        if (pt.IsPaused) {
                            csl.IsRunning = false;
                            IsRunning = false;
                            IsPaused = true;
                            semaphoreSlim.Release();
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPaused"] });
                            await pt.WaitWhilePausedAsync(ct);
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblResuming"] });
                            await semaphoreSlim.WaitAsync(ct);
                            Sequence = csl;
                            IsPaused = false;
                            csl.IsRunning = true;
                            IsRunning = true;
                        }

                        actualFilter = filterWheelInfo?.SelectedFilter;
                        prevFilterPosition = actualFilter?.Position ?? -1;
                    }
                } catch (OperationCanceledException) {
                } catch (CameraConnectionLostException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                } finally {
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                    csl.IsRunning = false;
                    semaphoreSlim.Release();
                }
                return true;
            });
        }

        private bool ShouldAutoFocus(CaptureSequenceList csl, CaptureSequence seq, int exposureCount, short previousFilterPosition, DateTime lastAutoFocusTime, double lastAutoFocusTemperature) {
            if (seq.FilterType != null && seq.FilterType.Position != previousFilterPosition
                    && seq.FilterType.Position >= 0
                    && csl.AutoFocusOnFilterChange) {
                /* Trigger autofocus after filter change */
                return true;
            }

            if (csl.AutoFocusAfterSetTime && (DateTime.UtcNow - lastAutoFocusTime) > TimeSpan.FromMinutes(csl.AutoFocusSetTime)) {
                /* Trigger autofocus after a set time */
                return true;
            }

            if (csl.AutoFocusAfterSetExposures && exposureCount % csl.AutoFocusSetExposures == 0) {
                /* Trigger autofocus after amount of exposures*/
                return true;
            }

            if (csl.AutoFocusAfterTemperatureChange && !double.IsNaN(focuserInfo?.Temperature ?? double.NaN)
                && Math.Abs(lastAutoFocusTemperature - focuserInfo.Temperature) > csl.AutoFocusAfterTemperatureChangeAmount) {
                /* Trigger autofocus after temperature change*/
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if auto meridian flip should be considered and executes it
        /// 1) Compare next exposure length with time to meridian - If exposure length is greater
        ///    than time to flip the system will wait
        /// 2) Pause Guider
        /// 3) Execute the flip
        /// 4) If recentering is enabled, platesolve current position, sync and recenter to old
        ///    target position
        /// 5) Resume Guider
        /// </summary>
        /// <param name="seq">        Current Sequence row</param>
        /// <param name="tokenSource">cancel token</param>
        /// <param name="progress">   progress reporter</param>
        /// <returns></returns>
        private async Task CheckMeridianFlip(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblCheckMeridianFlip"] });
            if (telescopeInfo != null && MeridianFlipVM.ShouldFlip(profileService, seq.ExposureTime, telescopeInfo)) {
                await new MeridianFlipVM(profileService, cameraMediator, telescopeMediator, guiderMediator, imagingMediator, applicationStatusMediator).MeridianFlip(seq, telescopeInfo);
            }
            progress.Report(new ApplicationStatus() { Status = string.Empty });
        }

        private bool CheckPreconditions() {
            bool valid = true;

            valid = HasWritePermission(profileService.ActiveProfile.ImageFileSettings.FilePath);

            return valid;
        }

        public bool HasWritePermission(string dir) {
            bool Allow = false;
            bool Deny = false;
            DirectorySecurity acl = null;

            if (Directory.Exists(dir)) {
                acl = Directory.GetAccessControl(dir);
            }

            if (acl == null) {
                Notification.ShowError(Locale.Loc.Instance["LblDirectoryNotFound"]);
                return false;
            }

            AuthorizationRuleCollection arc = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
            if (arc == null)
                return false;
            foreach (FileSystemAccessRule rule in arc) {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                    continue;
                if (rule.AccessControlType == AccessControlType.Allow)
                    Allow = true;
                else if (rule.AccessControlType == AccessControlType.Deny)
                    Deny = true;
            }

            if (Allow && !Deny) {
                return true;
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblDirectoryNotWritable"]);
                return false;
            }
        }

        public async Task<bool> SetSequenceCoordiantes(DeepSkyObject dso) {
            var sequenceDso = new DeepSkyObject(dso.AlsoKnownAs.FirstOrDefault() ?? dso.Name ?? string.Empty, dso.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            sequenceDso.Rotation = dso.Rotation;
            await Task.Run(() => {
                sequenceDso.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
            });

            Sequence.SetSequenceTarget(sequenceDso);
            return true;
        }

        private AsyncObservableCollection<CaptureSequenceList> targets;

        public AsyncObservableCollection<CaptureSequenceList> Targets {
            get {
                if (targets == null) {
                    targets = new AsyncObservableCollection<CaptureSequenceList>();
                    targets.Add(Sequence);
                }
                return targets;
            }
            set {
                targets = value;
                RaisePropertyChanged();
            }
        }

        private CaptureSequenceList GetTemplate() {
            CaptureSequenceList csl = null;
            if (File.Exists(profileService.ActiveProfile.SequenceSettings.TemplatePath)) {
                using (var s = new FileStream(profileService.ActiveProfile.SequenceSettings.TemplatePath, FileMode.Open)) {
                    csl = CaptureSequenceList.Load(
                        s,
                        profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                        profileService.ActiveProfile.AstrometrySettings.Latitude,
                        profileService.ActiveProfile.AstrometrySettings.Longitude
                    );
                }
            } else {
                var seq = new CaptureSequence();
                csl = new CaptureSequenceList(seq) { TargetName = "Target" };
                csl.DSO?.SetDateAndPosition(
                    SkyAtlasVM.GetReferenceDate(DateTime.Now),
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );
            }
            return csl;
        }

        private CaptureSequenceList _sequence;

        public CaptureSequenceList Sequence {
            get {
                if (_sequence == null) {
                    _sequence = GetTemplate();
                    SelectedSequenceRowIdx = _sequence.Count - 1;
                }
                return _sequence;
            }
            set {
                _sequence = value;
                RaisePropertyChanged();
            }
        }

        private int _selectedSequenceIdx;

        public int SelectedSequenceRowIdx {
            get {
                return _selectedSequenceIdx;
            }
            set {
                _selectedSequenceIdx = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _imageTypes;
        private ITelescopeMediator telescopeMediator;
        private IFilterWheelMediator filterWheelMediator;
        private FocuserInfo focuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
        private FilterWheelInfo filterWheelInfo = DeviceInfo.CreateDefaultInstance<FilterWheelInfo>();
        private IFocuserMediator focuserMediator;
        private IGuiderMediator guiderMediator;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
        private IRotatorMediator rotatorMediator;
        private RotatorInfo rotatorInfo;

        public ObservableCollection<string> ImageTypes {
            get {
                if (_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    Type type = typeof(Model.CaptureSequence.ImageTypes);
                    foreach (var p in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                        var v = p.GetValue(null);
                        _imageTypes.Add(v.ToString());
                    }
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }

        public void AddSequenceRow(object o) {
            Sequence.Add(new CaptureSequence());
            SelectedSequenceRowIdx = Sequence.Count - 1;
        }

        private void RemoveSequenceRow(object obj) {
            var idx = SelectedSequenceRowIdx;
            if (idx > -1) {
                Sequence.RemoveAt(idx);
                if (idx < Sequence.Count - 1) {
                    SelectedSequenceRowIdx = idx;
                } else {
                    SelectedSequenceRowIdx = Sequence.Count - 1;
                }
            }
        }

        public void UpdateDeviceInfo(FocuserInfo focuserInfo) {
            this.focuserInfo = focuserInfo;
        }

        public void UpdateDeviceInfo(FilterWheelInfo filterWheelInfo) {
            this.filterWheelInfo = filterWheelInfo;
        }

        public void UpdateDeviceInfo(TelescopeInfo telescopeInfo) {
            this.telescopeInfo = telescopeInfo;
        }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            this.rotatorInfo = deviceInfo;
        }

        public ICommand AddSequenceRowCommand { get; private set; }
        public ICommand AddTargetCommand { get; private set; }
        public ICommand RemoveTargetCommand { get; private set; }
        public ICommand RemoveSequenceRowCommand { get; private set; }

        public IAsyncCommand StartSequenceCommand { get; private set; }

        public ICommand CancelSequenceCommand { get; private set; }
        public ICommand PauseSequenceCommand { get; private set; }
        public ICommand ResumeSequenceCommand { get; private set; }
        public ICommand UpdateETACommand { get; private set; }
        public ICommand LoadSequenceCommand { get; private set; }
        public ICommand SaveSequenceCommand { get; private set; }
    }
}