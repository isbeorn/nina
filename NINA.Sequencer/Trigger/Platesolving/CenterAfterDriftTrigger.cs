#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Com.DriverAccess;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger.Platesolving {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_CenterAfterDriftTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_CenterAfterDriftTrigger_Description")]
    [ExportMetadata("Icon", "TargetWithArrowSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class CenterAfterDriftTrigger : SequenceTrigger, IValidatable {
        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IGuiderMediator guiderMediator;
        private IImagingMediator imagingMediator;
        private ICameraMediator cameraMediator;
        private IDomeMediator domeMediator;
        private IDomeFollower domeFollower;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly ISafetyMonitorMediator safetyMonitorMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private PlatesolvingImageFollower platesolvingImageFollower;
        private PlateSolvingStatusVM plateSolveStatusVM = new PlateSolvingStatusVM();

        [ImportingConstructor]
        public CenterAfterDriftTrigger(
            IProfileService profileService, ITelescopeMediator telescopeMediator, IFilterWheelMediator filterWheelMediator, IGuiderMediator guiderMediator,
            IImagingMediator imagingMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator, IDomeFollower domeFollower, IImageSaveMediator imageSaveMediator,
            IApplicationStatusMediator applicationStatusMediator, ISafetyMonitorMediator safetyMonitorMediator) : base() {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.cameraMediator = cameraMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.imageSaveMediator = imageSaveMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.safetyMonitorMediator = safetyMonitorMediator;
            DistanceArcMinutes = 10;
            AfterExposures = 1;
            Coordinates = new InputCoordinates();
        }

        private CenterAfterDriftTrigger(CenterAfterDriftTrigger cloneMe) : this(cloneMe.profileService, cloneMe.telescopeMediator, cloneMe.filterWheelMediator, cloneMe.guiderMediator, cloneMe.imagingMediator, cloneMe.cameraMediator, cloneMe.domeMediator, cloneMe.domeFollower, cloneMe.imageSaveMediator, cloneMe.applicationStatusMediator, cloneMe.safetyMonitorMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new CenterAfterDriftTrigger(this) {
                TriggerRunner = (SequentialContainer)TriggerRunner.Clone(),
                DistanceArcMinutes = DistanceArcMinutes,
                AfterExposures = AfterExposures,
                Coordinates = Coordinates?.Clone()
            };
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public InputCoordinates Coordinates { get; set; }

        private double distanceArcMinutes;

        [JsonProperty]
        public double DistanceArcMinutes {
            get => distanceArcMinutes;
            set {
                if (value > 0.0 && value != distanceArcMinutes) {
                    distanceArcMinutes = value;
                    RaisePropertyChanged(nameof(DistanceArcMinutes));
                    RaisePropertyChanged(nameof(DistancePixels));
                }
            }
        }

        private bool inherited;

        public bool Inherited {
            get => inherited;
            set {
                inherited = value;
                RaisePropertyChanged();
            }
        }

        public double DistancePixels {
            get {
                var arcsecPerPix = AstroUtil.ArcsecPerPixel(profileService.ActiveProfile.CameraSettings.PixelSize, profileService.ActiveProfile.TelescopeSettings.FocalLength);
                return DistanceArcMinutes * 60d / arcsecPerPix;
            }
        }

        private double lastDistanceArcMinutes = 0.0;

        public double LastDistanceArcMinutes {
            get => lastDistanceArcMinutes;
            private set {
                lastDistanceArcMinutes = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var centerSequenceItem = new Center(profileService, telescopeMediator, imagingMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, new PlateSolverFactoryProxy(), new WindowServiceFactory()) {
                Coordinates = Coordinates
            };
            await centerSequenceItem.Execute(progress, token);
            LastDistanceArcMinutes = 0.0;
            platesolvingImageFollower.LastCoordinates = null;
        }

        private int afterExposures;

        [JsonProperty]
        public int AfterExposures {
            get => afterExposures;
            set {
                afterExposures = value;
                if (platesolvingImageFollower != null) {
                    platesolvingImageFollower.AfterExposures = value;
                }
                RaisePropertyChanged();
            }
        }

        private void PlatesolvingImageFollower_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            var follower = (PlatesolvingImageFollower)sender;
            if (e.PropertyName == nameof(follower.LastCoordinates)) {
                var lastCoordinates = follower?.LastCoordinates;
                if (lastCoordinates == null) {
                    return;
                }

                if (Coordinates.Coordinates != null) {
                    var separation = lastCoordinates - Coordinates.Coordinates;
                    LastDistanceArcMinutes = separation.Distance.ArcMinutes;
                    Logger.Info($"Drift: {LastDistanceArcMinutes} / {DistanceArcMinutes} arc minutes");
                }
            }

            if (e.PropertyName == nameof(follower.ProgressExposures)) {
                RaisePropertyChanged(nameof(ProgressExposures));
            }
        }

        public int ProgressExposures {
            get {
                if (platesolvingImageFollower == null) {
                    return 0;
                }
                return platesolvingImageFollower.ProgressExposures;
            }
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            RaisePropertyChanged(nameof(ProgressExposures));
            if (nextItem == null) { return false; }
            if (!(nextItem is IExposureItem exposureItem)) { return false; }
            if (exposureItem.ImageType != "LIGHT") { return false; }
            if (safetyMonitorMediator.GetInfo() is { Connected: true, IsSafe: false }) { return false; }

            if (LastDistanceArcMinutes >= DistanceArcMinutes) {
                Logger.Info($"Drift exceeded threshold: {LastDistanceArcMinutes} / {DistanceArcMinutes} arc minutes");
                if (ItemUtility.IsTooCloseToMeridianFlip(Parent, TimeSpan.FromMinutes(1) + nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero)) {
                    Logger.Warning("CenterAfterDrift should be triggered, however the meridian flip is too close to be executed");
                    return false;
                }                
                Notification.ShowInformation(Loc.Instance["LblCenterAfterDrift"]);
                return true;
            }
            return false;
        }

        public override void SequenceBlockInitialize() {
            EnsureFollowerClosed();
            platesolvingImageFollower = new PlatesolvingImageFollower(this.profileService, this.telescopeMediator, this.imageSaveMediator, this.applicationStatusMediator) {
                AfterExposures = AfterExposures
            };
            platesolvingImageFollower.PropertyChanged += PlatesolvingImageFollower_PropertyChanged;
        }

        public override void SequenceBlockTeardown() {
            EnsureFollowerClosed();
        }

        private void EnsureFollowerClosed() {
            if (platesolvingImageFollower != null) {
                platesolvingImageFollower.PropertyChanged -= PlatesolvingImageFollower_PropertyChanged;
            }
            platesolvingImageFollower?.Dispose();
            platesolvingImageFollower = null;
        }

        public override void AfterParentChanged() {
            if (Parent == null) {
                SequenceBlockTeardown();
            } else {
                var contextCoordinates = ItemUtility.RetrieveContextCoordinates(this.Parent);
                if (contextCoordinates != null) {
                    Coordinates.Coordinates = contextCoordinates.Coordinates;
                    Inherited = true;
                } else {
                    Inherited = false;
                }
                Validate();
                if (Parent.Status == SequenceEntityStatus.RUNNING) {
                    SequenceBlockInitialize();
                }
            }
        }

        public override string ToString() {
            return $"Trigger: {nameof(CenterAfterDriftTrigger)}, DistanceArcMinutes: {DistanceArcMinutes}";
        }

        public bool Validate() {
            var i = new List<string>();
            var cameraInfo = cameraMediator.GetInfo();
            var telescopeInfo = telescopeMediator.GetInfo();
            if (!cameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            }
            if (!telescopeInfo.Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            if (!Inherited) {
                i.Add(Loc.Instance["LblNoTarget"]);
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}