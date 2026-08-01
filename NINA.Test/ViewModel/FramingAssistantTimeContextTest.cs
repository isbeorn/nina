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
using NINA.CustomControlLibrary;
using NINA.ViewModel.FramingAssistant;
using NUnit.Framework;
using System;
using System.Reflection;

namespace NINA.Test.ViewModel {

    [TestFixture]
    internal class FramingAssistantTimeContextTest {

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
        public void ManualEdit_StopsFollowingCurrentTimeUntilReset() {
            DateTime now = new DateTime(2026, 7, 28, 20, 15, 30, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            sut.Minute = 4;
            DateTime selected = sut.SelectedDateTime;
            now = now.AddHours(1);
            sut.Refresh();

            sut.SelectedDateTime.Should().Be(selected);
            sut.UseCurrentTime.Should().BeFalse();

            sut.ResetToCurrentTime();

            sut.SelectedDateTime.Should().Be(now);
            sut.UseCurrentTime.Should().BeTrue();
        }

        [Test]
        public void ExternallyConsumedTimeState_IsReadOnly() {
            typeof(FramingAssistantTimeContext).GetProperty(nameof(FramingAssistantTimeContext.SelectedDateTime))!
                .SetMethod!.IsPublic.Should().BeFalse();
            typeof(FramingAssistantTimeContext).GetProperty(nameof(FramingAssistantTimeContext.SelectedDate))!
                .SetMethod.Should().BeNull();
            typeof(FramingAssistantTimeContext).GetProperty(nameof(FramingAssistantTimeContext.UseCurrentTime))!
                .SetMethod.Should().BeNull();
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

        [TestCaseSource(nameof(StepCases))]
        public void Adjust_CarriesAndBorrowsAcrossAdjacentUnits(
            DateTime start,
            FramingAssistantTimePart part,
            StepDirection direction,
            DateTime expected) {
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => start, startTimer: false);

            sut.Adjust(part, direction);

            sut.SelectedDateTime.Should().Be(expected);
            sut.UseCurrentTime.Should().BeFalse();
        }

        [TestCase(2026, 12, 31, 23, 59, FramingAssistantTimePart.Month, 1, 2026, 1, 31, 23, 59)]
        [TestCase(2026, 1, 31, 23, 59, FramingAssistantTimePart.Month, 12, 2026, 12, 31, 23, 59)]
        [TestCase(2026, 7, 31, 23, 59, FramingAssistantTimePart.Day, 1, 2026, 7, 1, 23, 59)]
        [TestCase(2026, 7, 1, 23, 59, FramingAssistantTimePart.Day, 31, 2026, 7, 31, 23, 59)]
        [TestCase(2026, 7, 31, 23, 59, FramingAssistantTimePart.Hour, 0, 2026, 7, 31, 0, 59)]
        [TestCase(2026, 7, 31, 0, 59, FramingAssistantTimePart.Hour, 23, 2026, 7, 31, 23, 59)]
        [TestCase(2026, 7, 31, 23, 59, FramingAssistantTimePart.Minute, 0, 2026, 7, 31, 23, 0)]
        [TestCase(2026, 7, 31, 23, 0, FramingAssistantTimePart.Minute, 59, 2026, 7, 31, 23, 59)]
        public void TypedEndpointValue_IsAbsoluteAndDoesNotCarryOrBorrow(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            FramingAssistantTimePart part,
            int value,
            int expectedYear,
            int expectedMonth,
            int expectedDay,
            int expectedHour,
            int expectedMinute) {
            DateTime now = new DateTime(year, month, day, hour, minute, 30, DateTimeKind.Local);
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => now, startTimer: false);

            switch (part) {
                case FramingAssistantTimePart.Month: sut.Month = value; break;
                case FramingAssistantTimePart.Day: sut.Day = value; break;
                case FramingAssistantTimePart.Hour: sut.Hour = value; break;
                case FramingAssistantTimePart.Minute: sut.Minute = value; break;
            }

            sut.SelectedDateTime.Should().Be(new DateTime(
                expectedYear,
                expectedMonth,
                expectedDay,
                expectedHour,
                expectedMinute,
                0,
                DateTimeKind.Local));
            sut.UseCurrentTime.Should().BeFalse();
        }

        [TestCaseSource(nameof(GlobalLimitCases))]
        public void Adjust_AtGlobalLimit_OnlyAllowsInwardSteps(
            DateTime endpoint,
            FramingAssistantTimePart part,
            StepDirection direction,
            DateTime expected) {
            using FramingAssistantTimeContext sut = new FramingAssistantTimeContext(() => endpoint, startTimer: false);

            Action adjust = () => sut.Adjust(part, direction);

            adjust.Should().NotThrow();
            sut.SelectedDateTime.Should().Be(expected);
        }

