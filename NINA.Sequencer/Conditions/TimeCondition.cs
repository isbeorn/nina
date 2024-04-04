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
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using NINA.Sequencer.Utility;
using NINA.Core.Enum;
using NINA.Sequencer.Validations;
using NINA.Core.Locale;
using NINA.Astrometry;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_TimeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_TimeCondition_Description")]
    [ExportMetadata("Icon", "ClockSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TimeCondition : SequenceCondition, IValidatable {
        private IList<IDateTimeProvider> dateTimeProviders;
        private int hours;
        private int minutes;
        private int minutesOffset;
        private int seconds;
        private IDateTimeProvider selectedProvider;

        [ImportingConstructor]
        public TimeCondition(IList<IDateTimeProvider> dateTimeProviders) : this(dateTimeProviders, dateTimeProviders?.FirstOrDefault()) {
        }

        public TimeCondition(IList<IDateTimeProvider> dateTimeProviders, IDateTimeProvider selectedProvider) {
            DateTime = new SystemDateTime();
            this.DateTimeProviders = dateTimeProviders;
            this.SelectedProvider = selectedProvider;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenTimeIsUp, TimeSpan.FromSeconds(1));
        }

        private async Task InterruptWhenTimeIsUp() {
            Tick();
            if (!Check(null, null)) {
                if (this.Parent != null) {
                    if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                        Logger.Info("Time limit exceeded - Interrupting current Instruction Set");
                        Status = SequenceEntityStatus.FINISHED;
                        await this.Parent.Interrupt();
                    }
                }
            }
        }

        private TimeCondition(TimeCondition cloneMe) : this(cloneMe.DateTimeProviders, cloneMe.SelectedProvider) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new TimeCondition(this) {
                Hours = Hours,
                Minutes = Minutes,
                Seconds = Seconds,
                MinutesOffset = MinutesOffset
            };
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate() {
            var i = new List<string>();
            if(HasFixedTimeProvider) {
                var referenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
                if(lastReferenceDate != referenceDate) {                    
                    UpdateTime();
                }
            }
            if (!timeDeterminedSuccessfully) {
                i.Add(Loc.Instance["LblSelectedTimeSourceInvalid"]);
            }

            Issues = i;
            return i.Count == 0;
        }

        public ICustomDateTime DateTime { get; set; }

        public IList<IDateTimeProvider> DateTimeProviders {
            get => dateTimeProviders;
            set {
                dateTimeProviders = value;
                RaisePropertyChanged();
            }
        }

        public bool HasFixedTimeProvider => selectedProvider != null && !(selectedProvider is Utility.DateTimeProvider.TimeProvider);

        [JsonProperty]
        public int Hours {
            get => hours;
            set {
                hours = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        [JsonProperty]
        public int Minutes {
            get => minutes;
            set {
                minutes = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        [JsonProperty]
        public int MinutesOffset {
            get => minutesOffset;
            set {
                minutesOffset = value;
                UpdateTime();
                RaisePropertyChanged();
            }
        }

        public TimeSpan RemainingTime {
            get {
                TimeSpan remaining = (CalculateRemainingTime() - DateTime.Now);
                if (remaining.TotalSeconds < 0) return new TimeSpan(0);
                return remaining;
            }
        }

        [JsonProperty]
        public int Seconds {
            get => seconds;
            set {
                seconds = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        [JsonProperty]
        public IDateTimeProvider SelectedProvider {
            get => selectedProvider;
            set {
                selectedProvider = value;
                if (selectedProvider != null) {
                    UpdateTime();
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(HasFixedTimeProvider));
                }
            }
        }

        private DateTime CalculateRemainingTime() {
            var now = DateTime.Now;
            var then = new DateTime(now.Year, now.Month, now.Day, Hours, Minutes, Seconds);

            var rollover = SelectedProvider.GetRolloverTime(this);
            var timeOnlyNow = TimeOnly.FromDateTime(now);
            var timeOnlyThen = TimeOnly.FromDateTime(then);

            if (timeOnlyNow < rollover && timeOnlyThen >= rollover) {
                then = then.AddDays(-1);
            }

            if (timeOnlyNow >= rollover && timeOnlyThen < rollover) {
                then = then.AddDays(1);
            }

            return then;
        }

        private void Tick() {
            RaisePropertyChanged(nameof(RemainingTime));
        }

        private bool timeDeterminedSuccessfully;
        private DateTime lastReferenceDate;
        private void UpdateTime() {
            try {
                lastReferenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
                if (HasFixedTimeProvider) {
                    var t = SelectedProvider.GetDateTime(this) + TimeSpan.FromMinutes(MinutesOffset);
                    Hours = t.Hour;
                    Minutes = t.Minute;
                    Seconds = t.Second;

                }
                timeDeterminedSuccessfully = true;
            } catch (Exception) {
                timeDeterminedSuccessfully = false;
                Validate();
            }
        }

        public override void AfterParentChanged() {
            UpdateTime();
            RunWatchdogIfInsideSequenceRoot();
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            var nextItemDuration = nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero;
            var remainingTime = CalculateRemainingTime();
            var hasTimeRemaining = DateTime.Now + nextItemDuration <= remainingTime;
            if (!hasTimeRemaining && nextItemDuration > TimeSpan.Zero) {
                if(nextItem != null) {
                    Logger.Info($"No more time remaining. Remaining: {remainingTime - DateTime.Now}, Next Item {nextItem.Name ?? ""}, Next Item Estimated Duration {nextItemDuration}, Next Item Attempts: {nextItem.Attempts}");
                }
            }
            if (!hasTimeRemaining && IsActive()) {
                Logger.Info($"{nameof(TimeCondition)} finished.");
            }
            return hasTimeRemaining;
        }

        public override string ToString() {
            return $"Condition: {nameof(TimeCondition)}, Time: {Hours}:{Minutes}:{Seconds}h";
        }
    }
}