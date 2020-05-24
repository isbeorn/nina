#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Imaging {

    internal class AnchorablePlateSolverVM : DockableVM, ICameraConsumer, ITelescopeConsumer {
        private PlateSolveResult _plateSolveResult;

        private ObservableCollection<PlateSolveResult> _plateSolveResultList;

        private double _repeatThreshold;

        private BinningMode _snapBin;

        private double _snapExposureDuration;

        private Model.MyFilterWheel.FilterInfo _snapFilter;

        private int _snapGain = -1;

        private CancellationTokenSource _solveCancelToken;

        private ApplicationStatus _status;

        private IApplicationStatusMediator applicationStatusMediator;

        private CameraInfo cameraInfo;

        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;

        private TelescopeInfo telescopeInfo;

        private ITelescopeMediator telescopeMediator;

        public AnchorablePlateSolverVM(IProfileService profileService,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblPlateSolving";

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];

            SolveCommand = new AsyncCommand<bool>(() => CaptureSolveSyncAndReslew(new Progress<ApplicationStatus>(p => Status = p)));
            CancelSolveCommand = new RelayCommand(CancelSolve);

            SnapExposureDuration = profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
            SnapFilter = profileService.ActiveProfile.PlateSolveSettings.Filter;
            RepeatThreshold = profileService.ActiveProfile.PlateSolveSettings.Threshold;
            SlewToTarget = profileService.ActiveProfile.PlateSolveSettings.SlewToTarget;

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                SnapExposureDuration = profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
                SnapFilter = profileService.ActiveProfile.PlateSolveSettings.Filter;
                RepeatThreshold = profileService.ActiveProfile.PlateSolveSettings.Threshold;
                SlewToTarget = profileService.ActiveProfile.PlateSolveSettings.SlewToTarget;
            };
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

        public ICommand CancelSolveCommand { get; private set; }

        public new string ContentId {
            get {
                //Backwards compatibility for avalondock layouts prior to 1.10
                return "PlatesolveVM";
            }
        }

        public PlateSolveResult PlateSolveResult {
            get {
                return _plateSolveResult;
            }

            set {
                _plateSolveResult = value;
                if (value != null) {
                    var existingItem = PlateSolveResultList.FirstOrDefault(x => x.SolveTime == value.SolveTime);
                    if (existingItem != null) {
                        //In case an existing item is set again
                        var index = PlateSolveResultList.IndexOf(existingItem);
                        PlateSolveResultList[index] = existingItem;
                    } else {
                        PlateSolveResultList.Add(value);
                    }
                }
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<PlateSolveResult> PlateSolveResultList {
            get {
                if (_plateSolveResultList == null) {
                    _plateSolveResultList = new ObservableCollection<PlateSolveResult>();
                }
                return _plateSolveResultList;
            }
            set {
                _plateSolveResultList = value;
                RaisePropertyChanged();
            }
        }

        public double RepeatThreshold {
            get {
                return _repeatThreshold;
            }
            set {
                _repeatThreshold = value;
                RaisePropertyChanged();
            }
        }

        public bool Sync {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.Sync;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.Sync = value;
                RaisePropertyChanged();
            }
        }

        public bool SlewToTarget {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.SlewToTarget;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.SlewToTarget = value;
                if (value) {
                    Sync = true;
                }
                RaisePropertyChanged();
            }
        }

        public BinningMode SnapBin {
            get {
                return _snapBin;
            }

            set {
                _snapBin = value;
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

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }

            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        public int SnapGain {
            get {
                return _snapGain;
            }

            set {
                _snapGain = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand SolveCommand { get; private set; }

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

        public TelescopeInfo TelescopeInfo {
            get {
                return telescopeInfo ?? DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
            }
            private set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
            this.telescopeMediator.RemoveConsumer(this);
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            this.CameraInfo = cameraInfo;
        }

        public void UpdateDeviceInfo(TelescopeInfo telescopeInfo) {
            this.TelescopeInfo = telescopeInfo;
        }

        private void CancelSolve(object o) {
            _solveCancelToken?.Cancel();
        }

        private async Task<bool> CaptureSolveSyncAndReslew(IProgress<ApplicationStatus> progress) {
            _solveCancelToken?.Dispose();
            _solveCancelToken = new CancellationTokenSource();
            try {
                if ((this.Sync || this.SlewToTarget) && !telescopeInfo.Connected) {
                    throw new Exception(Locale.Loc.Instance["LblTelescopeNotConnected"]);
                }

                var seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
                seq.Gain = SnapGain;

                var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
                var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);
                var solveProgress = new Progress<PlateSolveProgress>(x => {
                    if (x.PlateSolveResult != null) {
                        PlateSolveResult = x.PlateSolveResult;
                    }
                });

                if (this.SlewToTarget) {
                    var solver = new CenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator);
                    var parameter = new CenterSolveParameter() {
                        Attempts = 1,
                        Binning = SnapBin?.X ?? CameraInfo.BinX,
                        Coordinates = telescopeMediator.GetCurrentPosition(),
                        DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                        FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                        MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                        PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                        ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                        Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                        SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                        Threshold = RepeatThreshold,
                        NoSync = profileService.ActiveProfile.TelescopeSettings.NoSync
                    };
                    _ = await solver.Center(seq, parameter, solveProgress, progress, _solveCancelToken.Token);
                } else {
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
                        SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                        Coordinates = telescopeMediator.GetCurrentPosition()
                    };
                    var result = await solver.Solve(seq, parameter, solveProgress, progress, _solveCancelToken.Token);

                    if (telescopeInfo.Connected) {
                        var position = parameter.Coordinates.Transform(result.Coordinates.Epoch);
                        result.Separation = result.DetermineSeparation(position);
                    }

                    if (!profileService.ActiveProfile.TelescopeSettings.NoSync && Sync) {
                        await telescopeMediator.Sync(result.Coordinates);
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }

            return true;
        }
    }
}