        private static object[] StepCases => [
            StepCase(new DateTime(2026, 12, 31, 20, 15, 30, DateTimeKind.Local), FramingAssistantTimePart.Month, StepDirection.Increment, new DateTime(2027, 1, 31, 20, 15, 0, DateTimeKind.Local)),
            StepCase(new DateTime(2026, 1, 31, 20, 15, 30, DateTimeKind.Local), FramingAssistantTimePart.Month, StepDirection.Decrement, new DateTime(2025, 12, 31, 20, 15, 0, DateTimeKind.Local)),
            StepCase(new DateTime(2028, 2, 29, 20, 15, 30, DateTimeKind.Local), FramingAssistantTimePart.Day, StepDirection.Increment, new DateTime(2028, 3, 1, 20, 15, 0, DateTimeKind.Local)),
            StepCase(new DateTime(2028, 3, 1, 20, 15, 30, DateTimeKind.Local), FramingAssistantTimePart.Day, StepDirection.Decrement, new DateTime(2028, 2, 29, 20, 15, 0, DateTimeKind.Local)),
            StepCase(new DateTime(2026, 12, 31, 23, 15, 30, DateTimeKind.Local), FramingAssistantTimePart.Hour, StepDirection.Increment, new DateTime(2027, 1, 1, 0, 15, 0, DateTimeKind.Local)),
            StepCase(new DateTime(2026, 1, 1, 0, 15, 30, DateTimeKind.Local), FramingAssistantTimePart.Hour, StepDirection.Decrement, new DateTime(2025, 12, 31, 23, 15, 0, DateTimeKind.Local)),
            StepCase(new DateTime(2026, 12, 31, 23, 59, 30, DateTimeKind.Local), FramingAssistantTimePart.Minute, StepDirection.Increment, new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Local)),
            StepCase(new DateTime(2026, 1, 1, 0, 0, 30, DateTimeKind.Local), FramingAssistantTimePart.Minute, StepDirection.Decrement, new DateTime(2025, 12, 31, 23, 59, 0, DateTimeKind.Local))
        ];

        private static object[] GlobalLimitCases => [
            LimitCase(DateTime.MaxValue, FramingAssistantTimePart.Month, StepDirection.Increment, DateTime.MaxValue),
            LimitCase(DateTime.MaxValue, FramingAssistantTimePart.Day, StepDirection.Increment, DateTime.MaxValue),
            LimitCase(DateTime.MaxValue, FramingAssistantTimePart.Hour, StepDirection.Increment, DateTime.MaxValue),
            LimitCase(DateTime.MaxValue, FramingAssistantTimePart.Minute, StepDirection.Increment, DateTime.MaxValue),
            LimitCase(DateTime.MinValue, FramingAssistantTimePart.Month, StepDirection.Decrement, DateTime.MinValue),
            LimitCase(DateTime.MinValue, FramingAssistantTimePart.Day, StepDirection.Decrement, DateTime.MinValue),
            LimitCase(DateTime.MinValue, FramingAssistantTimePart.Hour, StepDirection.Decrement, DateTime.MinValue),
            LimitCase(DateTime.MinValue, FramingAssistantTimePart.Minute, StepDirection.Decrement, DateTime.MinValue),
            LimitCase(DateTime.MaxValue, FramingAssistantTimePart.Month, StepDirection.Decrement, new DateTime(9999, 11, 30, 23, 59, 0)),
            LimitCase(DateTime.MaxValue, FramingAssistantTimePart.Day, StepDirection.Decrement, new DateTime(9999, 12, 30, 23, 59, 0)),
            LimitCase(DateTime.MaxValue, FramingAssistantTimePart.Hour, StepDirection.Decrement, new DateTime(9999, 12, 31, 22, 59, 0)),
            LimitCase(DateTime.MaxValue, FramingAssistantTimePart.Minute, StepDirection.Decrement, new DateTime(9999, 12, 31, 23, 58, 0)),
            LimitCase(DateTime.MinValue, FramingAssistantTimePart.Month, StepDirection.Increment, new DateTime(1, 2, 1, 0, 0, 0)),
            LimitCase(DateTime.MinValue, FramingAssistantTimePart.Day, StepDirection.Increment, new DateTime(1, 1, 2, 0, 0, 0)),
            LimitCase(DateTime.MinValue, FramingAssistantTimePart.Hour, StepDirection.Increment, new DateTime(1, 1, 1, 1, 0, 0)),
            LimitCase(DateTime.MinValue, FramingAssistantTimePart.Minute, StepDirection.Increment, new DateTime(1, 1, 1, 0, 1, 0))
        ];

        private static object[] StepCase(DateTime start, FramingAssistantTimePart part, StepDirection direction, DateTime expected) {
            return [start, part, direction, expected];
        }

        private static object[] LimitCase(DateTime endpoint, FramingAssistantTimePart part, StepDirection direction, DateTime expected) {
            return [endpoint, part, direction, expected];
        }
    }
}
