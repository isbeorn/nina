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
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger.Autofocus {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_AutofocusAfterTimeTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_AutofocusAfterTimeTrigger_Description")]
    [ExportMetadata("Icon", "AutoFocusAfterTimeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AutofocusAfterTimeTrigger : SequenceTrigger, IValidatable {
        private IProfileService profileService;
        private IImageHistoryVM history;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IAutoFocusVMFactory autoFocusVMFactory;
        private readonly ISafetyMonitorMediator safetyMonitorMediator;
        private DateTime initialTime;
        private bool initialized = false;

        [ImportingConstructor]
        public AutofocusAfterTimeTrigger(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IAutoFocusVMFactory autoFocusVMFactory, ISafetyMonitorMediator safetyMonitorMediator) : base() {
            this.history = history;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.autoFocusVMFactory = autoFocusVMFactory;
            this.safetyMonitorMediator = safetyMonitorMediator;
            Amount = 30;
            TriggerRunner.Add(new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory));
        }

        private AutofocusAfterTimeTrigger(AutofocusAfterTimeTrigger cloneMe) : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory, cloneMe.safetyMonitorMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new AutofocusAfterTimeTrigger(this) {
                Amount = Amount,
                TriggerRunner = (SequentialContainer)TriggerRunner.Clone()
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

        private double amount;

        [JsonProperty]
        public double Amount {
            get => amount;
            set {
                amount = value;
                RaisePropertyChanged();
            }
        }

        private double elapsed;

        public double Elapsed {
            get => elapsed;
            private set {
                elapsed = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            await TriggerRunner.Run(progress, token);
        }

        public override void SequenceBlockInitialize() {
            if (!initialized) {
                initialTime = DateTime.UtcNow;
                initialized = true;
            }
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (nextItem == null) { return false; }
            if (!(nextItem is IExposureItem exposureItem)) { return false; }
            if (exposureItem.ImageType != "LIGHT") { return false; }
            var safety = safetyMonitorMediator.GetInfo();
            if (safety != null && safety.Connected && !safety.IsSafe) { return false; }

            bool shouldTrigger = false;
            var lastAF = history.AutoFocusPoints.LastOrDefault();
            if (lastAF == null) {
                Elapsed = Math.Round((DateTime.UtcNow - initialTime).TotalMinutes, 2);
                shouldTrigger = (DateTime.UtcNow - initialTime) >= TimeSpan.FromMinutes(Amount);
            } else {
                Elapsed = Math.Round((DateTime.UtcNow - lastAF.AutoFocusPoint.Time.ToUniversalTime()).TotalMinutes, 2);
                shouldTrigger = (DateTime.UtcNow - lastAF.AutoFocusPoint.Time.ToUniversalTime()) >= TimeSpan.FromMinutes(Amount);
            }

            if (shouldTrigger) {
                if (ItemUtility.IsTooCloseToMeridianFlip(Parent, TriggerRunner.GetItemsSnapshot().First().GetEstimatedDuration() + nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero)) {
                    Logger.Warning("Autofocus should be triggered, however the meridian flip is too close to be executed");
                    shouldTrigger = false;
                }
            }
            return shouldTrigger;
        }

        public override string ToString() {
            return $"Trigger: {nameof(AutofocusAfterTimeTrigger)}, Amount: {Amount}m";
        }

        public bool Validate() {
            var i = new List<string>();
            var cameraInfo = cameraMediator.GetInfo();
            var focuserInfo = focuserMediator.GetInfo();

            if (!cameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            }
            if (!focuserInfo.Connected) {
                i.Add(Loc.Instance["LblFocuserNotConnected"]);
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}