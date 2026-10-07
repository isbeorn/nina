#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NINA.Test.Model {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class PagedListThreadingTest {

        [SetUp]
        public void SetUp() {
            Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            Assert.That(application.Dispatcher.CheckAccess(), Is.True);
        }

        [Test]
        public void Constructor_OnSynchronizationContext_LoadsFirstPageWithoutPostingContinuation() {
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            var context = new RecordingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);
            try {
                var pagedList = new PagedList<int>(3, Enumerable.Range(1, 8));

                Assert.That(context.PostCount, Is.Zero,
                    "synchronous construction must not wait for a continuation posted to its caller's context");
                Assert.That(pagedList.ItemPage, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(pagedList.CurrentPage, Is.EqualTo(1));
                Assert.That(pagedList.PageStartIndex, Is.EqualTo(1));
                Assert.That(pagedList.PageEndIndex, Is.EqualTo(3));
            } finally {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        [Test]
        public void NavigationCommands_OnDispatcher_KeepPageNotificationsOnCallerThread() {
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try {
                var pagedList = new PagedList<int>(2, Enumerable.Range(1, 5));
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Application.Current.Dispatcher));
                int callerThread = Environment.CurrentManagedThreadId;
                var notifications = new List<(string? Property, int Thread)>();
                pagedList.PropertyChanged += (_, args) => notifications.Add((args.PropertyName, Environment.CurrentManagedThreadId));

                ExecuteAndCheck(pagedList.NextPageCommand, 2, new[] { 3, 4 });
                ExecuteAndCheck(pagedList.LastPageCommand, 3, new[] { 5 });
                Assert.That(pagedList.NextPageCommand.CanExecute(null), Is.False);
                ExecuteAndCheck(pagedList.PrevPageCommand, 2, new[] { 3, 4 });
                ExecuteAndCheck(pagedList.FirstPageCommand, 1, new[] { 1, 2 });
                Assert.That(pagedList.PrevPageCommand.CanExecute(null), Is.False);
                pagedList.CurrentPage = 3;
                ExecuteAndCheck(pagedList.PageByNumberCommand, 3, new[] { 5 });

                void ExecuteAndCheck(System.Windows.Input.ICommand command, int page, int[] items) {
                    notifications.Clear();
                    Task execution = ((AsyncCommandBase)command).ExecuteAsync(null);
                    PumpUntil(() => execution.IsCompleted);
                    execution.GetAwaiter().GetResult();

                    Assert.That(pagedList.CurrentPage, Is.EqualTo(page));
                    Assert.That(pagedList.ItemPage, Is.EqualTo(items));
                    Assert.That(notifications.Select(x => x.Property), Does.Contain(nameof(PagedList<int>.ItemPage)));
                    Assert.That(notifications.Select(x => x.Property), Does.Contain(nameof(PagedList<int>.CurrentPage)));
                    Assert.That(notifications.Select(x => x.Thread), Is.All.EqualTo(callerThread));
                }
            } finally {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        private static void PumpUntil(Func<bool> completed) {
            Stopwatch timeout = Stopwatch.StartNew();
            while (!completed()) {
                if (timeout.Elapsed > TimeSpan.FromSeconds(10)) {
                    throw new TimeoutException("The page navigation operation did not complete.");
                }
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
        }

        private sealed class RecordingSynchronizationContext : SynchronizationContext {
            private int postCount;

            public int PostCount => Volatile.Read(ref postCount);

            public override void Post(SendOrPostCallback callback, object? state) {
                Interlocked.Increment(ref postCount);
                // Drain captured continuations on a worker so the regression fails without hanging the test process.
                ThreadPool.QueueUserWorkItem(_ => {
                    SynchronizationContext? previousContext = Current;
                    SetSynchronizationContext(this);
                    try {
                        callback(state);
                    } finally {
                        SetSynchronizationContext(previousContext);
                    }
                });
            }
        }
    }
}
