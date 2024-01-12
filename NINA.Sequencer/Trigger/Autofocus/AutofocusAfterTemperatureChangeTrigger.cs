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
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.Sequencer.Utility;
using NINA.Core.Utility;
using NINA.Sequencer.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.WPF.Base.Interfaces;

namespace NINA.Sequencer.Trigger.Autofocus {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_AutofocusAfterTemperatureChangeTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_AutofocusAfterTemperatureChangeTrigger_Description")]
    [ExportMetadata("Icon", "AutoFocusAfterTemperatureSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AutofocusAfterTemperatureChangeTrigger : SequenceTrigger, IValidatable {
        private IProfileService profileService;
        private IImageHistoryVM history;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IAutoFocusVMFactory autoFocusVMFactory;
        private double initialTemperature;

        [ImportingConstructor]
        public AutofocusAfterTemperatureChangeTrigger(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IAutoFocusVMFactory autoFocusVMFactory) : base() {
            this.history = history;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.autoFocusVMFactory = autoFocusVMFactory;
            Amount = 5;
            TriggerRunner.Add(new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory));
        }

        private AutofocusAfterTemperatureChangeTrigger(AutofocusAfterTemperatureChangeTrigger cloneMe) : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new AutofocusAfterTemperatureChangeTrigger(this) {
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

        private double deltaT;

        public double DeltaT {
            get => deltaT;
            set {
                deltaT = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            await TriggerRunner.Run(progress, token);
        }

        public override void Initialize() {
            initialTemperature = focuserMediator.GetInfo()?.Temperature ?? double.NaN;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (nextItem == null) { return false; }
            if (!(nextItem is IExposureItem exposureItem)) { return false; }
            if (exposureItem.ImageType != "LIGHT") { return false; }

            if (history.ImageHistory == null) { return false; }
            if (history.ImageHistory.Count == 0) { return false; }

            bool shouldTrigger = false;
            var lastAF = history.AutoFocusPoints.LastOrDefault();
            var info = focuserMediator.GetInfo();

            if (double.IsNaN(info?.Temperature ?? double.NaN)) {
                return false;
            }

            if (lastAF == null && double.IsNaN(initialTemperature)) {
                initialTemperature = info?.Temperature ?? double.NaN;
            }

            if (lastAF == null && !double.IsNaN(initialTemperature)) {
                DeltaT = Math.Round(Math.Abs(initialTemperature - info.Temperature), 2);
                shouldTrigger = Math.Abs(initialTemperature - info.Temperature) >= Amount;
            } else {
                DeltaT = Math.Round(Math.Abs(lastAF.AutoFocusPoint.Temperature - info.Temperature), 2);
                shouldTrigger = Math.Abs(lastAF.AutoFocusPoint.Temperature - info.Temperature) >= Amount;
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
            return $"Trigger: {nameof(AutofocusAfterTemperatureChangeTrigger)}, Amount: {Amount}°";
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