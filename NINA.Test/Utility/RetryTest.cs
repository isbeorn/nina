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
using System.Threading.Tasks;

namespace NINA.Test.Utility {

    [TestFixture]
    public class RetryTest {

        /// <summary>
        /// Verifies that a transient synchronous failure is retried and the successful attempt result is returned without retrying further.
        /// </summary>
        [Test]
        public async Task Do_GenericActionFailsThenSucceeds_ReturnsSuccessfulResult() {
            int attempts = 0;

            int result = await Retry.Do(() => {
                attempts++;
                if (attempts < 3) {
                    throw new InvalidOperationException($"Attempt {attempts}");
                }

                return 42;
            }, TimeSpan.Zero, 5);

            Assert.That(result, Is.EqualTo(42));
            Assert.That(attempts, Is.EqualTo(3));
        }

        /// <summary>
        /// Verifies that asynchronous retry preserves all failed attempts in the aggregate exception for diagnosability.
        /// </summary>
        [Test]
        public void Do_AsyncActionAlwaysFails_ThrowsAggregateWithEveryFailure() {
            int attempts = 0;

            AggregateException exception = Assert.ThrowsAsync<AggregateException>(async () => {
                await Retry.Do(async () => {
                    await Task.Yield();
                    attempts++;
                    throw new InvalidOperationException($"failure-{attempts}");
                }, TimeSpan.Zero, 3);
            });

            Assert.That(attempts, Is.EqualTo(3));
            Assert.That(exception.InnerExceptions, Has.Count.EqualTo(3));
            Assert.That(exception.InnerExceptions[0].Message, Is.EqualTo("failure-1"));
            Assert.That(exception.InnerExceptions[2].Message, Is.EqualTo("failure-3"));
        }

        /// <summary>
        /// Verifies that a void action uses the same retry policy and stops immediately after the first successful attempt.
        /// </summary>
        [Test]
        public async Task Do_VoidActionFailsOnceThenSucceeds_CompletesAfterSecondAttempt() {
            int attempts = 0;

            await Retry.Do(() => {
                attempts++;
                if (attempts == 1) {
                    throw new ApplicationException("transient");
                }
            }, TimeSpan.Zero, 4);

            Assert.That(attempts, Is.EqualTo(2));
        }
    }
}
