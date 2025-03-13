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
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.Trigger.Guider;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Sequencer.Utility;
using System.Windows;
using System.ComponentModel;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Imaging {

    [ExportMetadata("Name", "Lbl_SequenceItem_Imaging_SmartExposure_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Imaging_SmartExposure_Description")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class SmartExposure : SequentialContainer, IImmutableContainer {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        [ImportingConstructor]
        public SmartExposure(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IImagingMediator imagingMediator,
                IImageSaveMediator imageSaveMediator,
                IImageHistoryVM imageHistoryVM,
                IFilterWheelMediator filterWheelMediator,
                IGuiderMediator guiderMediator) : this(
                    null,
                    new SwitchFilter(profileService, filterWheelMediator),
                    new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM),
                    new LoopCondition(),
                    new DitherAfterExposures(guiderMediator, imageHistoryVM, profileService)
                ) {
        }

        /// <summary>
        /// Clone Constructor
        /// </summary>
        public SmartExposure(
                SmartExposure cloneMe,
                SwitchFilter switchFilter,
                TakeExposure takeExposure,
                LoopCondition loopCondition,
                DitherAfterExposures ditherAfterExposures
                ) {
            this.Add(switchFilter);
            this.Add(takeExposure);
            this.Add(loopCondition);
            this.Add(ditherAfterExposures);

            WeakEventManager<SwitchFilter, PropertyChangedEventArgs>.AddHandler(switchFilter, nameof(switchFilter.PropertyChanged), SwitchFilter_PropertyChanged);

            IsExpanded = false;

            if (cloneMe != null) {
                CopyMetaData(cloneMe);
            }
        }
        private SmartExposure(SmartExposure cloneMe) {

            IsExpanded = false;

            if (cloneMe != null) {
                CopyMetaData(cloneMe);
            }
        }

        partial void AfterClone(SmartExposure clone) {
            // The order of these matters!
            clone.Add((SwitchFilter)GetSwitchFilter().Clone());
            clone.Add((TakeExposure)GetTakeExposure().Clone());
            clone.Add((LoopCondition)GetLoopCondition().Clone());
            clone.Add((DitherAfterExposures)GetDitherAfterExposures().Clone());
            // Weak thing...
        }


        private void SwitchFilter_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(SwitchFilter.Filter)) {
                if(this.Status == Core.Enum.SequenceEntityStatus.CREATED || this.Status == Core.Enum.SequenceEntityStatus.RUNNING) { 
                    try {
                        GetSwitchFilter().ResetProgress();
                    } catch(Exception) { }                    
                }
            }
        }

        private InstructionErrorBehavior errorBehavior = InstructionErrorBehavior.ContinueOnError;

        [JsonProperty]
        public override InstructionErrorBehavior ErrorBehavior {
            get => errorBehavior;
            set {
                errorBehavior = value;
                foreach (var item in Items) {
                    item.ErrorBehavior = errorBehavior;
                }
                RaisePropertyChanged();
            }
        }

        private int attempts = 1;

        [JsonProperty]
        public override int Attempts {
            get => attempts;
            set {
                if (value > 0) {
                    attempts = value;
                    foreach (var item in Items) {
                        item.Attempts = attempts;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        [IsExpression(HasValidator = true)]
        private int dither;
        partial void DitherExpressionValidator(Logic.Expression expr) {
            DitherAfterExposures dae = GetDitherAfterExposures();
            if (dae != null) {
                dae.AfterExposures = (int)expr.Value;
            }
        }

        [IsExpression(Default = 1, HasValidator = true)]
        private int iterations;
        partial void IterationsExpressionValidator(Logic.Expression expr) {
            if (Conditions.Count > 0) {
                GetLoopCondition().Iterations = (int)expr.Value;
                RaisePropertyChanged("Iterations");
            }
        }

        public SwitchFilter GetSwitchFilter() {
            return Items[0] as SwitchFilter;
        }

        public TakeExposure GetTakeExposure() {
            return Items[1] as TakeExposure;
        }

        public DitherAfterExposures GetDitherAfterExposures() {
            return Triggers.Count > 0 ? Triggers[0] as DitherAfterExposures : null;
        }

        public LoopCondition GetLoopCondition() {
            return Conditions.Count > 0 ? Conditions[0] as LoopCondition : null;
        }

        public override bool Validate() {
            var issues = new List<string>();
            var sw = GetSwitchFilter();
            var te = GetTakeExposure();
            var dither = GetDitherAfterExposures();

            te.Validate();
            issues.AddRange(te.Issues);

            sw.Validate();
            issues.AddRange(sw.Issues);

            dither.Validate();
            issues.AddRange(dither.Issues);

            Issues = issues;

            Logic.Expression.ValidateExpressions(Issues, IterationsExpression, DitherExpression);

            RaisePropertyChanged(nameof(Issues));

            return Issues.Count == 0;
        }

        public override TimeSpan GetEstimatedDuration() {
            return GetTakeExposure().GetEstimatedDuration();
        }

        /// <summary>
        /// When an inner instruction interrupts this set, it should reroute the interrupt to the real parent set
        /// </summary>
        /// <returns></returns>
        public override Task Interrupt() {
            return this.Parent?.Interrupt();
        }
    }
}