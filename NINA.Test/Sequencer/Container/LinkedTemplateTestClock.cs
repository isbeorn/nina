#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Container;
using NUnit.Framework;
using System.Diagnostics;
using System.Reflection;

namespace NINA.Test.Sequencer.Container {
    // Drives the production TimeProvider timers without waiting five wall-clock minutes.
    internal sealed class LinkedTemplateTestClock : TimeProvider {
        private readonly object gate = new();
        private readonly List<ClockTimer> timers = new();
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() { lock (gate) return timestamp; }
        public int PendingTimers { get { lock (gate) return timers.Count(t => !t.Disposed && t.Due != long.MaxValue); } }

        public void Install(LinkedTemplateContainer linked) => typeof(LinkedTemplateContainer)
            .GetField("editTimeProvider", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(linked, this);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
            var timer = new ClockTimer(this, callback, state);
            lock (gate) {
                timers.Add(timer);
                timer.Change(dueTime, period);
            }
            return timer;
        }

        public void Advance(TimeSpan elapsed) {
            ClockTimer[] due;
            lock (gate) {
                timestamp += elapsed.Ticks;
                due = timers.Where(t => !t.Disposed && t.Due <= timestamp).ToArray();
                foreach (var timer in due) timer.Due = timer.Period == Timeout.InfiniteTimeSpan ? long.MaxValue : timestamp + timer.Period.Ticks;
            }
            foreach (var timer in due) timer.Callback(timer.State);
        }

        public static async Task Until(Func<bool> condition) {
            var elapsed = Stopwatch.StartNew();
            while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(5)) await Task.Delay(10);
            Assert.That(condition(), Is.True, "The expected asynchronous transition did not occur.");
        }

        private sealed class ClockTimer(LinkedTemplateTestClock clock, TimerCallback callback, object? state) : ITimer {
            public readonly TimerCallback Callback = callback;
            public readonly object? State = state;
            public long Due;
            public TimeSpan Period;
            public bool Disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period) {
                lock (clock.gate) {
                    if (Disposed) return false;
                    Due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock.timestamp + dueTime.Ticks;
                    Period = period;
                    return true;
                }
            }
            public void Dispose() { lock (clock.gate) { Disposed = true; clock.timers.Remove(this); } }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
