#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Math;
using Accord.Statistics.Models.Regression.Linear;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.WindowService;
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
using NINA.Image.ImageAnalysis;
using NINA.Sequencer.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.Trigger.Autofocus {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_AutofocusAfterHFRIncreaseTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_AutofocusAfterHFRIncreaseTrigger_Description")]
    [ExportMetadata("Icon", "AutoFocusAfterHFRSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class AutofocusAfterHFRIncreaseTrigger : SequenceTrigger, IValidatable {
        private IProfileService profileService;
        private IImageHistoryVM history;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IAutoFocusVMFactory autoFocusVMFactory;

        [ImportingConstructor]
        public AutofocusAfterHFRIncreaseTrigger(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IAutoFocusVMFactory autoFocusVMFactory) : base() {
            this.history = history;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.autoFocusVMFactory = autoFocusVMFactory;
            TriggerRunner.Add(new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory));
        }

        private AutofocusAfterHFRIncreaseTrigger(AutofocusAfterHFRIncreaseTrigger cloneMe) : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(AutofocusAfterHFRIncreaseTrigger clone) {
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

        [IsExpression (Default = 5, Range = [1, 1000])]
        private double amount;


        [IsExpression (Default = 10, Range = [3, 1000])]
        private int sampleSize;

        private double originalHFR;

        public double OriginalHFR {
            get => originalHFR;
            private set {
                originalHFR = value;
                RaisePropertyChanged();
            }
        }

        private double hFRTrendPercentage;

        public double HFRTrendPercentage {
            get => hFRTrendPercentage;
            private set {
                hFRTrendPercentage = value;
                RaisePropertyChanged();
            }
        }

        private double hFRTrend;

        public double HFRTrend {
            get => hFRTrend;
            private set {
                hFRTrend = value;
                RaisePropertyChanged();
            }
        }

        private string filter;

        public string Filter {
            get => filter;
            private set {
                filter = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            await TriggerRunner.Run(progress, token);
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (nextItem == null) { return false; }
            if (!(nextItem is IExposureItem exposureItem)) { return false; }
            if (exposureItem.ImageType != "LIGHT") { return false; }

            bool shouldTrigger = false;
            var fwInfo = this.filterWheelMediator.GetInfo();

            var lastAF = history.AutoFocusPoints?.LastOrDefault();
            // Filter the history for all light frames and frames that have a non zero HFR value
            var imageHistory = history.ImageHistory.Where(x => x.Type == "LIGHT" && !double.IsNaN(x.HFR) && x.HFR > 0).ToList();

            if (lastAF != null) {
                //Take all items that are after the last autofocus into consideration
                imageHistory = imageHistory.Where(point => point.Id > lastAF.Id).ToList();
            }

            if (fwInfo != null && fwInfo.Connected && fwInfo.SelectedFilter != null) {
                //Further filter the history to only considere items by the current filter
                Filter = fwInfo.SelectedFilter.Name;

                imageHistory = imageHistory.Where(x => x.Filter == filter).ToList();
            } else {
                Filter = string.Empty;
            }

            if (imageHistory.Count > 0) {
                // Determine the lowest HFR
                var lowest = imageHistory.Aggregate((curMin, x) => curMin == null || x.HFR < curMin.HFR ? x : curMin);
                OriginalHFR = lowest.HFR;

                //Take either the last point after AF Position as index or the sample size index, whichever is greater
                var minimumSampleIndex = Math.Max(0, imageHistory.Count - SampleSize);  // Note sampleSize isn't used

                if (imageHistory.Count > minimumSampleIndex + 2) {
                    var data = imageHistory
                    .Skip(minimumSampleIndex)
                    .Where(x => !double.IsNaN(x.HFR) && x.HFR > 0);

                    if (data.Count() >= 3) {
                        double[] outputs = data
                            .Select(img => img.HFR)
                            .ToArray();

                        double[] inputs = Enumerable.Range(1, data.Count()).Select(i => (double)i).ToArray();

                        OrdinaryLeastSquares ols = new OrdinaryLeastSquares();
                        SimpleLinearRegression regression = ols.Learn(inputs, outputs);

                        //Get current smoothed out HFR
                        HFRTrend = regression.Transform(data.Count());

                        Logger.Debug($"Autofocus condition extrapolated original HFR: {OriginalHFR} extrapolated current HFR: {HFRTrend}");

                        HFRTrendPercentage = Math.Round((1 - (OriginalHFR / HFRTrend)) * 100, 2);

                        if (HFRTrendPercentage > Amount) {
                            /* Trigger autofocus after HFR change */
                            Logger.Info($"Autofocus after HFR change should be triggered, as current HFR trend is {HFRTrendPercentage}% higher compared to threshold of {Amount}%");
                            shouldTrigger = true;
                        }
                    }
                } else {
                    HFRTrendPercentage = 0;
                    HFRTrend = 0;
                }
            } else {
                OriginalHFR = 0;
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
            return $"Trigger: {nameof(AutofocusAfterHFRIncreaseTrigger)}, Amount: {Amount}";
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

            Expression.ValidateExpressions(i, AmountExpression, SampleSizeExpression);

            Issues = i;
            return i.Count == 0;
        }
    }
}