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
using System.Runtime.CompilerServices;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequenceEditLifecycleTest {
        [Test]
        public void RootReplacement_ReleasesPreviousRootChildrenAndHistory() {
            using var profile = new NINA.Profile.Profile();
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            using var vm = new Sequence2VM(profiles.Object, Mock.Of<ICommandLineOptions>(), Mock.Of<ISequenceMediator>(),
                Mock.Of<IApplicationMediator>(), Mock.Of<IApplicationStatusMediator>(), Mock.Of<ICameraMediator>(),
                Mock.Of<ISequencerFactory>(), Mock.Of<ISymbolBroker>(), Mock.Of<ITemplateLinkResolver>());
            var references = ReplaceRootWithHistory(vm);

            SequencerLifetimeTest.AssertCollected(references);
            GC.KeepAlive(vm);
            GC.KeepAlive(profiles);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (string, WeakReference)[] ReplaceRootWithHistory(Sequence2VM vm) {
            var root = new SequenceRootContainer();
            var item = new NINA.Sequencer.SequenceItem.Utility.WaitForTimeSpan();
            root.Add(item);
            var sequencer = new NINA.Sequencer.Sequencer(root);
            typeof(Sequence2VM).GetProperty(nameof(vm.Sequencer))!.SetValue(vm, sequencer);
            var history = vm.EditHistory;
            item.DetachCommand.Execute(null);
            history.Position.Should().Be(1);
            sequencer.MainContainer = new SequenceRootContainer();
            return new[] { ("previous root", new WeakReference(root)), ("deleted item", new WeakReference(item)),
                ("previous history", new WeakReference(history)) };
        }

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
        public void LoadFromFile_DoesNotAllocateTheWholeFileAsText() {
            using var profile = new NINA.Profile.Profile();
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => new SequenceRootContainer());
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => new SequentialContainer());
            using var vm = new Sequence2VM(profiles.Object, Mock.Of<ICommandLineOptions>(), Mock.Of<ISequenceMediator>(),
                Mock.Of<IApplicationMediator>(), Mock.Of<IApplicationStatusMediator>(), Mock.Of<ICameraMediator>(),
                factory.Object, Mock.Of<ISymbolBroker>(), Mock.Of<ITemplateLinkResolver>());
            typeof(Sequence2VM).GetProperty(nameof(vm.Sequencer))!.SetValue(vm, new NINA.Sequencer.Sequencer(new SequenceRootContainer()));
            var converter = new SequenceJsonConverter(factory.Object);
            typeof(Sequence2VM).GetProperty(nameof(vm.SequenceJsonConverter))!.SetValue(vm, converter);
            var load = typeof(Sequence2VM).GetMethod("LoadSequenceFromFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var source = new SequenceRootContainer();
            source.Add(new SequentialContainer { Name = "Target \u03b1", IsExpanded = false });
            string json = converter.Serialize(source);
            string file = Path.Combine(Path.GetTempPath(), "NINA-stream-load-" + Guid.NewGuid().ToString("N") + ".json");
            object[] arguments = { file };
            try {
                File.WriteAllText(file, json);
                load.Invoke(vm, arguments); // Warm up serializer contracts and the view-model load path.
                var previousRoot = vm.Sequencer.MainContainer;
                using (var writer = new StreamWriter(file, false, new System.Text.UTF8Encoding(true))) {
                    writer.Write(json);
                    string padding = new string(' ', 4096);
                    for (int i = 0; i < 1024; i++) writer.Write(padding);
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                load.Invoke(vm, arguments);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

                vm.Sequencer.MainContainer.Should().NotBeSameAs(previousRoot);
                vm.Sequencer.MainContainer.Items.Should().ContainSingle();
                var target = vm.Sequencer.MainContainer.Items[0].Should().BeOfType<SequentialContainer>().Subject;
                target.Name.Should().Be("Target \u03b1");
                target.IsExpanded.Should().BeFalse();
                target.Parent.Should().BeSameAs(vm.Sequencer.MainContainer);
                vm.SavePath.Should().Be(file);
                vm.EditHistory.Root.Should().BeSameAs(vm.Sequencer.MainContainer);
                allocated.Should().BeLessThan(4 * 1024 * 1024,
                    "reading a padded sequence should use bounded buffers, not allocate a string for the complete file");
            } finally { File.Delete(file); }
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