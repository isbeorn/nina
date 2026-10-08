#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;

namespace NINA.Test.Sequencer {
    [TestFixture, NonParallelizable]
    public class SequenceEntityINPCTest {
        [Test]
        public void RepeatedSerializedNotifications_DoNotAllocateReflectionMetadata() {
            var warmup = new SerializedProbe();
            for (int index = 0; index < 32; index++) warmup.Notify(nameof(SerializedProbe.Value));
            var item = new SerializedProbe();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1024; index++) item.Notify(nameof(SerializedProbe.Value));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            // PropertyChangedEventArgs still allocate, but reflection metadata should be shared.
            allocated.Should().BeLessThan(64 * 1024);
        }

        [Test]
        public void TakeExposure_UpdatesDefaultAndNamedChangeSetsInBothDirections() {
            var root = new SequenceRootContainer();
            var item = CreateExposure();
            root.Add(item);
            ClearChanges(root);

            foreach (int count in new[] { 1, 0 }) {
                item.ExposureCount = count;
                root.HasChanges["Exposures"].Should().BeTrue();
                root.HasChanges[SequenceEntityINPC.defaultChangeSet].Should().BeFalse();
                ClearChanges(root);
            }
            foreach (string imageType in new[] { "DARK", "LIGHT" }) {
                item.ImageType = imageType;
                root.HasChanges[SequenceEntityINPC.defaultChangeSet].Should().BeTrue();
                root.HasChanges["Exposures"].Should().BeFalse();
                ClearChanges(root);
            }

            root.SetChanged();
            item.ExposureCount = 1;
            root.HasChanges["Exposures"].Should().BeFalse("an already-dirty default set keeps the existing short circuit");
            item.Detach();
        }

        [Test]
        public void SharedMetadata_DoesNotShareDirtyStateBetweenInstances() {
            var firstRoot = new SequenceRootContainer();
            var secondRoot = new SequenceRootContainer();
            var first = new SerializedProbe();
            var second = new SerializedProbe();
            firstRoot.Add(first);
            secondRoot.Add(second);
            ClearChanges(firstRoot);
            ClearChanges(secondRoot);

            first.Notify(nameof(SerializedProbe.Value));
            firstRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
            secondRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();

            ClearChanges(firstRoot);
            second.Notify(nameof(SerializedProbe.Value));
            secondRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
            firstRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();
        }

        [Test]
        public void SamePropertyName_UsesItsRuntimeTypeAndInheritedProperties() {
            var root = new SequenceRootContainer();
            var serialized = new SerializedProbe();
            var runtime = new RuntimeProbe();
            root.Add(serialized);
            root.Add(runtime);
            ClearChanges(root);

            serialized.Notify(nameof(SerializedProbe.Value));
            root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
            ClearChanges(root);
            runtime.Notify(nameof(RuntimeProbe.Value));
            root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();
            runtime.Notify(nameof(ProbeItem.InheritedValue));
            root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
        }

        [TestCase(nameof(ProbeItem.Name))]
        [TestCase("MissingProperty")]
        [TestCase("")]
        [TestCase(null)]
        public void NonSerializedOrMissingNotifications_DoNotMarkDirty(string? propertyName) {
            var root = new SequenceRootContainer();
            var item = new SerializedProbe();
            root.Add(item);
            ClearChanges(root);

            item.Notify(propertyName);

            root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();
        }

        [Test]
        public void Notifications_AfterDetachAndReparentOnlyAffectTheCurrentRoot() {
            var firstRoot = new SequenceRootContainer();
            var secondRoot = new SequenceRootContainer();
            var item = new SerializedProbe();
            firstRoot.Add(item);
            item.Notify(nameof(SerializedProbe.Value));
            item.Detach();
            ClearChanges(firstRoot);

            item.Notify(nameof(SerializedProbe.Value));
            firstRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();

            secondRoot.Add(item);
            ClearChanges(secondRoot);
            item.Notify(nameof(SerializedProbe.Value));
            secondRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
            firstRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();

            firstRoot.Add(item);
            ClearChanges(firstRoot);
            ClearChanges(secondRoot);
            item.Notify(nameof(SerializedProbe.Value));
            firstRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
            secondRoot.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();
        }

        [Test]
        public void NamedChangeSet_StillEvaluatesTheAttributeForEachNotification() {
            var root = new SequenceRootContainer();
            var item = new DynamicSetProbe();
            root.Add(item);
            ClearChanges(root);

            try {
                foreach (string name in new[] { "First", "Second" }) {
                    DynamicChangeSetAttribute.CurrentName = name;
                    item.Notify(nameof(DynamicSetProbe.Value));
                    root.DoesHaveChanges(name).Should().BeTrue();
                    root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeFalse();
                    ClearChanges(root);
                }
            } finally {
                DynamicChangeSetAttribute.CurrentName = "Initial";
            }
        }

        private static void ClearChanges(SequenceRootContainer root) {
            foreach (string key in root.HasChanges.Keys) root.HasChanges[key] = false;
        }

        private static TakeExposure CreateExposure() {
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(service => service.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            var camera = Mock.Of<ICameraMediator>(mediator => mediator.GetInfo() == new CameraInfo());
            return new TakeExposure(profiles.Object, camera, Mock.Of<IImagingMediator>(), Mock.Of<IImageSaveMediator>(), Mock.Of<IImageHistoryVM>());
        }

        private class ProbeItem : global::NINA.Sequencer.SequenceItem.SequenceItem {
            [JsonProperty]
            public int InheritedValue { get; set; }

            public void Notify(string? propertyName) => RaisePropertyChanged(propertyName);
            public override object Clone() => new ProbeItem();
            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) => Task.CompletedTask;
        }

        private sealed class SerializedProbe : ProbeItem {
            [JsonProperty]
            public int Value { get; set; }
        }

        private sealed class RuntimeProbe : ProbeItem {
            public int Value { get; set; }
        }

        private sealed class DynamicSetProbe : ProbeItem {
            [JsonProperty, DynamicChangeSet]
            public int Value { get; set; }
        }

        private sealed class DynamicChangeSetAttribute : HasChangedSetAttribute {
            public static string CurrentName { get; set; } = "Initial";
            public DynamicChangeSetAttribute() : base("Unused") { }
            public override string HasChangedSet => CurrentName;
        }
    }
}
