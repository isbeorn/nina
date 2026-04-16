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
using Moq;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitUntilTest {
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private Mock<ISequenceMediator> sequenceMediatorMock;
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            sequenceMediatorMock = new Mock<ISequenceMediator>();
            profileServiceMock = new Mock<IProfileService>();
        }

        /// <summary>
        /// Verifies the Clone Copies Predicate Expression Independently scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesPredicateExpressionIndependently() {
            WaitUntil sut = CreateSut();
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.PredicateDefinition = "4 + 1";

            WaitUntil clone = (WaitUntil)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Icon.Should().BeSameAs(sut.Icon);
            clone.Predicate.Should().Be(5);
            clone.PredicateExpression.Should().NotBeSameAs(sut.PredicateExpression);
            clone.PredicateExpression.Definition.Should().Be("4 + 1");

            clone.PredicateDefinition = "1";

            sut.PredicateExpression.Definition.Should().Be("4 + 1");
            sut.Predicate.Should().Be(5);
        }

        /// <summary>
        /// Verifies the Validate Syntax Error Returns Issue scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_SyntaxError_ReturnsIssue() {
            WaitUntil sut = CreateSut();
            sut.PredicateDefinition = "1 +";

            bool valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();
        }

        /// <summary>
        /// Verifies the Execute True Predicate Completes Without Waiting scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task Execute_TruePredicate_CompletesWithoutWaiting() {
            WaitUntil sut = CreateSut();
            sut.PredicateDefinition = "1 + 1";
            sut.WaitInterval = TimeSpan.FromSeconds(5);
            AddToParent(sut);

            await sut.Execute(default, CancellationToken.None);
        }

        /// <summary>
        /// Verifies the Execute False Predicate Waits Until Canceled And Reports Status scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task Execute_FalsePredicate_WaitsUntilCanceledAndReportsStatus() {
            WaitUntil sut = CreateSut();
            sut.PredicateDefinition = "0";
            sut.WaitInterval = TimeSpan.FromMilliseconds(10);
            AddToParent(sut);
            List<ApplicationStatus> statuses = new List<ApplicationStatus>();
            Progress<ApplicationStatus> progress = new Progress<ApplicationStatus>(statuses.Add);
            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            Func<Task> act = () => sut.Execute(progress, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            statuses.Should().Contain(s => s.Status == "Waiting...");
        }

        private WaitUntil CreateSut() {
            return new WaitUntil(safetyMonitorMediatorMock.Object, sequenceMediatorMock.Object, profileServiceMock.Object);
        }

        private static void AddToParent(WaitUntil sut) {
            SequenceRootContainer root = new SequenceRootContainer();
            root.Add(sut);
        }
    }
}
