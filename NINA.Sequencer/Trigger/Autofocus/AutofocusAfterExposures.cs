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
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Sequencer.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Sequencer.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.Trigger.Autofocus {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_AutofocusAfterExposures_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_AutofocusAfterExposures_Description")]
    [ExportMetadata("Icon", "AutoFocusAfterExposuresSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class AutofocusAfterExposures : SequenceTrigger, IValidatable {
        private IProfileService profileService;

        private IImageHistoryVM history;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IAutoFocusVMFactory autoFocusVMFactory;
        private readonly ISafetyMonitorMediator safetyMonitorMediator;

        [ImportingConstructor]
        public AutofocusAfterExposures(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IAutoFocusVMFactory autoFocusVMFactory, ISafetyMonitorMediator safetyMonitorMediator) : base() {
            this.history = history;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.autoFocusVMFactory = autoFocusVMFactory;
            this.safetyMonitorMediator = safetyMonitorMediator;
            TriggerRunner.Add(new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory));
        }

        private AutofocusAfterExposures(AutofocusAfterExposures cloneMe) : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory, cloneMe.safetyMonitorMediator) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(AutofocusAfterExposures clone) {
            clone.TriggerRunner = (SequentialContainer)TriggerRunner.Clone();
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        [IsExpression (Default = 5)]
        private int afterExposures;

        public int ProgressExposures { get; private set; }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            await TriggerRunner.Run(progress, token);
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (nextItem == null) { return false; }
            if (!(nextItem is IExposureItem exposureItem)) { return false; }
            if (exposureItem.ImageType != "LIGHT") { return false; }
            if (safetyMonitorMediator.GetInfo() is { Connected: true, IsSafe: false }) { return false; }

            var lastAFId = history.AutoFocusPoints?.LastOrDefault()?.Id ?? 0;
            var lightImageHistory = history.ImageHistory.Where(x => x.Type == "LIGHT" && x.Id > lastAFId).ToList();
            ProgressExposures = lightImageHistory.Count % AfterExposures;
            RaisePropertyChanged(nameof(ProgressExposures));


            var shouldTrigger =
                lightImageHistory.Count > 0
                && ProgressExposures == 0;

            if (shouldTrigger) {
                if (ItemUtility.IsTooCloseToMeridianFlip(Parent, TriggerRunner.GetItemsSnapshot().First().GetEstimatedDuration() + nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero)) {
                    Logger.Warning("Autofocus should be triggered, however the meridian flip is too close to be executed");
                    shouldTrigger = false;
                }
            }

            Logger.Debug($"{nameof(AutofocusAfterExposures)} - Should Trigger: {shouldTrigger}; Image History count since last AF: {lightImageHistory.Count}; Progress Exposures: {ProgressExposures}");
            return shouldTrigger;
        }

        public override string ToString() {
            return $"Trigger: {nameof(AutofocusAfterExposures)}, AfterExposures: {AfterExposures}";
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
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

            Expression.ValidateExpressions(i, AfterExposuresExpression);

            Issues = i;
            return i.Count == 0;
        }
    }
}