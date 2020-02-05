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

using Accord.Statistics.Models.Regression.Linear;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Model.MyFocuser;
using NINA.Model.MyGuider;
using NINA.Model.MyPlanetarium;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Model.MyWeatherData;
using NINA.PlateSolving;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.WindowService;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class SequenceVM : DockableVM, ITelescopeConsumer, IFocuserConsumer, IFilterWheelConsumer, IRotatorConsumer, IFlatDeviceConsumer, IGuiderConsumer, ICameraConsumer, IWeatherDataConsumer {

        public SequenceVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IFocuserMediator focuserMediator,
                IFilterWheelMediator filterWheelMediator,
                IGuiderMediator guiderMediator,
                IRotatorMediator rotatorMediator,
                IFlatDeviceMediator flatDeviceMediator,
                IWeatherDataMediator weatherDataMediator,
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

            this._flatDeviceMediator = flatDeviceMediator;
            this._flatDeviceMediator.RegisterConsumer(this);

            this.guiderMediator = guiderMediator;
            this.guiderMediator.RegisterConsumer(this);

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.weatherDataMediator = weatherDataMediator;
            this.weatherDataMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            this.DeepSkyObjectSearchVM = new DeepSkyObjectSearchVM();
            this.DeepSkyObjectSearchVM.PropertyChanged += DeepSkyObjectDetailVM_PropertyChanged;

            this.profileService = profileService;
            Title = "LblSequence";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current?.Resources["SequenceSVG"];

            AddSequenceRowCommand = new RelayCommand(AddSequenceRow);
            AddTargetCommand = new RelayCommand(AddTarget);
            RemoveTargetCommand = new RelayCommand(RemoveTarget, (object o) => this.Targets.Count > 1);
            ResetTargetCommand = new RelayCommand(ResetTarget, ResetTargetEnabled);
            RemoveSequenceRowCommand = new RelayCommand(RemoveSequenceRow);
            PromoteSequenceRowCommand = new RelayCommand(PromoteSequenceRow);
            DemoteSequenceRowCommand = new RelayCommand(DemoteSequenceRow);
            ResetSequenceRowCommand = new RelayCommand(ResetSequenceRow, ResetSequenceRowEnabled);
            StartSequenceCommand = new AsyncCommand<bool>(() => StartSequencing(new Progress<ApplicationStatus>(p => Status = p)), (object o) => !imagingMediator.IsLooping);
            SaveSequenceCommand = new RelayCommand(SaveSequence);
            SaveAsSequenceCommand = new RelayCommand(SaveAsSequence);
            LoadSequenceCommand = new RelayCommand(LoadSequence);
            CancelSequenceCommand = new RelayCommand(CancelSequence);
            PauseSequenceCommand = new RelayCommand(PauseSequence, (object o) => !_pauseTokenSource?.IsPaused == true);
            ResumeSequenceCommand = new RelayCommand(ResumeSequence);
            PromoteTargetCommand = new RelayCommand(PromoteTarget);
            DemoteTargetCommand = new RelayCommand(DemoteTarget);
            SaveTargetSetCommand = new RelayCommand(SaveTargetSet);
            LoadTargetSetCommand = new RelayCommand(LoadTargetSet);
            CoordsFromPlanetariumCommand = new AsyncCommand<bool>(() => Task.Run(CoordsFromPlanetarium));

            autoUpdateTimer = new DispatcherTimer(DispatcherPriority.Background);
            autoUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            autoUpdateTimer.IsEnabled = true;
            autoUpdateTimer.Tick += (sender, args) => CalculateETA();

            profileService.LocationChanged += (object sender, EventArgs e) => {
                foreach (var seq in this.Targets) {
                    var dso = new DeepSkyObject(seq.DSO.Name, seq.DSO.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                    dso.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                    seq.SetSequenceTarget(dso);
                }
            };

            autoUpdateTimer.Start();

            PropertyChanged += SequenceVM_PropertyChanged;
        }

        private void SequenceVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(Sequence))
                ActiveSequenceChanged();
        }

        private ImageHistoryVM imgHistoryVM;

        public ImageHistoryVM ImgHistoryVM {
            get {
                if (imgHistoryVM == null) {
                    imgHistoryVM = new ImageHistoryVM(profileService);
                }
                return imgHistoryVM;
            }
            set {
                imgHistoryVM = value;
                RaisePropertyChanged();
            }
        }

        private void DeepSkyObjectDetailVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DeepSkyObjectSearchVM.SelectedTargetSearchResult)) {
                if (DeepSkyObjectSearchVM.SelectedTargetSearchResult != null) {
                    Sequence.PropertyChanged -= _sequence_PropertyChanged;

                    Sequence.TargetName = DeepSkyObjectSearchVM.SelectedTargetSearchResult.Column1;
                    Sequence.Coordinates = DeepSkyObjectSearchVM.Coordinates;

                    Sequence.PropertyChanged += _sequence_PropertyChanged;
                }
            }
        }

        private DispatcherTimer autoUpdateTimer;

        private void RemoveTarget(object obj) {
            if (this.Targets.Count > 1) {
                var l = (CaptureSequenceList)obj;

                if (l.HasChanged) {
                    if (MyMessageBox.MyMessageBox.Show(
                        string.Format(Locale.Loc.Instance["LblChangedSequenceWarning"], l.TargetName),
                        Locale.Loc.Instance["LblChangedSequenceWarningTitle"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel) != System.Windows.MessageBoxResult.OK) {
                        return;
                    }
                }

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

        private void ResetTarget(object obj) {
            var target = (CaptureSequenceList)obj;
            foreach (CaptureSequence cs in target) {
                cs.ProgressExposureCount = 0;
            }

            target.IsFinished = false;
        }

        private bool ResetTargetEnabled(object obj) {
            var target = (CaptureSequenceList)obj;
            foreach (CaptureSequence cs in target) {
                if (cs.ProgressExposureCount != 0) return true;
            }
            return false;
        }

        private void AddTarget(object obj) {
            this.Targets.Add(GetTemplate());
            Sequence = Targets.Last();
        }

        private void PromoteTarget(object obj) {
            if (Sequence != null) {
                // Promoting a target moves it earlier in the sequence so the UI moves it to the left
                int activeTargetIndex = Targets.IndexOf(Sequence);
                if (activeTargetIndex > 0)
                    Targets.Move(activeTargetIndex, activeTargetIndex - 1);
            }
        }

        private void DemoteTarget(object obj) {
            if (Sequence != null) {
                // Demoting a target moves it later in the sequence so the UI moves it to the right
                int activeTargetIndex = Targets.IndexOf(Sequence);
                if ((activeTargetIndex < Targets.Count - 1) && (activeTargetIndex > -1))
                    Targets.Move(activeTargetIndex, activeTargetIndex + 1);
            }
        }

        private void SaveTargetSet(object obj) {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.Title = Locale.Loc.Instance["LblSaveTargetSet"];
            dialog.FileName = "";
            dialog.DefaultExt = "ninaTargetSet";
            dialog.Filter = "N.I.N.A target set files|*." + dialog.DefaultExt;
            dialog.OverwritePrompt = true;

            if (dialog.ShowDialog().Value) {
                CaptureSequenceList.SaveSequenceSet(Targets, dialog.FileName);
            }
        }

        private void LoadTargetSet(object obj) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = Locale.Loc.Instance["LblLoadTargetSet"];
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.FileName = "";
            dialog.DefaultExt = "ninaTargetSet";
            dialog.Filter = "N.I.N.A target set files|*." + dialog.DefaultExt;

            if (dialog.ShowDialog() == true) {
                using (var s = new FileStream(dialog.FileName, FileMode.Open)) {
                    Targets = new AsyncObservableCollection<CaptureSequenceList>(CaptureSequenceList.LoadSequenceSet(
                        s,
                        profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                        profileService.ActiveProfile.AstrometrySettings.Latitude,
                        profileService.ActiveProfile.AstrometrySettings.Longitude
                    ));
                    foreach (var l in Targets) {
                        AdjustCaptureSequenceListForSynchronization(l);
                    }
                    Sequence = Targets.FirstOrDefault();
                }
            }
        }

        private void LoadSequence(object obj) {
            // LoadSequence loads .xml files indivually - user may select any number of files from same folder
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Title = Locale.Loc.Instance["LblLoadSequence"];
            dialog.FileName = "Target";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";

            if (dialog.ShowDialog() == true) {
                foreach (var fileName in dialog.FileNames) {
                    var l = CaptureSequenceList.Load(fileName,
                        profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                        profileService.ActiveProfile.AstrometrySettings.Latitude,
                        profileService.ActiveProfile.AstrometrySettings.Longitude
                    );
                    AdjustCaptureSequenceListForSynchronization(l);
                    this.Targets.Add(l);

                    // set the last one loaded as the current sequence
                    Sequence = l;
                }
            }
        }

        private bool isUsingSynchronizedGuider;

        public bool IsUsingSynchronizedGuider {
            get => isUsingSynchronizedGuider;
            set {
                isUsingSynchronizedGuider = value;
                RaisePropertyChanged();
            }
        }

        public bool OKtoExit() {
            if (Targets.Any(t => t.HasChanged))
                if (MyMessageBox.MyMessageBox.Show(
                    string.Format(Locale.Loc.Instance["LblChangedSequenceWarning"],
                        Targets.Where(t => t.HasChanged)
                                .Aggregate("", (list, t) => list + ", " + t.TargetName)
                                .TrimStart(new char[] { ',', ' ' })),
                    Locale.Loc.Instance["LblChangedSequenceWarningTitle"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel) != System.Windows.MessageBoxResult.OK)
                    return false;

            return true;
        }

        private void SaveSequence(object obj) {
            // SaveSequence now saves only the active sequence
            if (!Sequence.HasFileName) {
                SaveAsSequence(obj);
            } else {
                Sequence.Save(Sequence.SequenceFileName);
            }
        }

        private void SaveAsSequence(object obj) {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.InitialDirectory = profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            dialog.Title = Locale.Loc.Instance["LblSaveAsSequence"];
            dialog.FileName = Sequence.TargetName;
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";
            dialog.OverwritePrompt = true;

            if (dialog.ShowDialog().Value) {
                Sequence.SequenceFileName = dialog.FileName;

                Sequence.Save(Sequence.SequenceFileName);
            }
        }

        private void ResumeSequence(object obj) {
            if (_pauseTokenSource != null) {
                autoUpdateTimer.Stop();
                _pauseTokenSource.IsPaused = false;
            }
        }

        private void PauseSequence(object obj) {
            if (_pauseTokenSource != null) {
                autoUpdateTimer.Start();
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

                var activeSequence = Sequence.ActiveSequence;

                if (activeSequence != null) {
                    _status.Status2 = Locale.Loc.Instance["LblSequence"];
                    _status.ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue;
                    _status.Progress2 = Sequence.ActiveSequenceIndex;
                    _status.MaxProgress2 = Sequence.Count;

                    _status.Status3 = Locale.Loc.Instance["LblExposures"];
                    _status.ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue;
                    _status.Progress3 = activeSequence.ProgressExposureCount;
                    _status.MaxProgress3 = activeSequence.TotalExposureCount;
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
                seq.EstimatedStartTime = DateTime.Now.AddSeconds(time.TotalSeconds);
                foreach (CaptureSequence cs in seq) {
                    if (cs.Enabled) {
                        var exposureCount = cs.TotalExposureCount - cs.ProgressExposureCount;
                        time = time.Add(
                            TimeSpan.FromSeconds(exposureCount *
                                                 (cs.ExposureTime + EstimatedDownloadTime.TotalSeconds)));
                    }
                }
                seq.EstimatedEndTime = DateTime.Now.AddSeconds(time.TotalSeconds);
                seq.EstimatedDuration = seq.EstimatedEndTime - seq.EstimatedStartTime;
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
                RaisePropertyChanged(nameof(SequenceEstimatedStartTime));
                RaisePropertyChanged(nameof(SequenceEstimatedEndTime));
                RaisePropertyChanged(nameof(SequenceEstimatedDuration));
            }
        }

        public DateTime SequenceEstimatedStartTime {
            get {
                return (Sequence != null) ? Sequence.EstimatedStartTime : DateTime.Now;
            }
        }

        public DateTime SequenceEstimatedEndTime {
            get {
                return (Sequence != null) ? Sequence.EstimatedEndTime : DateTime.Now;
            }
        }

        public TimeSpan SequenceEstimatedDuration {
            get {
                return (Sequence != null) ? Sequence.EstimatedDuration : TimeSpan.MinValue;
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

        private async Task RotateEquipment(CaptureSequenceList csl, PlateSolveResult plateSolveResult, CancellationToken ct, IProgress<ApplicationStatus> progress) {
            // Rotate to desired angle
            if (csl.CenterTarget && rotatorInfo?.Connected == true) {
                await StopGuiding(ct, progress);
                var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
                var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

                var service = WindowServiceFactory.Create();
                var plateSolveStatusVM = new PlateSolvingStatusVM();
                service.Show(plateSolveStatusVM, this.Title + " - " + plateSolveStatusVM.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);

                var orientation = (float)(plateSolveResult?.Orientation ?? 0.0f);
                float position = 0.0f;
                do {
                    if (plateSolveResult == null) {
                        var seq = new CaptureSequence(
                            profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                            CaptureSequence.ImageTypes.SNAPSHOT,
                            profileService.ActiveProfile.PlateSolveSettings.Filter,
                            new Model.MyCamera.BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                            1
                        );
                        seq.Gain = profileService.ActiveProfile.PlateSolveSettings.Gain;
                        var parameter = new CaptureSolverParameter() {
                            Attempts = profileService.ActiveProfile.PlateSolveSettings.NumberOfAttempts,
                            Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                            DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                            FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                            MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                            PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                            ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                            Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                            SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                            Coordinates = telescopeMediator.GetCurrentPosition()
                        };
                        var solver = new CaptureSolver(plateSolver, blindSolver, imagingMediator);

                        plateSolveResult = await solver.Solve(seq, parameter, plateSolveStatusVM.Progress, progress, _canceltoken.Token);
                    }

                    if (!plateSolveResult.Success) {
                        break;
                    }

                    orientation = (float)plateSolveResult.Orientation;

                    position = (float)((float)csl.DSO.Rotation - orientation);

                    var movement = Astrometry.EuclidianModulus((float)csl.DSO.Rotation - orientation, 180);
                    var movement2 = movement - 180;

                    if (movement < Math.Abs(movement2)) {
                        position = movement;
                    } else {
                        position = movement2;
                    }

                    if (Math.Abs(position) > profileService.ActiveProfile.PlateSolveSettings.RotationTolerance) {
                        await rotatorMediator.MoveRelative(position);
                    }
                    plateSolveResult = null;
                } while (Math.Abs(position) > profileService.ActiveProfile.PlateSolveSettings.RotationTolerance);
                service.DelayedClose(TimeSpan.FromSeconds(10));
            }
        }

        private async Task<PlateSolveResult> SlewToTarget(CaptureSequenceList csl, CancellationToken ct, IProgress<ApplicationStatus> progress) {
            PlateSolveResult plateSolveResult = null;
            if (csl.SlewToTarget) {
                await StopGuiding(ct, progress);
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

                var service = WindowServiceFactory.Create();
                var plateSolveStatusVM = new PlateSolvingStatusVM();
                service.Show(plateSolveStatusVM, this.Title + " - " + plateSolveStatusVM.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
                var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
                var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

                var seq = new CaptureSequence(
                    profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                    CaptureSequence.ImageTypes.SNAPSHOT,
                    profileService.ActiveProfile.PlateSolveSettings.Filter,
                    new Model.MyCamera.BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                    1
                );
                seq.Gain = profileService.ActiveProfile.PlateSolveSettings.Gain;

                var solver = new CenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator);
                var parameter = new CenterSolveParameter() {
                    Attempts = profileService.ActiveProfile.PlateSolveSettings.NumberOfAttempts,
                    Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                    Coordinates = telescopeMediator.GetCurrentPosition(),
                    DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                    FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                    MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                    PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                    ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                    Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                    SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                    Threshold = profileService.ActiveProfile.PlateSolveSettings.Threshold
                };
                plateSolveResult = await solver.Center(seq, parameter, plateSolveStatusVM.Progress, progress, _canceltoken.Token);

                service.DelayedClose(TimeSpan.FromSeconds(10));
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

        private async Task AutoFocusOnStart(CaptureSequenceList csl, CancellationToken ct, IProgress<ApplicationStatus> progress) {
            if (csl.AutoFocusOnStart) {
                if (profileService.ActiveProfile.FocuserSettings.AutoFocusDisableGuiding) {
                    await StopGuiding(ct, progress);
                }
                var item = csl.GetNextSequenceItem(csl.ActiveSequence);
                await AutoFocus(item.FilterType, ct, progress);
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

        private async Task StopGuiding(CancellationToken ct, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStopGuiding"] });
            await this.guiderMediator.StopGuiding(ct);
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
            bool canceledAtStart = false;
            try {
                profileService.PauseSave();
                _actualDownloadTimes.Clear();
                _canceltoken?.Dispose();
                _canceltoken = new CancellationTokenSource();
                _pauseTokenSource = new PauseTokenSource();
                IsPaused = false;
                IsRunning = true;

                /* Validate if preconditions are met */
                if (!CheckPreconditions()) {
                    canceledAtStart = true;
                    return false;
                }

                if (this.Targets.Count > 0) {
                    autoUpdateTimer.Stop();
                    // If sequencing was stopped (vs paused) and started again, reset active sequence of each target to the first one
                    foreach (CaptureSequenceList csl in this.Targets) {
                        csl.ResetActiveSequence();
                    }
                    var iterator = 0;
                    do {
                        var csl = this.Targets[iterator];
                        try {
                            csl.IsFinished = false;
                            csl.IsRunning = true;
                            Sequence = csl;
                            await StartSequence(csl, _canceltoken.Token, _pauseTokenSource.Token, progress);
                            csl.IsFinished = true;
                        } finally {
                            csl.IsRunning = false;

                            //Check if current target was not removed during pause
                            if (this.Targets.Contains(csl)) {
                                //Check the current index of current target (for the case that a previous target was removed during pause) and increment by one
                                iterator = this.Targets.IndexOf(csl) + 1;
                            }
                        }
                    } while (iterator < this.Targets.Count);
                }
            } catch (OperationCanceledException) {
            } finally {
                if (!canceledAtStart) {
                    await RunEndOfSequenceOptions(progress);
                }
                profileService.ResumeSave();
                IsPaused = false;
                IsRunning = false;
                autoUpdateTimer.Start();
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return true;
        }

        private async Task<bool> StartSequence(CaptureSequenceList csl, CancellationToken ct, PauseToken pt, IProgress<ApplicationStatus> progress) {
            try {
                if (csl.Count <= 0 || csl.GetNextSequenceItem(csl.ActiveSequence) == null) {
                    return false;
                }

                CalculateETA();

                /* delay sequence start by given amount */
                await DelaySequence(csl, progress);

                //Slew and center
                PlateSolveResult plateSolveResult = await SlewToTarget(csl, ct, progress);

                //Rotate for framing
                await RotateEquipment(csl, plateSolveResult, ct, progress);

                if (!profileService.ActiveProfile.FocuserSettings.AutoFocusDisableGuiding) {
                    await StartGuiding(csl, progress);
                }

                await AutoFocusOnStart(csl, ct, progress);

                await StartGuiding(csl, progress);

                return await ProcessSequence(csl, ct, pt, progress);
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
        }

        private async Task<AutoFocus.AutoFocusReport> AutoFocus(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            using (var autoFocus = new AutoFocusVM(profileService, cameraMediator, filterWheelMediator, focuserMediator, guiderMediator, imagingMediator, applicationStatusMediator)) {
                var service = WindowServiceFactory.Create();
                service.Show(autoFocus, this.Title + " - " + autoFocus.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
                var report = await autoFocus.StartAutoFocus(filter, token, progress);
                service.DelayedClose(TimeSpan.FromSeconds(10));
                return report;
            }
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets reference coordinates for the sequence. When centering is enabled it will get the current specified coordinates, otherwise the current scope position
        /// </summary>
        /// <param name="csl"></param>
        /// <returns></returns>
        private Coordinates GetReferenceCoordinates(CaptureSequenceList csl) {
            if (csl.CenterTarget) {
                return csl.Coordinates;
            } else {
                return telescopeInfo.Coordinates;
            }
        }

        /// <summary>
        /// Processes all sequence items in given capture sequence list
        ///
        /// One Sequence item is processed like:
        ///
        /// 1) Wait for previous item's parallel actions 5a, 5b to
        /// 2) Check if Autofocus is requiredfinish
        /// 3) Change Filter
        /// 3.5) Handle Flat Device
        /// 4) Capture
        /// 5) Parallel Actions after Capture
        ///     5a) Dither
        ///     5b) Change Filter if next sequence item has a different filter set
        /// 6) Download Image
        /// 7) Wait for previous item's parallel actions 8a, 8b to finish
        /// 8) Parallel Actions after Download
        ///     8a) Save ImageData
        ///     8b) Process ImageData for display
        /// </summary>
        /// <param name="csl">List containing Sequence Items to process</param>
        /// <param name="ct">Token to cancel process</param>
        /// <param name="pt">Token to pause process</param>
        /// <param name="progress">Progress to report to application</param>
        /// <returns></returns>
        private async Task<bool> ProcessSequence(CaptureSequenceList csl, CancellationToken ct, PauseToken pt, IProgress<ApplicationStatus> progress) {
            return await Task.Run<bool>(async () => {
                try {
                    //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released
                    await semaphoreSlim.WaitAsync(ct);

                    csl.IsRunning = true;

                    CaptureSequence seq;
                    short prevFilterPosition = filterWheelInfo?.SelectedFilter?.Position ?? -1;
                    var lastAutoFocusTime = DateTime.UtcNow;
                    var lastAutoFocusTemperature = focuserInfo?.Temperature ?? double.NaN;
                    var exposureCount = 0;

                    var referenceCoordinates = GetReferenceCoordinates(csl);

                    Task saveTask = null;
                    Task ditherTask = null;
                    Task filterChangeTask = null;
                    Task<IRenderedImage> imageProcessingTask = null;
                    while ((seq = csl.Next()) != null) {
                        exposureCount++;

                        seq.NextSequence = csl.GetNextSequenceItem(seq);

                        if (seq.IsLightSequence()) {
                            await CheckMeridianFlip(referenceCoordinates, seq.ExposureTime, ct, progress);
                        }

                        Stopwatch seqDuration = Stopwatch.StartNew();

                        /* 1) Wait for previous item's parallel action 5a to finish */
                        if (ditherTask?.IsCompleted == false) {
                            /* Wait for dither to finish. Runs in parallel to download and save. */
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForDither"] });
                            await ditherTask;
                        }

                        /* 1) Wait for previous item's parallel action 5b to finish */
                        if (filterChangeTask?.IsCompleted == false) {
                            /* Wait for FilterChange to finish. Runs in parallel to download and save. */
                            progress.Report(new ApplicationStatus() { Status = $"{Locale.Loc.Instance["LblChange"]} {Locale.Loc.Instance["LblFilter"]}" });

                            await filterChangeTask;
                        }

                        /* 2) Check if Autofocus is requiredfinish */
                        if (seq.IsLightSequence() && ShouldAutoFocus(csl, seq, exposureCount - 1, prevFilterPosition, lastAutoFocusTime, lastAutoFocusTemperature)) {
                            //Wait for previous image processing task to be done, so Autofocus doesn't turn off HFR calculation too early
                            if (imageProcessingTask != null && !imageProcessingTask.IsCompleted) {
                                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForImageProcessing"] });
                                await imageProcessingTask;
                            }

                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblAutoFocus"] });
                            var report = await AutoFocus(seq.FilterType, _canceltoken.Token, progress);
                            ImgHistoryVM.AppendAutoFocusPoint(report);
                            lastAutoFocusTime = DateTime.UtcNow;
                            lastAutoFocusTemperature = focuserInfo?.Temperature ?? double.NaN;
                            progress.Report(new ApplicationStatus() { Status = " " });
                        }

                        /* 3) Change Filter */
                        if (!seq.IsDarkSequence() && seq.FilterType != null) {
                            prevFilterPosition = filterWheelInfo?.SelectedFilter?.Position ?? -1;
                            await filterWheelMediator.ChangeFilter(seq.FilterType, ct, progress);
                        }

                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblImaging"] });
                        /* Start RMS Recording */
                        var rmsHandle = this.guiderMediator.StartRMSRecording();

                        /* 3.5 Flat device handling */
                        if (_flatDevice != null && _flatDevice.Connected) {
                            await HandleFlatDevice(seq);
                        }

                        /* 4) Capture */
                        var exposureStart = DateTime.Now;
                        await cameraMediator.Capture(seq, ct, progress);

                        /* Stop RMS Recording */
                        var rms = this.guiderMediator.StopRMSRecording(rmsHandle);

                        /* 5a) Dither */
                        if (seq.IsLightSequence()) {
                            ditherTask = ShouldDither(seq, ct, progress);
                        }

                        /* 5b) Change Filter if next sequence item has a different filter set */
                        if (seq.NextSequence != null && !seq.NextSequence.IsDarkSequence() && seq.NextSequence != seq) {
                            prevFilterPosition = filterWheelInfo?.SelectedFilter?.Position ?? -1;
                            filterChangeTask = filterWheelMediator.ChangeFilter(seq.NextSequence.FilterType, ct, progress);
                        }

                        /* 6) Download Image */
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblDownloading"] });
                        var data = await cameraMediator.Download(ct);

                        ct.ThrowIfCancellationRequested();

                        if (data != null) {
                            AddMetaData(data.MetaData, csl, seq, exposureStart, rms);

                            /* 8b) Process ImageData for display */
                            imageProcessingTask = imagingMediator.PrepareImage(data, new PrepareImageParameters(), ct);
                            progress.Report(new ApplicationStatus() { Status = " " });

                            /* 7) Wait for previous item's parallel actions 8a, 8b to finish */
                            if (saveTask?.IsCompleted == false) {
                                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForImageSaving"] });
                                await saveTask;
                            }

                            /* 8a) Save ImageData */
                            saveTask = Save(imageProcessingTask, profileService, ct);

                            seqDuration.Stop();

                            AddDownloadTime(seqDuration.Elapsed.Subtract(TimeSpan.FromSeconds(seq.ExposureTime)));
                        } else {
                            Logger.Error(new CameraDownloadFailedException(seq));
                            Notification.ShowError(string.Format(Locale.Loc.Instance["LblCameraDownloadFailed"], seq.ExposureTime, seq.ImageType, seq.Gain, seq.FilterType?.Name ?? string.Empty));
                        }

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
                    }
                    if (saveTask != null && !saveTask.IsCompleted) {
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForImageSaving"] });
                        await saveTask;
                    }
                } catch (OperationCanceledException ex) {
                    throw ex;
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

        private async Task HandleFlatDevice(CaptureSequence seq) {
            switch (seq.ImageType) {
                case CaptureSequence.ImageTypes.FLAT:
                    if (_flatDevice.CoverState != CoverState.Closed) {
                        await _flatDeviceMediator.CloseCover();
                    }

                    if (profileService.ActiveProfile.FlatDeviceSettings.UseWizardTrainedValues) {
                        var settings = profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(new FlatDeviceFilterSettingsKey(seq.FilterType.Name, seq.Binning, seq.Gain));
                        if (settings != null) {
                            _flatDeviceMediator.SetBrightness(settings.Brightness);
                            seq.ExposureTime = settings.Time;
                        }
                    }

                    if (!_flatDevice.LightOn) {
                        _flatDeviceMediator.ToggleLight(true);
                    }

                    break;

                case CaptureSequence.ImageTypes.DARKFLAT:
                    if (_flatDevice.LightOn) {
                        _flatDeviceMediator.ToggleLight(false);
                    }

                    if (profileService.ActiveProfile.FlatDeviceSettings.OpenForDarkFlats) {
                        if (_flatDevice.CoverState != CoverState.Open) {
                            await _flatDeviceMediator.OpenCover();
                            MyMessageBox.MyMessageBox.Show(
                            Locale.Loc.Instance["LblCoverScopeMsgBox"],
                            Locale.Loc.Instance["LblCoverScopeMsgBoxTitle"], MessageBoxButton.OKCancel,
                            MessageBoxResult.OK);
                        }
                    } else {
                        if (_flatDevice.CoverState != CoverState.Closed) {
                            await _flatDeviceMediator.CloseCover();
                        }
                    }

                    if (profileService.ActiveProfile.FlatDeviceSettings.UseWizardTrainedValues) {
                        var settings = profileService.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(new FlatDeviceFilterSettingsKey(seq.FilterType.Name, seq.Binning, seq.Gain));
                        if (settings != null) {
                            seq.ExposureTime = settings.Time;
                        }
                    }

                    break;

                case CaptureSequence.ImageTypes.DARK:
                    if (_flatDevice.LightOn) {
                        _flatDeviceMediator.ToggleLight(false);
                    }

                    if (profileService.ActiveProfile.FlatDeviceSettings.OpenForDarkFlats) {
                        if (_flatDevice.CoverState != CoverState.Open) {
                            await _flatDeviceMediator.OpenCover();
                            MyMessageBox.MyMessageBox.Show(
                                Locale.Loc.Instance["LblCoverScopeMsgBox"],
                                Locale.Loc.Instance["LblCoverScopeMsgBoxTitle"], MessageBoxButton.OKCancel,
                                MessageBoxResult.OK);
                        }
                    } else {
                        if (_flatDevice.CoverState != CoverState.Closed) {
                            await _flatDeviceMediator.CloseCover();
                        }
                    }

                    break;

                default:
                    if (_flatDevice.CoverState != CoverState.Open) {
                        await _flatDeviceMediator.OpenCover();
                    }
                    break;
            }
        }

        private void AddMetaData(
                ImageMetaData metaData,
                CaptureSequenceList csl,
                CaptureSequence sequence,
                DateTime start,
                RMS rms) {
            metaData.Image.ExposureStart = start;
            metaData.Image.Binning = sequence.Binning.Name;
            metaData.Image.ExposureNumber = sequence.ProgressExposureCount;
            metaData.Image.ExposureTime = sequence.ExposureTime;
            metaData.Image.ImageType = sequence.ImageType;
            metaData.Image.RecordedRMS = rms;
            metaData.Target.Name = csl.TargetName;
            metaData.Target.Coordinates = csl.Coordinates;

            // Fill all available info from profile
            metaData.FromProfile(profileService.ActiveProfile);
            metaData.FromTelescopeInfo(telescopeInfo);
            metaData.FromFilterWheelInfo(filterWheelInfo);
            metaData.FromRotatorInfo(rotatorInfo);
            metaData.FromFocuserInfo(focuserInfo);
            metaData.FromWeatherDataInfo(weatherDataInfo);

            metaData.FilterWheel.Filter = sequence.FilterType?.Name ?? metaData.FilterWheel.Filter;
        }

        private Task Save(Task<IRenderedImage> imageProcessingTask, IProfileService profileService, CancellationToken ct) {
            return Task.Run(async () => {
                var processedImage = await imageProcessingTask;
                var data = processedImage.RawImageData;
                var tempPath = await data.PrepareSave(new FileSaveInfo(profileService), ct);
                var renderedImage = await imageProcessingTask;

                var path = data.FinalizeSave(tempPath, profileService.ActiveProfile.ImageFileSettings.FilePattern);
                var imageStatistics = await data.Statistics.Task;

                imagingMediator.OnImageSaved(
                        new ImageSavedEventArgs() {
                            PathToImage = new Uri(path),
                            Image = processedImage.Image,
                            FileType = profileService.ActiveProfile.ImageFileSettings.FileType,
                            Mean = imageStatistics.Mean,
                            HFR = data.StarDetectionAnalysis.HFR,
                            Duration = data.MetaData.Image.ExposureTime,
                            IsBayered = data.Properties.IsBayered,
                            Filter = data.MetaData.FilterWheel.Filter
                        }
                );

                ImgHistoryVM.Add(data.StarDetectionAnalysis);
            });
        }

        private async Task<bool> ShouldDither(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (seq.Dither && ((seq.ProgressExposureCount % seq.DitherAmount) == 0)) {
                return await this.guiderMediator.Dither(token);
            }
            token.ThrowIfCancellationRequested();
            return false;
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

            if (csl.AutoFocusAfterSetExposures
                && (
                    (csl.AutoFocusSetExposures == 1)
                    || (exposureCount > 0 && exposureCount % csl.AutoFocusSetExposures == 0)
                )) {
                /* Trigger autofocus after amount of exposures*/
                return true;
            }

            if (csl.AutoFocusAfterTemperatureChange && !double.IsNaN(focuserInfo?.Temperature ?? double.NaN)
                && Math.Abs(lastAutoFocusTemperature - focuserInfo.Temperature) > csl.AutoFocusAfterTemperatureChangeAmount) {
                /* Trigger autofocus after temperature change*/
                return true;
            }

            if (csl.AutoFocusAfterHFRChange
                && imgHistoryVM.ImageHistory.Count() - AfHfrIndex > 3
                && imgHistoryVM.ImageHistory.Last().HFR != 0
                && imgHistoryVM.ImageHistory.ElementAt(AfHfrIndex).HFR != 0) {
                
                //Perform linear regression on HFR points since last AF run
                double[] outputs = imgHistoryVM.ImageHistory.Skip(AfHfrIndex).Select(img => img.HFR).ToArray();
                double[] inputs = Enumerable.Range(AfHfrIndex, imgHistoryVM.ImageHistory.Count() - AfHfrIndex).Select(index => (double)index).ToArray();
                OrdinaryLeastSquares ols = new OrdinaryLeastSquares();
                SimpleLinearRegression regression = ols.Learn(inputs, outputs);
                
                //Get current smoothed out HFR
                double currentHfrTrend = regression.Transform(imgHistoryVM.ImageHistory.Count());

                if (currentHfrTrend > (imgHistoryVM.ImageHistory.ElementAt(AfHfrIndex).HFR * (1 + csl.AutoFocusAfterHFRChangeAmount / 100))) {
                    /* Trigger autofocus after HFR change */
                    AfHfrIndex = imgHistoryVM.ImageHistory.Count();
                    return true;
                }
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
        /// <param name="flipTargetCoordinates">Reference Coordinates for the flip</param>
        /// <param name="nextExposureTime">Next exposure time</param>
        /// <param name="tokenSource">cancel token</param>
        /// <param name="progress">   progress reporter</param>
        /// <returns></returns>
        private async Task CheckMeridianFlip(Coordinates flipTargetCoordinates, double nextExposureTime, CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblCheckMeridianFlip"] });
            if (telescopeInfo != null && MeridianFlipVM.ShouldFlip(profileService, nextExposureTime, telescopeInfo)) {
                var target = flipTargetCoordinates;
                if (flipTargetCoordinates.RA == 0 && flipTargetCoordinates.Dec == 0) {
                    target = telescopeInfo.Coordinates;
                }
                await new MeridianFlipVM(profileService, telescopeMediator, guiderMediator, imagingMediator, applicationStatusMediator).MeridianFlip(target, TimeSpan.FromHours(telescopeInfo.TimeToMeridianFlip));
            }
            progress.Report(new ApplicationStatus() { Status = string.Empty });
        }

        private bool CheckPreconditions() {
            StringBuilder messageStringBuilder = new StringBuilder();
            bool displayMessage = false;

            messageStringBuilder.AppendLine(Locale.Loc.Instance["LblPreSequenceChecklist"]).AppendLine();

            if (cameraInfo.CoolerOn && !cameraMediator.AtTargetTemp) {
                messageStringBuilder.AppendFormat(Locale.Loc.Instance["LblCameraNotAtTargetTemp"], Math.Round(cameraInfo.Temperature, 2), cameraMediator.TargetTemp).AppendLine();
                displayMessage = true;
            }

            if (cameraInfo.CoolerOn && cameraInfo.CoolerPower >= 80) {
                messageStringBuilder.AppendFormat(Locale.Loc.Instance["LblCameraHighPower"], Math.Round(cameraInfo.CoolerPower, 0)).AppendLine();
                displayMessage = true;
            }

            if (!guiderInfo.Connected && Targets.Any(target => target.Items.Any(item => item.Dither && item.Enabled))) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblDitherOnButGuiderNotConnected"]);
                displayMessage = true;
            }

            if (!filterWheelInfo.Connected && Targets.Any(target => target.Items.Any(item => item.FilterType != null && item.Enabled))) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblFilterSetButFilterWheelNotConnected"]);
                displayMessage = true;
            }

            if (!guiderInfo.Connected && Targets.Any(target => target.StartGuiding)) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblStartGuidingButGuiderNotConnected"]);
                displayMessage = true;
            }

            if (!focuserInfo.Connected && Targets.Any(target => target.AutoFocusAfterSetExposures || target.AutoFocusAfterSetTime || target.AutoFocusAfterTemperatureChange || target.AutoFocusOnFilterChange || target.AutoFocusOnStart || target.AutoFocusAfterHFRChange)) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblAFOnButFocuserNotConnected"]);
                displayMessage = true;
            }

            if (cameraInfo.CanSetTemperature && !cameraInfo.CoolerOn) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblCameraCoolerNotEnabled"]);
                displayMessage = true;
            }

            if (!telescopeInfo.Connected && Targets.Any(target => target.SlewToTarget)) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblSlewEnabledButTelescopeNotConnected"]);
                displayMessage = true;
            }

            if (Targets.Any(target => target.SlewToTarget && target.Coordinates.Dec == 0 && target.Coordinates.RA == 0)) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblSlewWhenTargetCoordsUnset"]);
                displayMessage = true;
            }

            if (telescopeInfo.Connected && !telescopeInfo.CanPark && !telescopeInfo.CanSetTracking && profileService.ActiveProfile.SequenceSettings.ParkMountAtSequenceEnd) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblParkEnabledButTelescopeCannotPark"]);
                displayMessage = true;
            }

            if (!telescopeInfo.Connected && profileService.ActiveProfile.SequenceSettings.ParkMountAtSequenceEnd) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblParkEnabledButTelescopeNotConnected"]);
                displayMessage = true;
            }

            if (!cameraInfo.CanSetTemperature && profileService.ActiveProfile.SequenceSettings.WarmCamAtSequenceEnd) {
                messageStringBuilder.AppendLine(Locale.Loc.Instance["LblWarmEnabledButCameraCannotWarm"]);
                displayMessage = true;
            }

            messageStringBuilder.AppendLine();
            messageStringBuilder.Append(Locale.Loc.Instance["LblStartSequenceAnyway"]);

            if (displayMessage) {
                var diag = MyMessageBox.MyMessageBox.Show(messageStringBuilder.ToString(), Locale.Loc.Instance["LblPreSequenceChecklistHeader"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
                if (diag == System.Windows.MessageBoxResult.Cancel) {
                    return false;
                }
            }

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
            if (arc == null) {
                return false;
            }

            foreach (FileSystemAccessRule rule in arc) {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write) {
                    continue;
                }

                if (rule.AccessControlType == AccessControlType.Allow) {
                    Allow = true;
                } else if (rule.AccessControlType == AccessControlType.Deny) {
                    Deny = true;
                }
            }

            if (Allow && !Deny) {
                return true;
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblDirectoryNotWritable"]);
                return false;
            }
        }

        public async Task<bool> SetSequenceCoordiantes(DeepSkyObject dso) {
            Sequence.PropertyChanged -= _sequence_PropertyChanged;

            var sequenceDso = new DeepSkyObject(dso.AlsoKnownAs.FirstOrDefault() ?? dso.Name ?? string.Empty, dso.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
            sequenceDso.Rotation = dso.Rotation;
            await Task.Run(() => {
                sequenceDso.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
            });

            Sequence.SetSequenceTarget(sequenceDso);

            Sequence.PropertyChanged += _sequence_PropertyChanged;

            return true;
        }

        public async Task<bool> SetSequenceCoordiantes(ICollection<DeepSkyObject> deepSkyObjects, bool replace = true) {
            if (replace) {
                Targets.Clear();
                Sequence = null;
            }

            foreach (var dso in deepSkyObjects) {
                AddTarget(null);
                Sequence = Targets.Last();
                await SetSequenceCoordiantes(dso);
            }
            Sequence = Targets.FirstOrDefault();
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

        private void AdjustCaptureSequenceListForSynchronization(CaptureSequenceList csl) {
            if (IsUsingSynchronizedGuider) {
                foreach (var item in csl.Items) {
                    item.Dither = true;
                    item.DitherAmount = 1;
                }
            }
        }

        private CaptureSequenceList GetTemplate() {
            CaptureSequenceList csl = null;
            if (File.Exists(profileService.ActiveProfile.SequenceSettings.TemplatePath)) {
                csl = CaptureSequenceList.Load(profileService.ActiveProfile.SequenceSettings.TemplatePath,
                    profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );
                AdjustCaptureSequenceListForSynchronization(csl);
            } else {
                var seq = new CaptureSequence();
                csl = new CaptureSequenceList(seq) { TargetName = "Target" };
                csl.DSO?.SetDateAndPosition(
                    SkyAtlasVM.GetReferenceDate(DateTime.Now),
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );
                AdjustCaptureSequenceListForSynchronization(csl);
            }

            csl.MarkAsUnchanged();
            return csl;
        }

        private CaptureSequenceList _sequence;

        public CaptureSequenceList Sequence {
            get {
                if (_sequence == null) {
                    Sequence = GetTemplate();
                    SelectedSequenceRowIdx = _sequence.Count - 1;
                }
                return _sequence;
            }
            set {
                if (_sequence != null) {
                    _sequence.PropertyChanged -= _sequence_PropertyChanged;
                }

                _sequence = value;
                if (_sequence != null) {
                    _sequence.PropertyChanged += _sequence_PropertyChanged;
                }

                RaisePropertyChanged();
            }
        }

        public DeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; private set; }

        private void _sequence_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(CaptureSequenceList.TargetName)) {
                if (Sequence.TargetName.Length > 1) {
                    DeepSkyObjectSearchVM.TargetName = Sequence.TargetName;
                }
            }
            if (e.PropertyName == nameof(CaptureSequenceList.HasChanged)) {
                RaisePropertyChanged(nameof(SequenceSaveable));
                RaisePropertyChanged(nameof(SequenceModified));
            }
            if (e.PropertyName == nameof(CaptureSequenceList.HasFileName)) {
                RaisePropertyChanged(nameof(HasSequenceFileName));
            }
            if (e.PropertyName == nameof(CaptureSequenceList.EstimatedStartTime)) {
                RaisePropertyChanged("SequenceEstimatedStartTime");
            }
            if (e.PropertyName == nameof(CaptureSequenceList.EstimatedEndTime)) {
                RaisePropertyChanged("SequenceEstimatedEndTime");
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
        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
        private IRotatorMediator rotatorMediator;
        private RotatorInfo rotatorInfo;
        private IFlatDeviceMediator _flatDeviceMediator;
        private FlatDeviceInfo _flatDevice;
        private IWeatherDataMediator weatherDataMediator;
        private WeatherDataInfo weatherDataInfo;
        private GuiderInfo guiderInfo = DeviceInfo.CreateDefaultInstance<GuiderInfo>();

        private int AfHfrIndex = 0;

        public bool SequenceModified { get { return (Sequence != null) && (Sequence.HasChanged); } }
        public bool HasSequenceFileName { get { return (Sequence != null) && (Sequence.HasFileName); } }
        public bool SequenceSaveable { get { return SequenceModified && HasSequenceFileName; } }

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
            CaptureSequence newSeq = Sequence.Items.Any() ? Sequence.Items.Last().Clone() : new CaptureSequence();
            Sequence.Add(newSeq);
            SelectedSequenceRowIdx = Sequence.Count - 1;
            AdjustCaptureSequenceListForSynchronization(Sequence);
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

        private void ResetSequenceRow(object obj) {
            var idx = SelectedSequenceRowIdx;
            Sequence.ResetAt(idx);
            Sequence.IsFinished = false;
        }

        private bool ResetSequenceRowEnabled(object obj) {
            var idx = SelectedSequenceRowIdx;
            if (idx < 0 || idx >= Sequence.Items.Count) return false;
            return Sequence.Items[idx].ProgressExposureCount != 0;
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

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            this.cameraInfo = cameraInfo;
        }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            this.rotatorInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(WeatherDataInfo deviceInfo) {
            this.weatherDataInfo = deviceInfo;
        }

        private async Task<bool> CoordsFromPlanetarium() {
            IPlanetarium s = PlanetariumFactory.GetPlanetarium(profileService);
            DeepSkyObject resp = null;

            try {
                resp = await s.GetTarget();

                if (resp != null) {
                    Sequence.Coordinates = resp.Coordinates;
                    Sequence.TargetName = resp.Name;
                    Notification.ShowSuccess(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsOk"], s.Name));
                }
            } catch (PlanetariumObjectNotSelectedException) {
                Logger.Error($"Attempted to get coordinates from {s.Name} when no object was selected");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumObjectNotSelected"], s.Name));
            } catch (PlanetariumFailedToConnect ex) {
                Logger.Error($"Unable to connect to {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumFailedToConnect"], s.Name));
            } catch (Exception ex) {
                Logger.Error($"Failed to get coordinates from {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsError"], s.Name));
            }

            return (resp != null);
        }

        private async Task<bool> RunEndOfSequenceOptions(IProgress<ApplicationStatus> progress) {
            bool parkTelescope = false;
            bool warmCamera = false;
            bool closeFlatDeviceCover = false;
            StringBuilder message = new StringBuilder();

            message.AppendLine(Locale.Loc.Instance["LblEndOfSequenceDecision"]).AppendLine();

            if (profileService.ActiveProfile.SequenceSettings.ParkMountAtSequenceEnd) {
                if (telescopeInfo.CanPark || telescopeInfo.CanSetTracking) {
                    parkTelescope = true;
                    message.AppendLine(Locale.Loc.Instance["LblEndOfSequenceParkTelescope"]);
                }
            }

            if (profileService.ActiveProfile.SequenceSettings.WarmCamAtSequenceEnd) {
                if (cameraInfo.CanSetTemperature && cameraInfo.CoolerOn) {
                    warmCamera = true;
                    message.AppendLine(Locale.Loc.Instance["LblEndOfSequenceWarmCamera"]);
                }
            }

            if (_flatDevice != null && _flatDevice.Connected) {
                _flatDeviceMediator.ToggleLight(false);

                if (profileService.ActiveProfile.FlatDeviceSettings.CloseAtSequenceEnd && _flatDevice.SupportsOpenClose) {
                    closeFlatDeviceCover = true;
                    message.AppendLine(Locale.Loc.Instance["LblEndOfSequenceCloseFlatDeviceCover"]);
                }
            }

            if (warmCamera || parkTelescope || closeFlatDeviceCover) {
                if (_canceltoken.Token.IsCancellationRequested) { // Sequence was manually cancelled - ask before proceeding with end of sequence options
                    var diag = MyMessageBox.MyMessageBox.Show(message.ToString(), Locale.Loc.Instance["LblEndOfSequenceOptions"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
                    if (diag != System.Windows.MessageBoxResult.OK) {
                        parkTelescope = false;
                        warmCamera = false;
                        closeFlatDeviceCover = false;
                    } else {
                        // Need to reinitialize the cancellation token, as it is set to cancelation requested since sequence was manually cancelled.
                        _canceltoken?.Dispose();
                        _canceltoken = new CancellationTokenSource();
                    }
                }
                if (parkTelescope) {
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblEndOfSequenceParkTelescope"] });
                    await this.guiderMediator.StopGuiding(_canceltoken.Token);
                    await telescopeMediator.ParkTelescope();
                }
                if (warmCamera) {
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblEndOfSequenceWarmCamera"] });
                    Logger.Trace("Starting to warm the camera");
                    IProgress<double> warmProgress = new Progress<double>();
                    await cameraMediator.StartChangeCameraTemp(warmProgress, 10, TimeSpan.FromMinutes(10), true, _canceltoken.Token);
                    Logger.Trace("Camera has been warmed");
                }
                if (closeFlatDeviceCover) {
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblEndOfSequenceCloseFlatDeviceCover"] });
                    await _flatDeviceMediator.CloseCover();
                }
            }
            return true;
        }

        public ICommand CoordsFromPlanetariumCommand { get; set; }
        public ICommand AddSequenceRowCommand { get; private set; }
        public ICommand AddTargetCommand { get; private set; }
        public ICommand RemoveTargetCommand { get; private set; }
        public ICommand ResetTargetCommand { get; private set; }
        public ICommand RemoveSequenceRowCommand { get; private set; }
        public ICommand PromoteSequenceRowCommand { get; private set; }
        public ICommand DemoteSequenceRowCommand { get; private set; }

        public ICommand ResetSequenceRowCommand { get; private set; }

        public IAsyncCommand StartSequenceCommand { get; private set; }

        public ICommand CancelSequenceCommand { get; private set; }
        public ICommand PauseSequenceCommand { get; private set; }
        public ICommand ResumeSequenceCommand { get; private set; }
        public ICommand LoadSequenceCommand { get; private set; }
        public ICommand SaveSequenceCommand { get; private set; }
        public ICommand SaveAsSequenceCommand { get; private set; }

        public ICommand PromoteTargetCommand { get; private set; }
        public ICommand DemoteTargetCommand { get; private set; }
        public ICommand SaveTargetSetCommand { get; private set; }
        public ICommand LoadTargetSetCommand { get; private set; }

        private void PromoteSequenceRow(object obj) {
            if (Sequence != null) {
                var idx = SelectedSequenceRowIdx;
                if (idx > 0) {
                    CaptureSequence seq = Sequence.Items[idx];
                    Sequence.RemoveAt(idx);
                    Sequence.AddAt(idx - 1, seq);
                    SelectedSequenceRowIdx = idx - 1;
                }
            }
        }

        private void DemoteSequenceRow(object obj) {
            if (Sequence != null) {
                var idx = SelectedSequenceRowIdx;
                if ((idx < Sequence.Count - 1) && (idx > -1)) {
                    CaptureSequence seq = Sequence.Items[idx];
                    Sequence.RemoveAt(idx);
                    Sequence.AddAt(idx + 1, seq);
                    SelectedSequenceRowIdx = idx + 1;
                }
            }
        }

        public void UpdateDeviceInfo(GuiderInfo deviceInfo) {
            if (guiderMediator.IsUsingSynchronizedGuider != IsUsingSynchronizedGuider) {
                IsUsingSynchronizedGuider = guiderMediator.IsUsingSynchronizedGuider;
                foreach (var item in Targets) {
                    AdjustCaptureSequenceListForSynchronization(item);
                }
            }
            this.guiderInfo = deviceInfo;
        }

        internal void ActiveSequenceChanged() {
            // refresh properties that depend on Sequence
            RaisePropertyChanged(nameof(SequenceModified));
            RaisePropertyChanged(nameof(HasSequenceFileName));
            RaisePropertyChanged(nameof(SequenceSaveable));
        }

        public void UpdateDeviceInfo(FlatDeviceInfo deviceInfo) {
            this._flatDevice = deviceInfo;
        }

        public void Dispose() {
            this.telescopeMediator.RemoveConsumer(this);
            this.filterWheelMediator.RemoveConsumer(this);
            this.focuserMediator.RemoveConsumer(this);
            this.rotatorMediator.RemoveConsumer(this);
            this._flatDeviceMediator.RemoveConsumer(this);
            this.guiderMediator.RemoveConsumer(this);
            this.cameraMediator.RemoveConsumer(this);
            this.weatherDataMediator.RemoveConsumer(this);
        }
    }
}