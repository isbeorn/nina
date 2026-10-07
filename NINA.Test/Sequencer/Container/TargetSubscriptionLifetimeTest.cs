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
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace NINA.Test.Sequencer.Container {
    [TestFixture, NonParallelizable]
    public class TargetSubscriptionLifetimeTest {
        private NINA.Profile.Profile profile = null!;
        private IProfileService profileService = null!;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profileService = Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
        }

        [TearDown]
        public void TearDown() {
            profile.Dispose();
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TargetReplacementAcrossThreads_DetachesPreviousAndObservesCurrent(bool constructOnWorker, bool clear) {
            DeepSkyObjectContainer container = OnThread(constructOnWorker, CreateContainer);
            InputTarget oldTarget = container.Target;
            InputTarget? nextTarget = clear ? null : new InputTarget(Angle.Zero, Angle.Zero, null);
            Mock<ISequenceItem> child = new Mock<ISequenceItem>();
            container.Add(child.Object);

            OnThread(!constructOnWorker, () => {
                container.Target = nextTarget;
                container.Target = nextTarget;
                container.Target = nextTarget;
            });
            child.Invocations.Clear();

            oldTarget.TargetName = "Old target";

            child.Verify(item => item.AfterParentChanged(), Times.Never);
            SubscriptionCount(oldTarget).Should().Be(0);
            if (nextTarget != null) {
                nextTarget.TargetName = "Current target";
                child.Verify(item => item.AfterParentChanged(), Times.Once);
            }
            GC.KeepAlive(container);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TargetCloneAcrossThreads_HasIndependentSubscription(bool cloneOnWorker) {
            DeepSkyObjectContainer original = CreateContainer();
            DeepSkyObjectContainer clone = OnThread(cloneOnWorker, () => (DeepSkyObjectContainer)original.Clone());
            Mock<ISequenceItem> originalChild = new Mock<ISequenceItem>();
            Mock<ISequenceItem> cloneChild = new Mock<ISequenceItem>();
            original.Add(originalChild.Object);
            clone.Add(cloneChild.Object);
            originalChild.Invocations.Clear();
            cloneChild.Invocations.Clear();

            original.Target.TargetName = "Original";
            clone.Target.TargetName = "Clone";

            originalChild.Verify(item => item.AfterParentChanged(), Times.Once);
            cloneChild.Verify(item => item.AfterParentChanged(), Times.Once);
            InputTarget oldCloneTarget = clone.Target;
            OnThread(!cloneOnWorker, () => clone.Target = new InputTarget(Angle.Zero, Angle.Zero, null));
            cloneChild.Invocations.Clear();
            oldCloneTarget.TargetName = "Detached clone target";
            cloneChild.Verify(item => item.AfterParentChanged(), Times.Never);
            SubscriptionCount(oldCloneTarget).Should().Be(0);
            GC.KeepAlive(original);
            GC.KeepAlive(clone);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void MaterializedTargetReplacementAcrossThreads_DetachesPreviousAndObservesCurrent(bool constructOnWorker, bool clear) {
            TemplatedSequenceContainer template = CreateTemplate(new TargetContainer());
            LinkedTemplateContainer linked = OnThread(constructOnWorker, () => {
                LinkedTemplateContainer container = new LinkedTemplateContainer();
                container.MaterializeFromTemplate(template);
                return container;
            });
            InputTarget oldTarget = ((IDeepSkyObjectContainer)linked.Items.Single()).Target;
            OnThread(!constructOnWorker, () => {
                if (clear) {
                    linked.OnLinkedTemplateContainerDeserialized(default);
                } else {
                    linked.MaterializeFromTemplate(template);
                }
            });
            int overrideNotifications = 0;
            linked.PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(LinkedTemplateContainer.TargetOverride)) overrideNotifications++;
            };

            oldTarget.TargetName = "Old target";

            overrideNotifications.Should().Be(0);
            SubscriptionCount(oldTarget).Should().Be(0);
            if (!clear) {
                InputTarget currentTarget = ((IDeepSkyObjectContainer)linked.Items.Single()).Target;
                currentTarget.TargetName = "Current target";
                overrideNotifications.Should().Be(1);
                linked.TargetOverride.TargetName.Should().Be("Current target");
            }
            GC.KeepAlive(linked);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LinkedCloneAcrossThreads_HasIndependentSubscription(bool cloneOnWorker) {
            LinkedTemplateContainer original = new LinkedTemplateContainer();
            TemplatedSequenceContainer template = CreateTemplate(new TargetContainer());
            original.MaterializeFromTemplate(template);
            LinkedTemplateContainer clone = OnThread(cloneOnWorker, () => (LinkedTemplateContainer)original.Clone());
            InputTarget originalTarget = ((IDeepSkyObjectContainer)original.Items.Single()).Target;
            InputTarget cloneTarget = ((IDeepSkyObjectContainer)clone.Items.Single()).Target;

            originalTarget.TargetName = "Original";
            cloneTarget.TargetName = "Clone";

            original.TargetOverride.TargetName.Should().Be("Original");
            clone.TargetOverride.TargetName.Should().Be("Clone");
            OnThread(!cloneOnWorker, () => clone.MaterializeFromTemplate(template));
            int overrideNotifications = 0;
            clone.PropertyChanged += (_, args) => {
                if (args.PropertyName == nameof(LinkedTemplateContainer.TargetOverride)) overrideNotifications++;
            };
            cloneTarget.TargetName = "Detached clone target";
            overrideNotifications.Should().Be(0);
            SubscriptionCount(cloneTarget).Should().Be(0);
            GC.KeepAlive(original);
            GC.KeepAlive(clone);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RetainedCurrentTarget_DoesNotKeepContainerAlive(bool linked) {
            (WeakReference owner, InputTarget target) = CreateUnrootedContainer(linked);
            Stopwatch timeout = Stopwatch.StartNew();
            do {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                if (!owner.IsAlive) break;
                Thread.Sleep(10);
            } while (timeout.Elapsed < TimeSpan.FromSeconds(3));

            owner.IsAlive.Should().BeFalse("a target source must only weakly retain its container");
            Action raise = () => target.TargetName = "Owner collected";
            raise.Should().NotThrow();
            GC.KeepAlive(target);
            GC.KeepAlive(profileService);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private (WeakReference, InputTarget) CreateUnrootedContainer(bool linked) {
            DeepSkyObjectContainer container = CreateContainer();
            if (!linked) return (new WeakReference(container), container.Target);
            LinkedTemplateContainer linkedContainer = new LinkedTemplateContainer();
            linkedContainer.MaterializeFromTemplate(CreateTemplate(container));
            return (new WeakReference(linkedContainer), ((IDeepSkyObjectContainer)linkedContainer.Items.Single()).Target);
        }

        private DeepSkyObjectContainer CreateContainer() {
            return new DeepSkyObjectContainer(profileService, Mock.Of<INighttimeCalculator>(),
                Mock.Of<IFramingAssistantVM>(), Mock.Of<IApplicationMediator>(), Mock.Of<IPlanetariumFactory>(),
                Mock.Of<ICameraMediator>(), Mock.Of<IFilterWheelMediator>(), Mock.Of<ISymbolBroker>());
        }

        private TemplatedSequenceContainer CreateTemplate(ISequenceContainer container) {
            return new TemplatedSequenceContainer(profileService, "LblTemplate_UserTemplates", container);
        }

        private static int SubscriptionCount(InputTarget target) {
            FieldInfo field = typeof(InputTarget).GetField(nameof(InputTarget.CoordinatesChanged), BindingFlags.Instance | BindingFlags.NonPublic)!;
            return ((Delegate?)field.GetValue(target))?.GetInvocationList().Length ?? 0;
        }

        private static void OnThread(bool worker, Action action) {
            OnThread(worker, () => {
                action();
                return true;
            });
        }

        private static T OnThread<T>(bool worker, Func<T> action) {
            if (!worker) return action();
            T result = default!;
            Exception? failure = null;
            Thread thread = new Thread(() => {
                try {
                    result = action();
                } catch (Exception ex) {
                    failure = ex;
                }
            }) { IsBackground = true };
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(10)).Should().BeTrue("model subscriptions must not wait for a dispatcher");
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
            return result;
        }

        private sealed class TargetContainer : SequentialContainer, IDeepSkyObjectContainer {
            public InputTarget Target { get; set; } = new InputTarget(Angle.Zero, Angle.Zero, null);
            public NighttimeData NighttimeData => null!;

            public override object Clone() => new TargetContainer();
        }
    }
}
