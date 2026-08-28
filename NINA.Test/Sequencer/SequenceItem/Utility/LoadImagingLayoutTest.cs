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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class LoadImagingLayoutTest {
        private string testDirectory;

        [SetUp]
        public void SetUp() {
            testDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, nameof(LoadImagingLayoutTest), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
        }

        [TearDown]
        public void TearDown() {
            if (Directory.Exists(testDirectory)) {
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        public void Clone_CopiesMetadataAndFilePath() {
            var mediator = Mock.Of<IApplicationMediator>();
            var sut = new LoadImagingLayout(mediator) {
                Name = "Layout loader",
                Description = "Loads a layout",
                Icon = new GeometryGroup(),
                FilePath = Path.Combine(testDirectory, "Imaging.dock.config")
            };

            var clone = (LoadImagingLayout)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be(sut.Name);
            clone.Description.Should().Be(sut.Description);
            clone.Icon.Should().BeSameAs(sut.Icon);
            clone.FilePath.Should().Be(sut.FilePath);
        }

        [Test]
        public void GetEstimatedDuration_ReturnsZero() {
            var sut = new LoadImagingLayout(Mock.Of<IApplicationMediator>());

            sut.GetEstimatedDuration().Should().Be(TimeSpan.Zero);
        }

        [TestCase("")]
        [TestCase("missing.dock.config")]
        public void Validate_WhenPathIsEmptyRelativeOrMissing_ReturnsFalse(string filePath) {
            var sut = new LoadImagingLayout(Mock.Of<IApplicationMediator>()) {
                FilePath = filePath
            };

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().ContainSingle();
        }

        [Test]
        public void Validate_WhenFileIsMalformed_ReturnsFalse() {
            string filePath = WriteLayout("malformed.dock.config", "<LayoutRoot>");
            var sut = new LoadImagingLayout(Mock.Of<IApplicationMediator>()) {
                FilePath = filePath
            };

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().ContainSingle();
        }

        [Test]
        public void Validate_WhenRootIsNotLayoutRoot_ReturnsFalse() {
            string filePath = WriteLayout("wrong-root.dock.config", "<NotALayout />");
            var sut = new LoadImagingLayout(Mock.Of<IApplicationMediator>()) {
                FilePath = filePath
            };

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().ContainSingle();
        }

        [Test]
        public void Validate_WhenLayoutRootIsPresent_ReturnsTrue() {
            string filePath = WriteLayout("valid.dock.config", "<LayoutRoot><RootPanel /></LayoutRoot>");
            var sut = new LoadImagingLayout(Mock.Of<IApplicationMediator>()) {
                FilePath = filePath
            };

            sut.Validate().Should().BeTrue();
            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public async Task Execute_ForwardsPathAndCancellationTokenWithoutChangingTabs() {
            string filePath = WriteLayout("valid.dock.config", "<LayoutRoot><RootPanel /></LayoutRoot>");
            var mediator = new Mock<IApplicationMediator>();
            using var cancellationTokenSource = new CancellationTokenSource();
            var sut = new LoadImagingLayout(mediator.Object) {
                FilePath = filePath
            };

            await sut.Execute(Mock.Of<IProgress<ApplicationStatus>>(), cancellationTokenSource.Token);

            mediator.Verify(x => x.LoadImagingLayout(filePath, cancellationTokenSource.Token), Times.Once);
            mediator.Verify(x => x.ChangeTab(It.IsAny<ApplicationTab>()), Times.Never);
        }

        [Test]
        public async Task Execute_WhenMediatorFails_PropagatesFailure() {
            string filePath = WriteLayout("valid.dock.config", "<LayoutRoot><RootPanel /></LayoutRoot>");
            var mediator = new Mock<IApplicationMediator>();
            mediator.Setup(x => x.LoadImagingLayout(filePath, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidDataException("Invalid layout"));
            var sut = new LoadImagingLayout(mediator.Object) {
                FilePath = filePath
            };

            await sut.Invoking(x => x.Execute(null, CancellationToken.None))
                .Should().ThrowAsync<InvalidDataException>();
        }

        [Test]
        public void SequenceJsonConverter_RoundTripsFilePath() {
            string filePath = Path.Combine(testDirectory, "round-trip.dock.config");
            var mediator = Mock.Of<IApplicationMediator>();
            var itemPrototype = new LoadImagingLayout(mediator);
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => new SequentialContainer());
            factory.Setup(x => x.GetItem<LoadImagingLayout>()).Returns(() => (LoadImagingLayout)itemPrototype.Clone());
            var converter = new SequenceJsonConverter(factory.Object);
            var source = new SequentialContainer();
            source.Add(new LoadImagingLayout(mediator) { FilePath = filePath });

            string json = converter.Serialize(source);
            ISequenceContainer result = converter.Deserialize(json);

            var roundTripped = result.Items.Single().Should().BeOfType<LoadImagingLayout>().Subject;
            roundTripped.FilePath.Should().Be(filePath);
        }

        private string WriteLayout(string fileName, string content) {
            string filePath = Path.Combine(testDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }
    }
}
