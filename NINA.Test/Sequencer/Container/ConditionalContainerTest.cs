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
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Container {

    [TestFixture]
    public class ConditionalContainerTest {

        [Test]
        public async Task Run_WhenPredicateTrue_RunsInstructionsOnce() {
            ConditionalContainer sut = new ConditionalContainer();
            sut.PredicateExpression.Definition = "1";
            Mock<ISequenceItem> item1 = CreateRunnableItem();
            Mock<ISequenceItem> item2 = CreateRunnableItem();

            sut.Add(item1.Object);
            sut.Add(item2.Object);

            await sut.Run(default, CancellationToken.None);

            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
            item1.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item2.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            item1.Verify(x => x.Skip(), Times.Never);
            item2.Verify(x => x.Skip(), Times.Never);
        }

        [Test]
        public async Task Run_WhenPredicateFalse_SkipsContainerAndCreatedInstructions() {
            ConditionalContainer sut = new ConditionalContainer();
            sut.PredicateExpression.Definition = "0";
            Mock<ISequenceItem> item1 = CreateCreatedItem();
            Mock<ISequenceItem> item2 = CreateCreatedItem();

            sut.Add(item1.Object);
            sut.Add(item2.Object);

            await sut.Run(default, CancellationToken.None);

            sut.Status.Should().Be(SequenceEntityStatus.SKIPPED);
            item1.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
            item2.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
            item1.Verify(x => x.Skip(), Times.Once);
            item2.Verify(x => x.Skip(), Times.Once);
        }

        [Test]
        public async Task Run_WhenPredicateFalse_SkipsNestedCreatedInstructions() {
            ConditionalContainer sut = new ConditionalContainer();
            sut.PredicateExpression.Definition = "0";
            SequentialContainer nestedContainer = new SequentialContainer();
            Mock<ISequenceItem> nestedItem = CreateCreatedItem();

            nestedContainer.Add(nestedItem.Object);
            sut.Add(nestedContainer);

            await sut.Run(default, CancellationToken.None);

            sut.Status.Should().Be(SequenceEntityStatus.SKIPPED);
            nestedContainer.Status.Should().Be(SequenceEntityStatus.SKIPPED);
            nestedItem.Verify(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
            nestedItem.Verify(x => x.Skip(), Times.Once);
        }

        [Test]
        public async Task Run_WhenPredicateMissing_FailsContainer() {
            ConditionalContainer sut = new ConditionalContainer();

            await sut.Run(default, CancellationToken.None);

            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }

        [Test]
        public void Validate_WhenPredicateMissing_ReportsExpressionRequiredIssue() {
            ConditionalContainer sut = new ConditionalContainer();

            bool valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().Contain(Loc.Instance["Lbl_SequenceContainer_ConditionalContainer_ExpressionRequired"]);
        }

        [Test]
        public void Validate_WhenPredicateInvalid_ReportsExpressionIssue() {
            ConditionalContainer sut = new ConditionalContainer();
            sut.PredicateExpression.Definition = "1 +";

            bool valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().Contain(Loc.Instance["LblSyntaxError"]);
        }

        [Test]
        public void Validate_WhenPredicateIsFixedValue_ForcesAnnotatedResult() {
            ConditionalContainer sut = new ConditionalContainer();
            sut.PredicateExpression.Definition = "0";

            bool valid = sut.Validate();

            valid.Should().BeTrue();
            sut.PredicateExpression.ForceAnnotated.Should().BeTrue();
            sut.PredicateExpression.Value.Should().Be(0);
        }

        [Test]
        public void Clone_PreservesNameExpressionAndItems_ButNotTriggersOrConditions() {
            ConditionalContainer sut = new ConditionalContainer {
                Name = "My conditional set"
            };
            sut.PredicateExpression.Definition = "1 + 1";

            Mock<ISequenceItem> item = new Mock<ISequenceItem>();
            Mock<ISequenceItem> itemClone = new Mock<ISequenceItem>();
            item.Setup(x => x.Clone()).Returns(itemClone.Object);
            sut.Add(item.Object);
            sut.Add(Mock.Of<ISequenceCondition>());
            sut.Add(Mock.Of<ISequenceTrigger>());

            ConditionalContainer clone = sut.Clone().Should().BeOfType<ConditionalContainer>().Subject;

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be("My conditional set");
            clone.PredicateExpression.Definition.Should().Be("1 + 1");
            clone.Items.Should().ContainSingle().Which.Should().BeSameAs(itemClone.Object);
            clone.Conditions.Should().BeEmpty();
            clone.Triggers.Should().BeEmpty();
            itemClone.Verify(x => x.AttachNewParent(clone), Times.Once);
        }

        private static Mock<ISequenceItem> CreateCreatedItem() {
            Mock<ISequenceItem> item = new Mock<ISequenceItem>();
            item.Setup(x => x.Status).Returns(SequenceEntityStatus.CREATED);
            return item;
        }

        private static Mock<ISequenceItem> CreateRunnableItem() {
            Mock<ISequenceItem> item = CreateCreatedItem();
            item
                .Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Callback(() => item.Setup(x => x.Status).Returns(SequenceEntityStatus.FINISHED))
                .Returns(Task.CompletedTask);
            return item;
        }
    }
}
