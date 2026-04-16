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
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using OxyPlot;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer {

    [TestFixture]
    public class ControllerAdapterTest {
        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        /// <summary>
        /// Verifies the Templated Sequence Container Clones Container And Honors Collapse Setting scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void TemplatedSequenceContainer_ClonesContainerAndHonorsCollapseSetting() {
            SequentialContainer container = new SequentialContainer {
                Name = "Template",
                IsExpanded = true
            };
            TemplatedSequenceContainer sut = new TemplatedSequenceContainer(profileServiceMock.Object, TemplateController.DefaultTemplatesGroup, container) {
                SubGroups = new[] { "Night", "Luminance" }
            };

            sut.GroupTranslated.Should().Contain("Night").And.Contain("Luminance");
            sut.ToString().Should().Be("Template");
            sut.Parent.Should().BeNull();
            sut.DetachCommand.Should().BeNull();
            sut.MoveUpCommand.Should().BeNull();
            sut.MoveDownCommand.Should().BeNull();

            profile.SequenceSettings.CollapseSequencerTemplatesByDefault = true;
            ISequenceContainer clone = (ISequenceContainer)sut.Clone();

            clone.Should().NotBeSameAs(container);
            clone.Name.Should().Be("Template");
            clone.IsExpanded.Should().BeFalse();

            sut.AfterParentChanged();
            sut.AttachNewParent(new SequentialContainer());
            sut.Detach();
            sut.MoveDown();
            sut.MoveUp();
        }

        /// <summary>
        /// Verifies the Target Sequence Container Exposes Grouping Weight And Collapsed Clone scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void TargetSequenceContainer_ExposesGroupingWeightAndCollapsedClone() {
            DateTime referenceDate = new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc);
            Mock<RiseAndSetEvent> sun = new Mock<RiseAndSetEvent>(referenceDate, 0d, 0d, 0d);
            sun.SetupGet(x => x.Set).Returns(referenceDate.AddHours(18));
            sun.SetupGet(x => x.Rise).Returns(referenceDate.AddDays(1).AddHours(6));
            Mock<IDeepSkyObjectContainer> containerMock = new Mock<IDeepSkyObjectContainer>();
            InputTarget target = new InputTarget(Angle.Zero, Angle.Zero, CustomHorizon.FromReader_Standard(new StringReader("0 0\r\n360 0")));
            Mock<IDeepSkyObject> deepSkyObjectMock = new Mock<IDeepSkyObject>();
            deepSkyObjectMock.SetupGet(x => x.MaxAltitude).Returns(new DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(referenceDate.AddDays(1)), 45));
            target.DeepSkyObject = deepSkyObjectMock.Object;
            containerMock.SetupGet(x => x.Name).Returns("M 31");
            containerMock.SetupGet(x => x.Target).Returns(target);
            containerMock.SetupGet(x => x.NighttimeData).Returns(new NighttimeData(
                referenceDate,
                referenceDate,
                AstroUtil.MoonPhase.NewMoon,
                0,
                null,
                null,
                sun.Object,
                null,
                null));
            Mock<IDeepSkyObjectContainer> cloneMock = new Mock<IDeepSkyObjectContainer>();
            cloneMock.SetupProperty(x => x.IsExpanded, true);
            containerMock.Setup(x => x.Clone()).Returns(cloneMock.Object);
            TargetSequenceContainer sut = new TargetSequenceContainer(profileServiceMock.Object, containerMock.Object) {
                SubGroups = new[] { "Spring" }
            };

            sut.Name.Should().Be("M 31");
            sut.Grouping.Should().Be("Spring");
            sut.Weight.Should().BeGreaterThan(0).And.BeLessThan(1);
            sut.Parent.Should().BeNull();
            sut.DetachCommand.Should().BeNull();
            sut.MoveUpCommand.Should().BeNull();
            sut.MoveDownCommand.Should().BeNull();

            profile.SequenceSettings.CollapseSequencerTemplatesByDefault = true;

            sut.Clone().Should().BeSameAs(cloneMock.Object);
            cloneMock.Object.IsExpanded.Should().BeFalse();
            sut.AfterParentChanged();
            sut.AttachNewParent(new SequentialContainer());
            sut.Detach();
            sut.MoveDown();
            sut.MoveUp();
        }

        /// <summary>
        /// Verifies the Save Sequence Validates Path Creates Directory And Delegates Root Save scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task SaveSequence_ValidatesPathCreatesDirectoryAndDelegatesRootSave() {
            Mock<ISequenceMediator> sequenceMediatorMock = new Mock<ISequenceMediator>();
            SaveSequence sut = new SaveSequence(sequenceMediatorMock.Object);
            SequenceRootContainer root = new SequenceRootContainer { SequenceTitle = "Root" };
            sut.AttachNewParent(root);
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
            sut.FilePath = Path.Combine(directory, "sequence.json");

            sut.Validate().Should().BeTrue();
            sut.Issues.Should().BeEmpty();

            await sut.Execute(Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            Directory.Exists(directory).Should().BeTrue();
            sequenceMediatorMock.Verify(x => x.SaveContainer(root, sut.FilePath, It.IsAny<CancellationToken>()), Times.Once);
            SaveSequence clone = (SaveSequence)sut.Clone();
            clone.FilePath.Should().Be(sut.FilePath);
            clone.ToString().Should().Contain("Path").And.Contain(sut.FilePath);
        }

        /// <summary>
        /// Verifies the Save Sequence Rejects Invalid Path Or Missing Drive scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void SaveSequence_RejectsInvalidPathOrMissingDrive() {
            SaveSequence sut = new SaveSequence(Mock.Of<ISequenceMediator>()) {
                FilePath = "::"
            };

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();

            sut.FilePath = Path.Combine("Z:\\", Guid.NewGuid().ToString("N"), "sequence.json");

            sut.Validate().Should().BeFalse();
            sut.Issues.Should().NotBeEmpty();
        }
    }
}
