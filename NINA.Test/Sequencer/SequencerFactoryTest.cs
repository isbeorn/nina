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
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ISequenceEntityUpgrader = NINA.Sequencer.ISequenceEntityUpgrader;
using SequencerFactory = NINA.Sequencer.SequencerFactory;
using SidebarEntity = NINA.Sequencer.SidebarEntity;

namespace NINA.Test.Sequencer {

    [TestFixture]
    public class SequencerFactoryTest {
        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;
        private Annotation annotation;
        private LoopCondition condition;
        private UnknownSequenceTrigger trigger;
        private SequentialContainer container;
        private SequencerFactory sut;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);

            annotation = new Annotation { Name = "Annotation", Category = "Utility" };
            condition = new LoopCondition { Name = "Loop", Category = "Condition" };
            trigger = new UnknownSequenceTrigger("MissingTrigger") { Category = "Trigger" };
            container = new SequentialContainer { Name = "Sequential", Category = "Container" };

            sut = new SequencerFactory(
                profileServiceMock.Object,
                new List<ISequenceItem> { annotation },
                new List<ISequenceCondition> { condition },
                new List<ISequenceTrigger> { trigger },
                new List<ISequenceContainer> { container },
                new List<IDateTimeProvider>(),
                new List<ISequenceEntityUpgrader>());
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        /// <summary>
        /// Verifies the Get Methods Return Cloned Prototype Entities By Concrete Type scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void GetMethods_ReturnClonedPrototypeEntitiesByConcreteType() {
            sut.GetItem<Annotation>().Should().NotBeSameAs(annotation);
            sut.GetCondition<LoopCondition>().Should().NotBeSameAs(condition);
            sut.GetTrigger<UnknownSequenceTrigger>().Should().NotBeSameAs(trigger);
            sut.GetContainer<SequentialContainer>().Should().NotBeSameAs(container);

            sut.GetItem<UnknownSequenceItem>().Should().BeNull();
            sut.DateTimeProviders.Should().BeEmpty();
            sut.Upgraders.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the Items View Filters By Name And Enabled State And Settings Mode Shows Disabled Entities scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ItemsView_FiltersByNameAndEnabledStateAndSettingsModeShowsDisabledEntities() {
            SidebarEntity annotationEntity = sut.ItemsView.Cast<SidebarEntity>().Single(x => x.Entity == annotation);

            sut.ViewFilter = "annot";
            sut.ItemsView.Cast<SidebarEntity>().Should().ContainSingle().Which.Entity.Should().BeSameAs(annotation);

            sut.ViewFilter = "missing name";
            sut.ItemsView.Cast<SidebarEntity>().Should().BeEmpty();

            sut.ViewFilter = string.Empty;
            annotationEntity.Enabled = false;
            sut.ItemsView.Refresh();
            sut.InstructionsView.Refresh();

            sut.ItemsView.Cast<SidebarEntity>().Should().NotContain(annotationEntity);
            sut.InstructionsView.Cast<SidebarEntity>().Should().NotContain(x => x.Entity == annotation);

            sut.SettingsMode = true;

            sut.ItemsView.Cast<SidebarEntity>().Should().Contain(annotationEntity);
            sut.InstructionsView.Cast<SidebarEntity>().Should().NotContain(x => x.Entity == annotation);
        }

        /// <summary>
        /// Verifies the Profile Changed Resets Filter And Settings Mode scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void ProfileChanged_ResetsFilterAndSettingsMode() {
            sut.ViewFilter = "annot";
            sut.SettingsMode = true;

            profileServiceMock.Raise(x => x.ProfileChanged += null, EventArgs.Empty);

            sut.ViewFilter.Should().BeEmpty();
            sut.SettingsMode.Should().BeFalse();
        }
    }
}
