#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Sequencer.Conditions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class ConditionWatchdogTest {

        [Test]
        public void ConditionWatchdog_Ctor_NoTaskStarted_Test() {
            var sut = new ConditionWatchdog(() => Task.CompletedTask, TimeSpan.FromMilliseconds(10));

            sut.WatchdogTask.Should().BeNull();
        }

        [Test]
        public void ConditionWatchdog_Start_NoTaskExists_TaskStarted_Test() {
            var sut = new ConditionWatchdog(() => Task.CompletedTask, TimeSpan.FromMilliseconds(10));

            var task = sut.Start();

            task.Should().NotBeNull();
            sut.WatchdogTask.Should().NotBeNull();
        }

        [Test]
        public void ConditionWatchdog_Start_TaskExists_TaskReturned_Test() {
            var sut = new ConditionWatchdog(() => Task.CompletedTask, TimeSpan.FromMilliseconds(10));

            var task1 = sut.Start();
            var task2 = sut.Start();

            task1.Should().BeSameAs(task2);
            sut.WatchdogTask.Should().NotBeNull();
        }

        [Test]
        public void ConditionWatchdog_Cancel_TaskExists_TaskCanceled_Test() {
            var sut = new ConditionWatchdog(() => Task.CompletedTask, TimeSpan.FromMilliseconds(10));

            var task = sut.Start();
            sut.Cancel();

            while (task.Status == TaskStatus.WaitingForActivation) {
            }
            task.Status.Should().Be(TaskStatus.Canceled);
            sut.WatchdogTask.Should().BeNull();
        }

        [Test]
        public void ConditionWatchdog_Cancel_NoTaskExists_NoOp_Test() {
            var sut = new ConditionWatchdog(() => Task.CompletedTask, TimeSpan.FromMilliseconds(10));

            sut.Cancel();

            sut.WatchdogTask.Should().BeNull();
        }
    }
}