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
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Editing;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Serialization;
using NINA.Utility;
using NINA.ViewModel.Sequencer;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;
using System.IO;
using System.Reflection;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequenceEditLifecycleTest {
        [Test]
        public void RootReplacementAndLocking_ManageOneSessionWhileRunAndViewChangesPreserveIt() {
            using var profile = new NINA.Profile.Profile();
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            using var vm = new Sequence2VM(profiles.Object, Mock.Of<ICommandLineOptions>(), Mock.Of<ISequenceMediator>(),
                Mock.Of<IApplicationMediator>(), Mock.Of<IApplicationStatusMediator>(), Mock.Of<ICameraMediator>(),
                Mock.Of<ISequencerFactory>(), Mock.Of<ISymbolBroker>(), Mock.Of<ITemplateLinkResolver>());
            var root = new SequenceRootContainer { Name = "Original" };
            var sequencer = new NINA.Sequencer.Sequencer(root);
            typeof(Sequence2VM).GetProperty(nameof(vm.Sequencer))!.SetValue(vm, sequencer);
            SequenceEditHistory history = vm.EditHistory;
            var item = new SequenceEditHistoryTest.PluginItem();
            root.Add(item);
            item.DisableEnableCommand.Execute(null);
            history.Position.Should().Be(1);
            vm.IsRunning = true;
            vm.SwitchToOverviewCommand.Execute(null);
            vm.IsRunning = false;
            vm.EditHistory.Should().BeSameAs(history);
            vm.LockSequence();
            history.CanUndo.Should().BeFalse();
            vm.UnlockSequence();
            history.Undo().Should().BeTrue();
            sequencer.MainContainer = root;
            vm.EditHistory.Should().BeSameAs(history);
            var replacement = new SequenceRootContainer { Name = "Replacement" };
            sequencer.MainContainer = replacement;
            history.IsEnabled.Should().BeFalse();
            vm.EditHistory.Root.Should().BeSameAs(replacement);
            vm.EditHistory.Position.Should().Be(0);
            vm.SavePath = "replacement.json";
            replacement.SequenceTitle = "Changed";
            vm.SavePath.Should().BeEmpty("the replacement root must retain the existing title listener");
        }

        [Test]
        public void Save_PreservesJournalMarksPositionAndLeavesDirtyTrackingAuthoritative() {
            using var profile = new NINA.Profile.Profile();
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            var factory = Mock.Of<ISequencerFactory>();
            using var vm = new Sequence2VM(profiles.Object, Mock.Of<ICommandLineOptions>(), Mock.Of<ISequenceMediator>(),
                Mock.Of<IApplicationMediator>(), Mock.Of<IApplicationStatusMediator>(), Mock.Of<ICameraMediator>(),
                factory, Mock.Of<ISymbolBroker>(), Mock.Of<ITemplateLinkResolver>());
            var root = new SequenceRootContainer { Name = "Saved" };
            typeof(Sequence2VM).GetProperty(nameof(vm.Sequencer))!.SetValue(vm, new NINA.Sequencer.Sequencer(root));
            typeof(Sequence2VM).GetProperty(nameof(vm.SequenceJsonConverter))!.SetValue(vm, new SequenceJsonConverter(factory));
            string file = Path.Combine(Path.GetTempPath(), "NINA-history-" + Guid.NewGuid().ToString("N") + ".json");
            try {
                vm.SavePath = file;
                var item = new SequenceEditHistoryTest.PluginItem();
                root.Add(item);
                item.DisableEnableCommand.Execute(null);
                SequenceEditHistory history = vm.EditHistory;
                vm.SaveSequenceCommand.Execute(null);
                File.Exists(file).Should().BeTrue();
                File.ReadAllText(file).Should().NotContain("\"EditHistory\":");
                vm.EditHistory.Should().BeSameAs(history);
                history.Entries.Single(x => x.IsCurrent).IsSaved.Should().BeTrue();
                history.Undo().Should().BeTrue();
                history.Redo().Should().BeTrue();
                history.Entries.Single(x => x.IsCurrent).IsSaved.Should().BeTrue();
                root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
            } finally { File.Delete(file); }
        }
    }
}