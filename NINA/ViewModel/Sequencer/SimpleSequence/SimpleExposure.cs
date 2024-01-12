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
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Guider;
using NINA.ViewModel.ImageHistory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.ViewModel.Sequencer.SimpleSequence {

    [JsonObject(MemberSerialization.OptIn)]
    public class SimpleExposure : SequentialContainer, ISimpleExposure {
        private bool enabled;
        private bool dither;
        private ISequencerFactory factory;
        private DitherAfterExposures ditherAfterExposures;

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        [ImportingConstructor]
        public SimpleExposure(ISequencerFactory factory) {
            this.factory = factory;
            var switchFilter = factory.GetItem<SwitchFilter>();
            var takeExposure = factory.GetItem<TakeExposure>();
            var loopCondition = factory.GetCondition<LoopCondition>();
            ditherAfterExposures = factory.GetTrigger<DitherAfterExposures>();

            this.Add(switchFilter);
            this.Add(takeExposure);
            this.Add(loopCondition);

            WeakEventManager<SwitchFilter, PropertyChangedEventArgs>.AddHandler(switchFilter, nameof(switchFilter.PropertyChanged), SwitchFilter_PropertyChanged);

            loopCondition.PropertyChanged += LoopCondition_PropertyChanged;

            IsExpanded = false;
            Enabled = true;
        }
        private void SwitchFilter_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(SwitchFilter.Filter)) {
                if (this.Status == Core.Enum.SequenceEntityStatus.CREATED || this.Status == Core.Enum.SequenceEntityStatus.RUNNING) {
                    try {
                        GetSwitchFilter().ResetProgress();
                    } catch (Exception) { }
                }
            }
        }

        [JsonProperty]
        public bool Enabled {
            get => enabled;
            set {
                enabled = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool Dither {
            get => dither;
            set {
                dither = value;
                if (dither) {
                    this.Triggers.Add(ditherAfterExposures);
                } else {
                    this.Triggers.Remove(ditherAfterExposures);
                }

                RaisePropertyChanged();
            }
        }

        public ISequenceItem GetSwitchFilter() {
            return Items[0] as SwitchFilter;
        }

        public ISequenceItem GetTakeExposure() {
            return Items[1] as TakeExposure;
        }

        public ISequenceTrigger GetDitherAfterExposures() {
            return ditherAfterExposures;
        }

        public ISequenceCondition GetLoopCondition() {
            return Conditions[0] as LoopCondition;
        }

        public IImmutableContainer TransformToSmartExposure() {
            var smart = factory.GetItem<SmartExposure>();

            var filter = smart.GetSwitchFilter() as SwitchFilter;
            filter.Filter = (this.GetSwitchFilter() as SwitchFilter).Filter;

            var exposure = smart.GetTakeExposure();
            exposure.Binning = (this.GetTakeExposure() as TakeExposure).Binning;
            exposure.ImageType = (this.GetTakeExposure() as TakeExposure).ImageType;
            exposure.ExposureTime = (this.GetTakeExposure() as TakeExposure).ExposureTime;
            exposure.Gain = (this.GetTakeExposure() as TakeExposure).Gain;
            exposure.Offset = (this.GetTakeExposure() as TakeExposure).Offset;

            var dither = smart.GetDitherAfterExposures();
            dither.AfterExposures = this.Dither ? (this.GetDitherAfterExposures() as DitherAfterExposures).AfterExposures : 0;

            var loop = smart.GetLoopCondition();
            loop.CompletedIterations = (this.GetLoopCondition() as LoopCondition).CompletedIterations;
            loop.Iterations = (this.GetLoopCondition() as LoopCondition).Iterations;

            return smart;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Enabled) {
                return base.Execute(progress, token);
            } else {
                return Task.CompletedTask;
            }
        }

        public override bool Validate() {
            var issues = new List<string>();
            var sw = GetSwitchFilter() as SwitchFilter;
            var te = GetTakeExposure() as TakeExposure;

            bool valid = false;

            valid = te.Validate() && valid;
            issues.AddRange(te.Issues);

            if (sw.Filter != null) {
                valid = sw.Validate() && valid;
                issues.AddRange(sw.Issues);
            }

            if (Dither) {
                var ditherAfterExposures = GetDitherAfterExposures();
                valid = (ditherAfterExposures as DitherAfterExposures).Validate() && valid;
                issues.AddRange((ditherAfterExposures as DitherAfterExposures).Issues);
            }

            Issues = issues;
            RaisePropertyChanged(nameof(Issues));

            return valid;
        }

        public override object Clone() {
            var clone = new SimpleExposure(factory) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(),
                Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(t => t.Clone() as ISequenceCondition)),
            };

            clone.Enabled = this.Enabled;
            clone.Dither = this.Dither;
            clone.ditherAfterExposures.AfterExposures = this.ditherAfterExposures.AfterExposures;

            foreach (var item in clone.Items) {
                item.AttachNewParent(clone);
            }

            foreach (var condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (var trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }

            (clone.GetLoopCondition() as LoopCondition).PropertyChanged += LoopCondition_PropertyChanged;

            return clone;
        }

        private void LoopCondition_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(LoopCondition.Iterations)) {
                var loopCondition = (LoopCondition)sender;
                foreach (var item in loopCondition.Parent?.Items) {
                    item.ResetProgress();
                }
                loopCondition.Parent?.ResetProgressCascaded();
            }
        }
    }
}