#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Equipment.Interfaces;

namespace NINA.ViewModel.Imaging {

    internal class AnchorablePlateSolverVM : DockableVM, ICameraConsumer, ITelescopeConsumer, IAnchorablePlateSolverVM {
        private PlateSolveResult _plateSolveResult;

        private ObservableCollection<PlateSolveResult> _plateSolveResultList;

        private double _repeatThreshold;

        private BinningMode _snapBin;

        private double _snapExposureDuration;

        private FilterInfo _snapFilter;

        private int _snapGain = -1;

        private CancellationTokenSource _solveCancelToken;

        private ApplicationStatus _status;

        private IApplicationStatusMediator applicationStatusMediator;
        private IFilterWheelMediator filterWheelMediator;
        private CameraInfo cameraInfo;

        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;

        private TelescopeInfo telescopeInfo;

        private ITelescopeMediator telescopeMediator;
        private IDomeMediator domeMediator;
        private IDomeFollower domeFollower;

        public AnchorablePlateSolverVM(IProfileService profileService,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IDomeMediator domeMediator,
                IDomeFollower domeFollower,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator,
                IFilterWheelMediator filterWheelMediator) : base(profileService) {
            Title = Loc.Instance["LblPlateSolving"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.filterWheelMediator = filterWheelMediator;

            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];

            SolveCommand = new AsyncCommand<bool>(async () => {
                cameraMediator.RegisterCaptureBlock(this);
                try {
                    var result = await CaptureSolveSyncAndReslew(new Progress<ApplicationStatus>(p => Status = p));
                    return result;
                } finally {
                    cameraMediator.ReleaseCaptureBlock(this);
                }
            },
            (o) => cameraMediator.IsFreeToCapture(this));
            CancelSolveCommand = new RelayCommand(CancelSolve);

            SnapExposureDuration = profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
            SnapFilter = profileService.ActiveProfile.PlateSolveSettings.Filter;
            RepeatThreshold = profileService.ActiveProfile.PlateSolveSettings.Threshold;
            SlewToTarget = profileService.ActiveProfile.PlateSolveSettings.SlewToTarget;
            SnapBin = new BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning);
            SnapGain = profileService.ActiveProfile.PlateSolveSettings.Gain;

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                SnapExposureDuration = profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
                SnapFilter = profileService.ActiveProfile.PlateSolveSettings.Filter;
                RepeatThreshold = profileService.ActiveProfile.PlateSolveSettings.Threshold;
                SlewToTarget = profileService.ActiveProfile.PlateSolveSettings.SlewToTarget;
                SnapBin = new BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning);
                SnapGain = profileService.ActiveProfile.PlateSolveSettings.Gain;
            };
        }

        public CameraInfo CameraInfo {
            get => cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public ICommand CancelSolveCommand { get; private set; }

        public new string ContentId =>
                //Backwards compatibility for avalondock layouts prior to 1.10
                "PlatesolveVM";

        public PlateSolveResult PlateSolveResult {
            get => _plateSolveResult;

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
            get => _repeatThreshold;
            set {
                _repeatThreshold = value;
                RaisePropertyChanged();
            }
        }

        public bool Sync {
            get => profileService.ActiveProfile.PlateSolveSettings.Sync;
            set {
                profileService.ActiveProfile.PlateSolveSettings.Sync = value;
                RaisePropertyChanged();
            }
        }

        public bool SlewToTarget {
            get => profileService.ActiveProfile.PlateSolveSettings.SlewToTarget;
            set {
                profileService.ActiveProfile.PlateSolveSettings.SlewToTarget = value;
                if (value) {
                    Sync = true;
                }
                RaisePropertyChanged();
            }
        }

        public BinningMode SnapBin {
            get => _snapBin;

            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        public double SnapExposureDuration {
            get => _snapExposureDuration;

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo SnapFilter {
            get => _snapFilter;

            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        public int SnapGain {
            get => _snapGain;

            set {
                _snapGain = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand SolveCommand { get; private set; }

        public ApplicationStatus Status {
            get => _status;
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public TelescopeInfo TelescopeInfo {
            get => telescopeInfo ?? DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
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
            try { _solveCancelToken?.Cancel(); } catch { }
        }

        private async Task<bool> CaptureSolveSyncAndReslew(IProgress<ApplicationStatus> progress) {
            _solveCancelToken?.Dispose();
            _solveCancelToken = new CancellationTokenSource();
            try {
                if ((this.Sync || this.SlewToTarget) && !telescopeInfo.Connected) {
                    throw new Exception(Loc.Instance["LblTelescopeNotConnected"]);
                }

                await telescopeMediator.WaitForSlew(_solveCancelToken.Token);
                await domeMediator.WaitForDomeSynchronization(_solveCancelToken.Token);

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
                    var solver = new CenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator, filterWheelMediator, domeMediator, domeFollower);
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
                        NoSync = profileService.ActiveProfile.TelescopeSettings.NoSync,
                        BlindFailoverEnabled = profileService.ActiveProfile.PlateSolveSettings.BlindFailoverEnabled
                    };
                    _ = await solver.Center(seq, parameter, solveProgress, progress, _solveCancelToken.Token);
                } else {
                    var solver = new CaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator);
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
                        Coordinates = telescopeMediator.GetCurrentPosition(),
                        BlindFailoverEnabled = profileService.ActiveProfile.PlateSolveSettings.BlindFailoverEnabled
                    };
                    var result = await solver.Solve(seq, parameter, solveProgress, progress, _solveCancelToken.Token);
                    if (result.Success) {
                        if (telescopeInfo.Connected) {
                            var epoch = telescopeInfo.EquatorialSystem;
                            var resultCoordinates = result.Coordinates.Transform(epoch);
                            var position = parameter.Coordinates.Transform(epoch);
                            result.Separation = position - resultCoordinates;

                            if (!profileService.ActiveProfile.TelescopeSettings.NoSync && Sync) {
                                await telescopeMediator.Sync(resultCoordinates);
                            }
                        }
                    } else {
                        Notification.ShowError(Loc.Instance["LblPlatesolveFailed"]);
                        return false;
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                return false;
            }

            return true;
        }
    }
}