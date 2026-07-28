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
using System;
using System.Windows.Threading;

namespace NINA.ViewModel.FramingAssistant {

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
            set {
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
                SetSelectedDateTime(value, Month, day, Hour, Minute);
            }
        }

        public int Month {
            get => SelectedDateTime.Month;
            set {
                DateTime date = SelectedDateTime;
                if (Month == 12 && value == 1) {
                    date = SelectedDateTime.AddYears(1);
                } else if (Month == 1 && value == 12) {
                    date = SelectedDateTime.AddYears(-1);
                }
                int day = Math.Min(date.Day, DateTime.DaysInMonth(date.Year, value));
                SetSelectedDateTime(date.Year, value, day, Hour, Minute);
            }
        }

        public int DaysInSelectedMonth => DateTime.DaysInMonth(Year, Month);

        public int Day {
            get => SelectedDateTime.Day;
            set {
                DateTime date = SelectedDateTime;
                int day = value;
                if (Day == DaysInSelectedMonth && value == 1) {
                    date = SelectedDateTime.AddDays(1);
                } else if (Day == 1 && value == DaysInSelectedMonth) {
                    date = SelectedDateTime.AddDays(-1);
                    day = date.Day;
                }
                SetSelectedDateTime(date.Year, date.Month, day, Hour, Minute);
            }
        }

        public int Hour {
            get => SelectedDateTime.Hour;
            set {
                DateTime date = SelectedDateTime;
                if (Hour == 23 && value == 0) {
                    date = SelectedDateTime.AddDays(1);
                } else if (Hour == 0 && value == 23) {
                    date = SelectedDateTime.AddDays(-1);
                }
                SetSelectedDateTime(date.Year, date.Month, date.Day, value, Minute);
            }
        }

        public int Minute {
            get => SelectedDateTime.Minute;
            set {
                DateTime date = SelectedDateTime;
                if (Minute == 59 && value == 0) {
                    date = SelectedDateTime.AddHours(1);
                } else if (Minute == 0 && value == 59) {
                    date = SelectedDateTime.AddHours(-1);
                }
                SetSelectedDateTime(date.Year, date.Month, date.Day, date.Hour, value);
            }
        }

        private void SetSelectedDateTime(int year, int month, int day, int hour, int minute) {
            UseCurrentTime = false;
            SelectedDateTime = new DateTime(year, month, day, hour, minute, 0, SelectedDateTime.Kind);
        }

        public DateTime SelectedDate {
            get => SelectedDateTime.Date;
            set => SelectedDateTime = value.Date + SelectedDateTime.TimeOfDay;
        }

        public bool UseCurrentTime {
            get => useCurrentTime;
            set {
                if (useCurrentTime != value) {
                    useCurrentTime = value;
                    RaisePropertyChanged();
                    Refresh();
                }
            }
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