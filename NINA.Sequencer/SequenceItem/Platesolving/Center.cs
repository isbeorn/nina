#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.WindowService;
using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using NINA.WPF.Base.ViewModel;
using NINA.PlateSolving.Interfaces;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Platesolving {

    [ExportMetadata("Name", "Lbl_SequenceItem_Platesolving_Center_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Platesolving_Center_Description")]
    [ExportMetadata("Icon", "PlatesolveSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class Center : CoordinatesInstruction, IValidatable {
        protected IProfileService profileService;
        protected ITelescopeMediator telescopeMediator;
        protected IImagingMediator imagingMediator;
        protected IFilterWheelMediator filterWheelMediator;
        protected IGuiderMediator guiderMediator;
        protected IDomeMediator domeMediator;
        protected IDomeFollower domeFollower;
        protected IPlateSolverFactory plateSolverFactory;
        protected IWindowServiceFactory windowServiceFactory;
        protected ISymbolBrokerVM symbolBroker;
        public PlateSolvingStatusVM PlateSolveStatusVM { get; } = new PlateSolvingStatusVM();

        [ImportingConstructor]
        public Center(IProfileService profileService,
                      ITelescopeMediator telescopeMediator,
                      IImagingMediator imagingMediator,
                      IFilterWheelMediator filterWheelMediator,
                      IGuiderMediator guiderMediator,
                      IDomeMediator domeMediator,
                      IDomeFollower domeFollower,
                      IPlateSolverFactory plateSolverFactory,
                      IWindowServiceFactory windowServiceFactory,
                      ISymbolBrokerVM symbolBroker) :base(symbolBroker) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.imagingMediator = imagingMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.guiderMediator = guiderMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.plateSolverFactory = plateSolverFactory;
            this.windowServiceFactory = windowServiceFactory;
            this.symbolBroker = symbolBroker;
            Coordinates = new InputCoordinates();
        }

        private Center(Center cloneMe) : this(cloneMe.profileService,
                                              cloneMe.telescopeMediator,
                                              cloneMe.imagingMediator,
                                              cloneMe.filterWheelMediator,
                                              cloneMe.guiderMediator,
                                              cloneMe.domeMediator,
                                              cloneMe.domeFollower,
                                              cloneMe.plateSolverFactory,
                                              cloneMe.windowServiceFactory,
                                              cloneMe.symbolBroker) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            Center clone = new Center(this);
            UpdateExpressions(clone, this);
            return clone;
        }

        protected virtual async Task<PlateSolveResult> DoCenter(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (telescopeMediator.GetInfo().AtPark) {
                Notification.ShowError(Loc.Instance["LblTelescopeParkedWarning"]);
                throw new SequenceEntityFailedException(Loc.Instance["LblTelescopeParkedWarning"]);
            }
            progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblSlew"] });
            await telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates, token);            

            var domeInfo = domeMediator.GetInfo();
            if (domeInfo.Connected && domeInfo.CanSetAzimuth && !domeFollower.IsFollowing) {
                progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSynchronizingDome"] });
                Logger.Info($"Centering Solver - Synchronize dome to scope since dome following is not enabled");
                if (!await domeFollower.TriggerTelescopeSync()) {
                    Notification.ShowWarning(Loc.Instance["LblDomeSyncFailureDuringCentering"]);
                    Logger.Warning("Centering Solver - Synchronize dome operation didn't complete successfully. Moving on");
                }
            }
            progress?.Report(new ApplicationStatus() { Status = string.Empty });

            var plateSolver = plateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = plateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

            var solver = plateSolverFactory.GetCenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator, filterWheelMediator, domeMediator, domeFollower);
            var parameter = new CenterSolveParameter() {
                Attempts = profileService.ActiveProfile.PlateSolveSettings.NumberOfAttempts,
                Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                Coordinates = Coordinates?.Coordinates ?? telescopeMediator.GetCurrentPosition(),
                DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                Threshold = profileService.ActiveProfile.PlateSolveSettings.Threshold,
                NoSync = profileService.ActiveProfile.TelescopeSettings.NoSync,
                BlindFailoverEnabled = profileService.ActiveProfile.PlateSolveSettings.BlindFailoverEnabled
            };

            var seq = new CaptureSequence(
                profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                CaptureSequence.ImageTypes.SNAPSHOT,
                profileService.ActiveProfile.PlateSolveSettings.Filter,
                new BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                1
            );
            seq.Gain = profileService.ActiveProfile.PlateSolveSettings.Gain;
            return await solver.Center(seq, parameter, PlateSolveStatusVM.Progress, progress, token);
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var service = windowServiceFactory.Create();
            progress = PlateSolveStatusVM.CreateLinkedProgress(progress);
            service.Show(PlateSolveStatusVM, Loc.Instance["Lbl_SequenceItem_Platesolving_Center_Name"], System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
            try {
                var stoppedGuiding = await guiderMediator.StopGuiding(token);
                var result = await DoCenter(progress, token);
                if (stoppedGuiding) {
                    await guiderMediator.StartGuiding(false, progress, token);
                }
                if (result.Success == false) {
                    throw new SequenceEntityFailedException(Loc.Instance["LblPlatesolveFailed"]);
                }
            } finally {
                service.DelayedClose(TimeSpan.FromSeconds(10));
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        public bool Validate() {
            Issues.Clear();
            var info = telescopeMediator.GetInfo();
            if (!info.Connected) {
                Issues.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            Expression.ValidateExpressions(Issues, RaExpression, DecExpression);
            RaisePropertyChanged("Issues");
            return Issues.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(Center)}, Coordinates {Coordinates?.Coordinates}";
        }
    }
}