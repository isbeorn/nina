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
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Equipment.Interfaces;

namespace NINA.Sequencer.Trigger.Dome {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_SynchronizeDomeTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_SynchronizeDomeTrigger_Description")]
    [ExportMetadata("Icon", "LoopSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Dome")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SynchronizeDomeTrigger : SequenceTrigger, IValidatable {
        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;
        private IDomeMediator domeMediator;
        private IDomeFollower domeFollower;
        private IApplicationStatusMediator applicationStatusMediator;

        [ImportingConstructor]
        public SynchronizeDomeTrigger(
            IProfileService profileService, ITelescopeMediator telescopeMediator, IDomeMediator domeMediator, IDomeFollower domeFollower, IApplicationStatusMediator applicationStatusMediator) : base() {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.applicationStatusMediator = applicationStatusMediator;
        }

        private SynchronizeDomeTrigger(SynchronizeDomeTrigger cloneMe) : this(cloneMe.profileService, cloneMe.telescopeMediator, cloneMe.domeMediator, cloneMe.domeFollower, cloneMe.applicationStatusMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SynchronizeDomeTrigger(this) {
                TriggerRunner = (SequentialContainer)TriggerRunner.Clone(),
                CurrentAzimuth = this.CurrentAzimuth,
                TargetAzimuth = this.TargetAzimuth,
                TargetAltitude = this.TargetAltitude
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

        private double currentAzimuth;

        public double CurrentAzimuth {
            get => currentAzimuth;
            set {
                currentAzimuth = value;
                RaisePropertyChanged();
            }
        }

        private double targetAzimuth;

        public double TargetAzimuth {
            get => targetAzimuth;
            set {
                targetAzimuth = value;
                RaisePropertyChanged();
            }
        }

        private double targetAltitude;

        public double TargetAltitude {
            get => targetAltitude;
            set {
                targetAltitude = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            await domeMediator.SlewToAzimuth(this.TargetAzimuth, token);
            this.CurrentAzimuth = this.domeMediator.GetInfo().Azimuth;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            try { 
                if (this.telescopeMediator.GetInfo().AtPark) {
                    Logger.Warning("Telescope is parked, so Synchronize Dome will not be triggered");
                    return false;
                }
                if (this.domeFollower.IsFollowing) {
                    Logger.Warning("Cannot synchronize dome via trigger since Dome Following is enabled");
                    return false;
                }

                var calculatedTargetDomeCoordinates = this.domeFollower.GetSynchronizedDomeCoordinates(this.telescopeMediator.GetInfo());
                if(calculatedTargetDomeCoordinates != null) { 
                    this.TargetAltitude = calculatedTargetDomeCoordinates.Altitude.Degree;
                    this.TargetAzimuth = calculatedTargetDomeCoordinates.Azimuth.Degree;
                    this.CurrentAzimuth = this.domeMediator.GetInfo().Azimuth;
                    var withinTolerance = domeFollower.IsDomeWithinTolerance(Angle.ByDegree(this.CurrentAzimuth), calculatedTargetDomeCoordinates);
                    if (!withinTolerance) {
                        Logger.Info($"SynchronizeDome: Outside of tolerance. Current Azimuth {this.CurrentAzimuth}, Target Azimuth {this.TargetAzimuth}, Target Altitude {this.TargetAltitude}");
                        return true;
                    }
                }
            } catch(Exception ex) {
                Logger.Error(ex);
            }
            return false;
        }

        public override string ToString() {
            return $"Trigger: {nameof(SynchronizeDomeTrigger)}";
        }

        public bool Validate() {
            var i = new List<string>();
            var domeInfo = domeMediator.GetInfo();
            var telescopeInfo = telescopeMediator.GetInfo();
            if (!domeInfo.Connected) {
                i.Add(Loc.Instance["LblDomeNotConnected"]);
            }
            if (domeInfo.Connected && !domeInfo.CanSetAzimuth) {
                i.Add(Loc.Instance["LblDomeCannotSetAzimuth"]);
            }
            if (!telescopeInfo.Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            if (domeFollower.IsFollowing) {
                i.Add(Loc.Instance["LblDomeFollowerEnabledError"]);
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}