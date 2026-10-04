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
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System.Runtime.CompilerServices;

namespace NINA.Test.Sequencer {
    [TestFixture]
    public class ExecutionCleanupTest {
        [TestCase("initialize")]
        [TestCase("run")]
        [TestCase("cancel")]
        [TestCase("success")]
        public void SequenceExit_ReleasesInitializedSubscriptions(string exit) {
            var publisher = new Publisher();
            var reference = RunSequence(publisher, exit);
            AssertReleased(publisher, reference);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RunSequence(Publisher publisher, string exit) {
            var item = new SubscribingItem(publisher, exit == "initialize");
            var root = new Mock<ISequenceRootContainer>();
            root.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem> { item });
            root.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>());
            var sequencer = new NINA.Sequencer.Sequencer(root.Object);
            root.Setup(x => x.Run(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Returns(() => {
                    return exit switch {
                        "run" => Task.FromException(new InvalidOperationException("Execution failed")),
                        "cancel" => Task.FromCanceled(new CancellationToken(true)),
                        _ => Task.CompletedTask
                    };
                });
            if (exit is "initialize" or "run") {
                Assert.ThrowsAsync<InvalidOperationException>(() => sequencer.Start(null, CancellationToken.None, true));
            } else {
                sequencer.Start(null, CancellationToken.None, true).GetAwaiter().GetResult();
            }
            return new WeakReference(item);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void BlockExit_ReleasesInitializedSubscriptions(bool conditional, bool failDuringInitialization) {
            var publisher = new Publisher();
            var reference = RunBlock(publisher, conditional, failDuringInitialization);
            AssertReleased(publisher, reference);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RunBlock(Publisher publisher, bool conditional, bool failDuringInitialization) {
            var item = new SubscribingItem(publisher, failDuringInitialization);
            ISequenceContainer container = conditional ? new ConditionalContainer() : new SequentialContainer();
            if (container is ConditionalContainer condition) condition.PredicateExpression.Definition = "1";
            container.Add(item);
            IExecutionStrategy strategy = conditional ? new ConditionalStrategy() : new SequentialStrategy();
            if (failDuringInitialization) {
                Assert.ThrowsAsync<InvalidOperationException>(() => strategy.Execute(container, null, CancellationToken.None));
            } else {
                strategy.Execute(container, null, CancellationToken.None).GetAwaiter().GetResult();
            }
            return new WeakReference(item);
        }

        private static void AssertReleased(Publisher publisher, WeakReference reference) {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            reference.IsAlive.Should().BeFalse("the publisher must not retain a finished sequence through an event handler");
            publisher.Subscribers.Should().Be(0);
            GC.KeepAlive(publisher);
        }

        private sealed class Publisher {
            public event EventHandler? Changed;
            public int Subscribers => Changed?.GetInvocationList().Length ?? 0;
        }

        private sealed class SubscribingItem(Publisher publisher, bool failDuringInitialization) : NINA.Sequencer.SequenceItem.SequenceItem {
            public override object Clone() => new SubscribingItem(publisher, failDuringInitialization);
            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) => Task.CompletedTask;
            public override void Initialize() {
                publisher.Changed += OnChanged;
                if (failDuringInitialization) throw new InvalidOperationException("Initialization failed");
            }
            public override void Teardown() => publisher.Changed -= OnChanged;
            public override void SequenceBlockInitialize() => Initialize();
            public override void SequenceBlockTeardown() => Teardown();
            private void OnChanged(object? sender, EventArgs args) { }
        }
    }
}