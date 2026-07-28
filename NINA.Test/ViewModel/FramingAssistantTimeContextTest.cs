#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.ViewModel.FramingAssistant;
using NUnit.Framework;
using System;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class FramingAssistantTimeContextTest {

        [Test]
        public void Refresh_WhenUsingCurrentTime_AdvancesSelectedTime() {
            DateTime now = new DateTime(2026, 7, 28, 20, 15, 0, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);
            DateTime later = now.AddSeconds(30);
            bool selectedDateChanged = false;
            bool selectedDateTimeChanged = false;
            sut.PropertyChanged += (_, e) => {
                selectedDateChanged |= e.PropertyName == nameof(FramingAssistantTimeContext.SelectedDate);
                selectedDateTimeChanged |= e.PropertyName == nameof(FramingAssistantTimeContext.SelectedDateTime);
            };

            now = later;
            sut.Refresh();

            sut.SelectedDateTime.Should().Be(later);
            selectedDateChanged.Should().BeTrue();
            selectedDateTimeChanged.Should().BeTrue();
        }

        [Test]
        public void Refresh_WhenUsingFixedTime_KeepsSelectedTimeUntilCurrentTimeIsReenabled() {
            DateTime now = new DateTime(2026, 7, 28, 20, 15, 0, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);
            DateTime selected = new DateTime(2030, 1, 2, 3, 4, 0, DateTimeKind.Local);
            sut.UseCurrentTime = false;
            sut.SelectedDateTime = selected;

            now = now.AddHours(1);
            sut.Refresh();

            sut.SelectedDateTime.Should().Be(selected);

            sut.UseCurrentTime = true;

            sut.SelectedDateTime.Should().Be(now);
        }

        [Test]
        public void SelectedDate_PreservesSelectedTime() {
            DateTime now = new DateTime(2026, 7, 28, 20, 15, 30, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false) {
                UseCurrentTime = false
            };

            sut.SelectedDate = new DateTime(2030, 1, 2);

            sut.SelectedDateTime.Should().Be(new DateTime(2030, 1, 2, 20, 15, 30));
        }

        [Test]
        public void Year_ClampsLeapDayAndStopsFollowingCurrentTime() {
            DateTime now = new DateTime(2028, 2, 29, 20, 15, 30, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            sut.Year = 2027;

            sut.SelectedDateTime.Should().Be(new DateTime(2027, 2, 28, 20, 15, 0, DateTimeKind.Local));
            sut.UseCurrentTime.Should().BeFalse();
        }

        [Test]
        public void Month_ClampsDayToSelectedMonth() {
            DateTime now = new DateTime(2026, 3, 31, 20, 15, 30, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            sut.Month = 4;

            sut.SelectedDateTime.Should().Be(new DateTime(2026, 4, 30, 20, 15, 0, DateTimeKind.Local));
            sut.DaysInSelectedMonth.Should().Be(30);
        }

        [Test]
        public void DayAndTimeComponents_UpdateSelectedDateTime() {
            DateTime now = new DateTime(2026, 7, 28, 20, 15, 30, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            sut.Day = 12;
            sut.Hour = 3;
            sut.Minute = 4;

            sut.SelectedDateTime.Should().Be(new DateTime(2026, 7, 12, 3, 4, 0, DateTimeKind.Local));
            sut.UseCurrentTime.Should().BeFalse();
        }

        [Test]
        public void Hour_WrappingForward_IncrementsDay() {
            DateTime now = new DateTime(2026, 12, 31, 23, 15, 0, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            sut.Hour = 0;

            sut.SelectedDateTime.Should().Be(new DateTime(2027, 1, 1, 0, 15, 0, DateTimeKind.Local));
        }

        [Test]
        public void Minute_WrappingForward_IncrementsHourAndDay() {
            DateTime now = new DateTime(2026, 12, 31, 23, 59, 0, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            sut.Minute = 0;

            sut.SelectedDateTime.Should().Be(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Local));
        }

        [Test]
        public void Day_WrappingForward_IncrementsMonth() {
            DateTime now = new DateTime(2028, 2, 29, 20, 15, 0, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            sut.Day = 1;

            sut.SelectedDateTime.Should().Be(new DateTime(2028, 3, 1, 20, 15, 0, DateTimeKind.Local));
        }

        [Test]
        public void Month_WrappingForward_IncrementsYear() {
            DateTime now = new DateTime(2026, 12, 31, 20, 15, 0, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            sut.Month = 1;

            sut.SelectedDateTime.Should().Be(new DateTime(2027, 1, 31, 20, 15, 0, DateTimeKind.Local));
        }
    }
}