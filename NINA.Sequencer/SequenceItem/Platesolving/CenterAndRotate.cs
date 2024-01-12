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
using NINA.Core.Utility;
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
using NINA.Core.Locale;
using NINA.Equipment.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility.Notification;
using NINA.PlateSolving.Interfaces;
using NINA.Equipment.Interfaces;

namespace NINA.Sequencer.SequenceItem.Platesolving {

    [ExportMetadata("Name", "Lbl_SequenceItem_Platesolving_CenterAndRotate_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Platesolving_CenterAndRotate_Description")]
    [ExportMetadata("Icon", "PlatesolveAndRotateSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class CenterAndRotate : Center {
        private IRotatorMediator rotatorMediator;

        [ImportingConstructor]
        public CenterAndRotate(IProfileService profileService,
                               ITelescopeMediator telescopeMediator,
                               IImagingMediator imagingMediator,
                               IRotatorMediator rotatorMediator,
                               IFilterWheelMediator filterWheelMediator,
                               IGuiderMediator guiderMediator,
                               IDomeMediator domeMediator,
                               IDomeFollower domeFollower,
                               IPlateSolverFactory plateSolverFactory,
                               IWindowServiceFactory windowServiceFactory) : base(profileService,
                                                                        telescopeMediator,
                                                                        imagingMediator,
                                                                        filterWheelMediator,
                                                                        guiderMediator,
                                                                        domeMediator,
                                                                        domeFollower,
                                                                        plateSolverFactory,
                                                                        windowServiceFactory) {
            this.rotatorMediator = rotatorMediator;
        }

        private CenterAndRotate(CenterAndRotate cloneMe) : this(cloneMe.profileService,
                                                                cloneMe.telescopeMediator,
                                                                cloneMe.imagingMediator,
                                                                cloneMe.rotatorMediator,
                                                                cloneMe.filterWheelMediator,
                                                                cloneMe.guiderMediator,
                                                                cloneMe.domeMediator,
                                                                cloneMe.domeFollower,
                                                                cloneMe.plateSolverFactory,
                                                                cloneMe.windowServiceFactory) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new CenterAndRotate(this) {
                Coordinates = Coordinates?.Clone(),
                PositionAngle = PositionAngle
            };
        }

        /// <summary>
        /// Backwards compatibility property that will migrate to position angle
        /// </summary>
        [JsonProperty(propertyName: "Rotation")]
        public double DeprecatedRotation { set => PositionAngle = 360 - value; }

        private double positionAngle = 0;
        [JsonProperty]
        public double PositionAngle {
            get => positionAngle;
            set {
                positionAngle = AstroUtil.EuclidianModulus(value, 360);
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (telescopeMediator.GetInfo().AtPark) {
                Notification.ShowError(Loc.Instance["LblTelescopeParkedWarning"]);
                throw new SequenceEntityFailedException(Loc.Instance["LblTelescopeParkedWarning"]);
            }
            var service = windowServiceFactory.Create();
            progress = PlateSolveStatusVM.CreateLinkedProgress(progress);
            service.Show(PlateSolveStatusVM, Loc.Instance["Lbl_SequenceItem_Platesolving_CenterAndRotate_Name"], System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);

            bool stoppedGuiding = false;
            try {
                float rotationDistance = float.MaxValue;

                stoppedGuiding = await guiderMediator.StopGuiding(token);
                progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblSlew"] });
                await telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates, token);

                var domeInfo = domeMediator.GetInfo();
                if (domeInfo.Connected && domeInfo.CanSetAzimuth && !domeFollower.IsFollowing) {
                    progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSynchronizingDome"] });
                    Logger.Info($"Center and Rotate - Synchronize dome to scope since dome following is not enabled");
                    if (!await domeFollower.TriggerTelescopeSync()) {
                        Notification.ShowWarning(Loc.Instance["LblDomeSyncFailureDuringCentering"]);
                        Logger.Warning("Center and Rotate - Synchronize dome operation didn't complete successfully. Moving on");
                    }
                }
                progress?.Report(new ApplicationStatus() { Status = string.Empty });

                var targetRotation = (float)PositionAngle;

                /* Loop until the rotation is within tolerances*/
                do {
                    var solveResult = await Solve(progress, token);
                    if (!solveResult.Success) {
                        throw new SequenceEntityFailedException(Loc.Instance["LblPlatesolveFailed"]);
                    }

                    var orientation = (float)solveResult.PositionAngle;
                    rotatorMediator.Sync(orientation);

                    var prevTargetRotation = targetRotation;
                    targetRotation = rotatorMediator.GetTargetPosition(prevTargetRotation);
                    if (Math.Abs(targetRotation - prevTargetRotation) > 0.1) {
                        Logger.Info($"Rotator target position {PositionAngle} adjusted to {targetRotation} to be within the allowed mechanical range");
                        Notification.ShowInformation(string.Format(Loc.Instance["LblRotatorRangeAdjusted"], targetRotation));
                    }

                    rotationDistance = targetRotation - orientation;
                    if (profileService.ActiveProfile.RotatorSettings.RangeType == Core.Enum.RotatorRangeTypeEnum.FULL) {
                        // If the full rotation range is allowed, then consider the 180-degree rotated orientation as well in case it is closer
                        var movement = AstroUtil.EuclidianModulus(rotationDistance, 180);
                        var movement2 = movement - 180;

                        if (movement < Math.Abs(movement2)) {
                            rotationDistance = movement;
                        } else {
                            targetRotation = AstroUtil.EuclidianModulus(targetRotation + 180, 360);
                            Logger.Info($"Changing rotation target to {targetRotation} instead since it is closer to the current position");
                            rotationDistance = movement2;
                        }
                    }

                    if (!Angle.ByDegree(rotationDistance).Equals(Angle.Zero, Angle.ByDegree(profileService.ActiveProfile.PlateSolveSettings.RotationTolerance), true)) {
                        Logger.Info($"Rotator not inside tolerance {profileService.ActiveProfile.PlateSolveSettings.RotationTolerance} - Current {orientation}° / Target: {PositionAngle}° - Moving rotator relatively by {rotationDistance}°"); 
                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblRotating"] });
                        await rotatorMediator.MoveRelative(rotationDistance, token);
                        progress?.Report(new ApplicationStatus() { Status = string.Empty });
                        token.ThrowIfCancellationRequested();
                    }
                } while (!Angle.ByDegree(rotationDistance).Equals(Angle.Zero, Angle.ByDegree(profileService.ActiveProfile.PlateSolveSettings.RotationTolerance), true));

                /* Once everything is in place do a centering of the object */
                var centerResult = await base.DoCenter(progress, token);

                if (!centerResult.Success) {
                    throw new Exception(Loc.Instance["LblPlatesolveFailed"]);
                }
            } finally {
                if (stoppedGuiding) {
                    try {
                        var restartedGuiding = await guiderMediator.StartGuiding(false, progress, token);
                        if (!restartedGuiding) {
                            Logger.Error("Failed to resume guiding after CenterAndRotate");
                        }
                    } catch (Exception e) {
                        Logger.Error("Failed to resume guiding after CenterAndRotate", e);
                    }
                }

                service.DelayedClose(TimeSpan.FromSeconds(10));
            }
        }

        private async Task<PlateSolveResult> Solve(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var plateSolver = plateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = plateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

            var solver = plateSolverFactory.GetCaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator);
            var parameter = new CaptureSolverParameter() {
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
                BlindFailoverEnabled = profileService.ActiveProfile.PlateSolveSettings.BlindFailoverEnabled
            };

            var seq = new CaptureSequence(
                profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                CaptureSequence.ImageTypes.SNAPSHOT,
                profileService.ActiveProfile.PlateSolveSettings.Filter,
                new BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                1
            );
            return await solver.Solve(seq, parameter, PlateSolveStatusVM.Progress, progress, token);
        }

        public override void AfterParentChanged() {
            var contextCoordinates = ItemUtility.RetrieveContextCoordinates(this.Parent);
            if (contextCoordinates != null) {
                Coordinates.Coordinates = contextCoordinates.Coordinates;
                PositionAngle = contextCoordinates.PositionAngle;
                Inherited = true;
            } else {
                Inherited = false;
            }
            Validate();
        }

        public override bool Validate() {
            var i = new List<string>();
            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            if (!rotatorMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblRotatorNotConnected"]);
            }
            Issues = i;
            return Issues.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(CenterAndRotate)}, Coordinates {Coordinates?.Coordinates}, Position Angle: {PositionAngle}°";
        }
    }
}