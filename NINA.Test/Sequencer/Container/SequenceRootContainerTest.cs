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
using NINA.Core.Model;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Container {

    [TestFixture]
    public class SequenceRootContainerTest {

        /// <summary>
        /// Verifies the Sequence Title Tracks Name And Change Sets scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SequenceTitle_TracksNameAndChangeSets() {
            SequenceRootContainer sut = new SequenceRootContainer {
                Name = "Root"
            };

            sut.SequenceTitle.Should().Be("Root");
            sut.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();

            sut.SequenceTitle = "Plan";
            sut.SetChanged();
            sut.SetChanged("templates");

            sut.Name.Should().Be("Plan");
            sut.SequenceTitle.Should().Be("Plan");
            sut.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
            sut.DoesHaveChanges("templates").Should().BeTrue();
            sut.DoesHaveChanges("missing").Should().BeFalse();
        }

        /// <summary>
        /// Verifies the Running Items Are Tracked Skipped And Removed scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void RunningItems_AreTrackedSkippedAndRemoved() {
            SequenceRootContainer sut = new SequenceRootContainer();
            Mock<ISequenceItem> first = new Mock<ISequenceItem>();
            Mock<ISequenceItem> second = new Mock<ISequenceItem>();

            sut.AddRunningItem(first.Object);
            sut.AddRunningItem(second.Object);

            sut.GetCurrentRunningItems().Should().BeEquivalentTo(new[] { first.Object, second.Object });

            sut.SkipCurrentRunningItems();

            first.Verify(x => x.Skip(), Times.Once);
            second.Verify(x => x.Skip(), Times.Once);

            sut.RemoveRunningItem(first.Object);

            sut.GetCurrentRunningItems().Should().ContainSingle().Which.Should().BeSameAs(second.Object);
        }

        /// <summary>
        /// Verifies the Raise Failure Event Notifies Subscribers And Swallows Subscriber Exceptions scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task RaiseFailureEvent_NotifiesSubscribersAndSwallowsSubscriberExceptions() {
            SequenceRootContainer sut = new SequenceRootContainer();
            Mock<ISequenceItem> sender = new Mock<ISequenceItem>();
            Exception failure = new InvalidOperationException("boom");
            bool observed = false;

            sut.FailureEvent += (s, args) => {
                s.Should().BeSameAs(sender.Object);
                args.Entity.Should().BeSameAs(sender.Object);
                args.Exception.Should().BeSameAs(failure);
                observed = true;
                return Task.CompletedTask;
            };
            sut.FailureEvent += (s, args) => throw new Exception("subscriber failure");

            Func<Task> act = () => sut.RaiseFailureEvent(sender.Object, failure);

            await act.Should().NotThrowAsync();
            observed.Should().BeTrue();
        }

        /// <summary>
        /// Verifies the Clone Copies Metadata Only scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesMetadataOnly() {
            SequenceRootContainer sut = new SequenceRootContainer {
                Name = "Root",
                Category = "Category",
                Description = "Description",
                Icon = new System.Windows.Media.GeometryGroup()
            };

            SequenceRootContainer clone = (SequenceRootContainer)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be(sut.Name);
            clone.Category.Should().Be(sut.Category);
            clone.Description.Should().Be(sut.Description);
            clone.Icon.Should().BeSameAs(sut.Icon);
            clone.Items.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies deserialization clears any constructor-created or stale child containers before JSON items are populated.
        /// </summary>
        [Test]
        public void OnDeserializing_ClearsExistingItems() {
            SequenceRootContainer sut = new SequenceRootContainer();
            sut.Add(new Annotation());

            sut.OnDeserializing(new StreamingContext());

            sut.Items.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the root drop command delegates dropped sequence items into the target-area container.
        /// </summary>
        [Test]
        public void DropIntoCommand_DelegatesDropIntoTargetAreaContainer() {
            SequenceRootContainer sut = new SequenceRootContainer();
            SequentialContainer start = new SequentialContainer();
            TargetAreaContainer targetArea = new TargetAreaContainer();
            SequentialContainer end = new SequentialContainer();
            Mock<ISequenceItem> source = new Mock<ISequenceItem>();
            Mock<ISequenceItem> clone = new Mock<ISequenceItem>();
            source.Setup(x => x.Clone()).Returns(clone.Object);
            sut.Add(start);
            sut.Add(targetArea);
            sut.Add(end);

            sut.DropIntoCommand.Execute(new DropIntoParameters(source.Object, null, DropTargetEnum.Center));

            targetArea.Items.Should().ContainSingle().Which.Should().BeSameAs(clone.Object);
            clone.Verify(x => x.AttachNewParent(targetArea), Times.Once);
        }
    }
}
