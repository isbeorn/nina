#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.CustomControlLibrary;
using System;
using System.Windows.Threading;

namespace NINA.ViewModel.FramingAssistant {

    internal enum FramingAssistantTimePart {
        Month,
        Day,
        Hour,
        Minute
    }

    internal sealed class FramingAssistantTimeContext : BaseINPC, IDisposable {
        private readonly Func<DateTime> clock;
        private readonly DispatcherTimer timer;
        private DateTime selectedDateTime;
        private bool useCurrentTime = true;

        public FramingAssistantTimeContext() : this(() => DateTime.Now, startTimer: true) {
        }

        internal FramingAssistantTimeContext(Func<DateTime> clock, bool startTimer) {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            selectedDateTime = clock();
            if (startTimer) {
                timer = new DispatcherTimer(DispatcherPriority.Background) {
                    Interval = TimeSpan.FromSeconds(30)
                };
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        public DateTime SelectedDateTime {
            get => selectedDateTime;
            private set {
                if (selectedDateTime != value) {
                    selectedDateTime = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SelectedDate));
                    RaisePropertyChanged(nameof(Year));
                    RaisePropertyChanged(nameof(Month));
                    RaisePropertyChanged(nameof(Day));
                    RaisePropertyChanged(nameof(Hour));
                    RaisePropertyChanged(nameof(Minute));
                    RaisePropertyChanged(nameof(DaysInSelectedMonth));
                }
            }
        }

        public int Year {
            get => SelectedDateTime.Year;
            set {
                int day = Math.Min(SelectedDateTime.Day, DateTime.DaysInMonth(value, SelectedDateTime.Month));
                SetFixedTime(value, Month, day, Hour, Minute);
            }
        }

        public int Month {
            get => SelectedDateTime.Month;
            set {
                int day = Math.Min(Day, DateTime.DaysInMonth(Year, value));
                SetFixedTime(Year, value, day, Hour, Minute);
            }
        }

        public int DaysInSelectedMonth => DateTime.DaysInMonth(Year, Month);

        public int Day {
            get => SelectedDateTime.Day;
            set => SetFixedTime(Year, Month, value, Hour, Minute);
        }

        public int Hour {
            get => SelectedDateTime.Hour;
            set => SetFixedTime(Year, Month, Day, value, Minute);
        }

        public int Minute {
            get => SelectedDateTime.Minute;
            set => SetFixedTime(Year, Month, Day, Hour, value);
        }

        private void SetFixedTime(int year, int month, int day, int hour, int minute) {
            SetFixedTime(new DateTime(year, month, day, hour, minute, 0, SelectedDateTime.Kind));
        }

        private void SetFixedTime(DateTime value) {
            if (useCurrentTime) {
                useCurrentTime = false;
                RaisePropertyChanged(nameof(UseCurrentTime));
            }
            SelectedDateTime = value;
        }

        public DateTime SelectedDate => SelectedDateTime.Date;

        public bool UseCurrentTime => useCurrentTime;

        public void Adjust(FramingAssistantTimePart part, StepDirection direction) {
            int amount = (int)direction;
            DateTime adjusted;
            try {
                adjusted = part switch {
                    FramingAssistantTimePart.Month => SelectedDateTime.AddMonths(amount),
                    FramingAssistantTimePart.Day => SelectedDateTime.AddDays(amount),
                    FramingAssistantTimePart.Hour => SelectedDateTime.AddHours(amount),
                    FramingAssistantTimePart.Minute => SelectedDateTime.AddMinutes(amount),
                    _ => SelectedDateTime
                };
            } catch (ArgumentOutOfRangeException) {
                return;
            }

            SetFixedTime(new DateTime(
                adjusted.Year,
                adjusted.Month,
                adjusted.Day,
                adjusted.Hour,
                adjusted.Minute,
                0,
                adjusted.Kind));
        }

        public void ResetToCurrentTime() {
            if (!useCurrentTime) {
                useCurrentTime = true;
                RaisePropertyChanged(nameof(UseCurrentTime));
            }
            Refresh();
        }

        internal void Refresh() {
            if (UseCurrentTime) {
                SelectedDateTime = clock();
            }
        }

        private void Timer_Tick(object sender, EventArgs e) {
            Refresh();
        }

        public void Dispose() {
            if (timer is not null) {
                timer.Stop();
                timer.Tick -= Timer_Tick;
            }
        }
    }
}
