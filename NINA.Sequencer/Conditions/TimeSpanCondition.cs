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
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Sequencer.Utility;
using NINA.Core.Enum;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_TimeSpanCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_TimeSpanCondition_Description")]
    [ExportMetadata("Icon", "HourglassSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TimeSpanCondition : SequenceCondition {
        private int hours;

        private int minutes;

        private int seconds;

        [ImportingConstructor]
        public TimeSpanCondition() {
            DateTime = new SystemDateTime();
            Minutes = 1;
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

        private TimeSpanCondition(TimeSpanCondition cloneMe) : this() {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new TimeSpanCondition(this) {
                Hours = Hours,
                Minutes = Minutes,
                Seconds = Seconds
            };
        }

        private void Tick() {
            RaisePropertyChanged(nameof(RemainingTime));
        }

        public ICustomDateTime DateTime { get; set; }

        [JsonProperty]
        public int Hours {
            get => hours;
            set {
                hours = value;
                previousRemainingTime = null;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        [JsonProperty]
        public int Minutes {
            get => minutes;
            set {
                minutes = value;
                previousRemainingTime = null;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        [JsonProperty]
        public int Seconds {
            get => seconds;
            set {
                seconds = value;
                previousRemainingTime = null;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RemainingTime));
            }
        }

        public TimeSpan RemainingTime {
            get {
                var duration = TimeSpan.FromHours(Hours) + TimeSpan.FromMinutes(Minutes) + TimeSpan.FromSeconds(Seconds);
                if (startTime.HasValue) {
                    var elapsed = DateTime.Now - startTime.Value;
                    if (previousRemainingTime > TimeSpan.Zero) {
                        elapsed = elapsed + duration - previousRemainingTime.Value;
                    }

                    if (elapsed > duration) {
                        return TimeSpan.Zero;
                    } else {
                        return duration - elapsed;
                    }
                } else {
                    return duration;
                }
            }
        }

        private DateTime? startTime;
        private TimeSpan? previousRemainingTime;

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            var nextItemDuration = nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero;
            var hasTimeRemaining = (RemainingTime - nextItemDuration) > TimeSpan.Zero;
            if (!hasTimeRemaining && nextItemDuration > TimeSpan.Zero) {
                if (nextItem != null) {
                    Logger.Info($"No more time remaining. Remaining: {RemainingTime}, Next Item {nextItem.Name ?? ""}, Next Item Estimated Duration {nextItemDuration}, Next Item Attempts: {nextItem.Attempts}");
                }
                // There is no time remaining due to the next instruction taking longer - cut off any remaining time
                startTime = DateTime.Now.Subtract(TimeSpan.FromHours(Hours) + TimeSpan.FromMinutes(Minutes) + TimeSpan.FromSeconds(Seconds));
                RaisePropertyChanged(nameof(RemainingTime));
            }
            if (!hasTimeRemaining && IsActive()) {
                Logger.Info($"{nameof(TimeSpanCondition)} finished.");
            }
            return hasTimeRemaining;
        }

        public override void SequenceBlockInitialize() {
            startTime = DateTime.Now;

            ConditionWatchdog?.Start();
        }

        public override void SequenceBlockTeardown() {
            if (RemainingTime > TimeSpan.Zero) {
                previousRemainingTime = RemainingTime;
            }
            try { ConditionWatchdog?.Cancel(); } catch { }
        }

        public override void ResetProgress() {
            Status = SequenceEntityStatus.CREATED;
            previousRemainingTime = null;
            startTime = DateTime.Now;
        }

        public override string ToString() {
            return $"Condition: {nameof(TimeSpanCondition)}, Time: {Hours}:{Minutes}:{Seconds}h";
        }
    }
}