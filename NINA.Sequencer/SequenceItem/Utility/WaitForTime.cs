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
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Sequencer.Validations;
using NINA.Core.Locale;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForTime_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForTime_Description")]
    [ExportMetadata("Icon", "ClockSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForTime : SequenceItem, IValidatable {
        private IList<IDateTimeProvider> dateTimeProviders;
        private int hours;
        private int minutes;
        private int minutesOffset;
        private int seconds;
        private IDateTimeProvider selectedProvider;

        [ImportingConstructor]
        public WaitForTime(IList<IDateTimeProvider> dateTimeProviders) {
            DateTime = new SystemDateTime();
            this.DateTimeProviders = dateTimeProviders;
            this.SelectedProvider = DateTimeProviders?.FirstOrDefault();
        }

        public WaitForTime(IList<IDateTimeProvider> dateTimeProviders, IDateTimeProvider selectedProvider) {
            DateTime = new SystemDateTime();
            this.DateTimeProviders = dateTimeProviders;
            this.SelectedProvider = selectedProvider;
        }

        private WaitForTime(WaitForTime cloneMe) : this(cloneMe.DateTimeProviders, cloneMe.SelectedProvider) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WaitForTime(this) {
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
            if (HasFixedTimeProvider) {
                var referenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
                if (lastReferenceDate != referenceDate) {
                    UpdateTime();
                }
            }
            if (!timeDeterminedSuccessfully) {
                i.Add($"{Loc.Instance["LblSelectedTimeSourceInvalid"]} {failureReason}");
            }

            Issues = i;
            return i.Count == 0;
        }

        public IList<IDateTimeProvider> DateTimeProviders {
            get => dateTimeProviders;
            set {
                dateTimeProviders = value;
                RaisePropertyChanged();
            }
        }

        public bool HasFixedTimeProvider => selectedProvider != null && !(selectedProvider is NINA.Sequencer.Utility.DateTimeProvider.TimeProvider);

        [JsonProperty]
        public int Hours {
            get => hours;
            set {
                hours = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int Minutes {
            get => minutes;
            set {
                minutes = value;
                RaisePropertyChanged();
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

        [JsonProperty]
        public int Seconds {
            get => seconds;
            set {
                seconds = value;
                RaisePropertyChanged();
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
        private bool timeDeterminedSuccessfully;
        private string failureReason;
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
            } catch(Exception ex) {
                timeDeterminedSuccessfully = false;
                failureReason = ex is TimeProviderException tpe ? tpe.LocalizedMessage : ex.Message;
                Validate();
            }            
        }

        public override void AfterParentChanged() {
            UpdateTime();
        }

        public ICustomDateTime DateTime { get; set; }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return NINA.Core.Utility.CoreUtil.Wait(GetEstimatedDuration(), true, token, progress, "");
        }

        public override TimeSpan GetEstimatedDuration() {
            var now = DateTime.Now;
            var then = new DateTime(now.Year, now.Month, now.Day, Hours, Minutes, Seconds);

            var rollover = SelectedProvider.GetRolloverTime(this);
            var timeOnlyNow = TimeOnly.FromDateTime(now);
            var timeOnlyThen = TimeOnly.FromDateTime(then);

            if(timeOnlyNow < rollover && timeOnlyThen >= rollover) {
                then = then.AddDays(-1);
            }

            if (timeOnlyNow >= rollover && timeOnlyThen < rollover) {
                then = then.AddDays(1);
            }

            var diff = then - DateTime.Now;
            if (diff < TimeSpan.Zero) {
                return TimeSpan.Zero;
            } else {
                return diff;
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForTime)}, Time: {Hours}:{Minutes}:{Seconds}h, Offset: {MinutesOffset}";
        }
    }
}