#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger.Platesolving {

    public class PlatesolvingImageFollower : BaseINPC, IDisposable {
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IProfileService profileService;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IProgress<ApplicationStatus> progress;
        private object lockObj = new object();
        private bool closed = false;
        private Task solverBackgroundTask;
        private CancellationTokenSource solverBackgroundTaskCancellationSource = new CancellationTokenSource();

        public PlatesolvingImageFollower(IProfileService profileService, ITelescopeMediator telescopeMediator, IImageSaveMediator imageSaveMediator, IApplicationStatusMediator applicationStatusMediator) {
            this.profileService = profileService;
            this.imageSaveMediator = imageSaveMediator;
            this.telescopeMediator = telescopeMediator;
            this.imageSaveMediator.BeforeImageSaved += ImageSaveMediator_BeforeImageSaved;
            this.applicationStatusMediator = applicationStatusMediator;
            this.progress = new Progress<ApplicationStatus>(ProgressStatusUpdate);
        }

        private void ProgressStatusUpdate(ApplicationStatus status) {
            if (string.IsNullOrWhiteSpace(status.Source)) {
                status.Source = Loc.Instance["Lbl_SequenceTrigger_CenterAfterDriftTrigger_Name"];
            }
            applicationStatusMediator.StatusUpdate(status);
        }

        private async Task ImageSaveMediator_BeforeImageSaved(object sender, BeforeImageSavedEventArgs e) {
            lock (lockObj) {
                if(e.Image.MetaData.Image.ImageType != "LIGHT") {
                    return;
                }
                ProgressExposures++;
                if (ProgressExposures < AfterExposures) {
                    return;
                }

                if (solverBackgroundTask != null && !solverBackgroundTask.IsCompleted) {
                    Logger.Info($"Won't platesolve image because another operation is already in progress");
                    return;
                }

                solverBackgroundTask = Task.Run(async () => {
                    await SolveLastImage(e.Image, solverBackgroundTaskCancellationSource.Token);                    
                });
            }
        }

        public void Dispose() {
            lock (lockObj) {
                if (!closed) {
                    this.imageSaveMediator.BeforeImageSaved -= ImageSaveMediator_BeforeImageSaved;
                    try {
                        solverBackgroundTaskCancellationSource?.Cancel();
                    } catch { }
                    closed = true;
                }
            }
        }

        private int afterExposures = 1;

        public int AfterExposures {
            get => afterExposures;
            set {
                afterExposures = value;
                RaisePropertyChanged();
            }
        }
        private int progressExposures = 0;

        public int ProgressExposures {
            get => progressExposures;
            set {
                progressExposures = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates lastCoordinates;

        public Coordinates LastCoordinates {
            get => lastCoordinates;
            set {
                lastCoordinates = value;
                RaisePropertyChanged();
            }
        }

        private async Task SolveLastImage(IImageData loadedImage, CancellationToken token) {
            var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

            var solver = new ImageSolver(plateSolver, blindSolver);

            //Take coordinates from image header - if not available take them from the telescope
            Coordinates coordinates = loadedImage.MetaData.Telescope.Coordinates;
            if (coordinates == null && telescopeMediator.GetInfo().Connected) {
                coordinates = telescopeMediator.GetCurrentPosition();
            }

            var parameter = new PlateSolveParameter() {
                Coordinates = coordinates,
                Binning = loadedImage.MetaData.Camera.BinX,
                DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                DisableNotifications = true,
                BlindFailoverEnabled = profileService.ActiveProfile.PlateSolveSettings.BlindFailoverEnabled
            };
            var solveResult = await solver.Solve(loadedImage, parameter, this.progress, token);
            if (!solveResult.Success) {
                Notification.ShowWarning(Loc.Instance["LblPlatesolveFailed"]);
                return;
            }

            LastCoordinates = solveResult.Coordinates;
            ProgressExposures = 0;
        }
    }
}