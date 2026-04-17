#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Test.Utility {

    [TestFixture]
    public class NotifyTaskCompletionTest {

        /// <summary>
        /// Verifies that an already-completed task exposes the success state and result immediately.
        /// </summary>
        [Test]
        public void Constructor_CompletedTask_ExposesSuccessfulResult() {
            NotifyTaskCompletion<int> completion = new NotifyTaskCompletion<int>(Task.FromResult(17));

            Assert.That(completion.IsCompleted, Is.True);
            Assert.That(completion.IsSuccessfullyCompleted, Is.True);
            Assert.That(completion.IsNotCompleted, Is.False);
            Assert.That(completion.Result, Is.EqualTo(17));
            Assert.That(completion.ErrorMessage, Is.Null);
        }

        /// <summary>
        /// Verifies that an asynchronously completing task raises the expected property notifications.
        /// </summary>
        [Test]
        public async Task WatchTaskAsync_TaskCompletes_RaisesCompletionNotifications() {
            TaskCompletionSource<string> source = new TaskCompletionSource<string>();
            NotifyTaskCompletion<string> completion = new NotifyTaskCompletion<string>(source.Task);
            List<string> changedProperties = new List<string>();
            completion.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs args) => changedProperties.Add(args.PropertyName);

            source.SetResult("done");
            await completion.TaskCompletion;

            Assert.That(changedProperties, Does.Contain(nameof(NotifyTaskCompletion<string>.Status)));
            Assert.That(changedProperties, Does.Contain(nameof(NotifyTaskCompletion<string>.IsCompleted)));
            Assert.That(changedProperties, Does.Contain(nameof(NotifyTaskCompletion<string>.IsSuccessfullyCompleted)));
            Assert.That(changedProperties, Does.Contain(nameof(NotifyTaskCompletion<string>.Result)));
            Assert.That(completion.Result, Is.EqualTo("done"));
        }

        /// <summary>
        /// Verifies that a faulted task records the inner exception and raises fault-specific notifications.
        /// </summary>
        [Test]
        public async Task WatchTaskAsync_TaskFaults_ExposesErrorMessage() {
            TaskCompletionSource<int> source = new TaskCompletionSource<int>();
            NotifyTaskCompletion<int> completion = new NotifyTaskCompletion<int>(source.Task);
            List<string> changedProperties = new List<string>();
            completion.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs args) => changedProperties.Add(args.PropertyName);

            source.SetException(new InvalidOperationException("camera offline"));
            await completion.TaskCompletion;

            Assert.That(completion.IsFaulted, Is.True);
            Assert.That(completion.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(completion.ErrorMessage, Is.EqualTo("camera offline"));
            Assert.That(completion.Result, Is.EqualTo(default(int)));
            Assert.That(changedProperties, Does.Contain(nameof(NotifyTaskCompletion<int>.IsFaulted)));
            Assert.That(changedProperties, Does.Contain(nameof(NotifyTaskCompletion<int>.ErrorMessage)));
        }
    }
}
