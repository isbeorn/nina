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
using Newtonsoft.Json.Linq;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyGuider.MetaGuide;
using NINA.Equipment.Equipment.MyGuider.PHD2;
using NINA.Profile.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace NINA.Test.Equipment {
    [TestFixture]
    public class GuiderSynchronizationContextTest {
        [TestCase(false)]
        [TestCase(true)]
        public void Phd2GetLockPosition_DeferredResponseCompletesWithoutPumpingCallerContext(bool transportFails) {
            var guider = new PHD2Guider(Mock.Of<IProfileService>(), Mock.Of<IWindowServiceFactory>());
            using var writer = new StreamWriter(new MemoryStream());
            SetField(guider, "_connected", true);
            SetField(guider, "_writer", writer);
            var pending = (ConcurrentDictionary<string, TaskCompletionSource<JObject>>)typeof(PHD2Guider)
                .GetField("_pending", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(guider)!;

            using var context = new NonPumpingContext();
            Task<LockPosition> operation = StartOnContext(context, guider.GetLockPosition);
            operation.IsCompleted.Should().BeFalse("the RPC response has not arrived yet");
            var response = pending.Single().Value;
            if (transportFails) {
                response.SetException(new IOException("The test transport disconnected."));
            } else {
                response.SetResult(JObject.Parse("""{"result":[12.5,21.75]}"""));
            }

            try {
                operation.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue(
                    "the synchronous guider facade must not need its caller's context to finish an RPC");
                LockPosition position = operation.GetAwaiter().GetResult();
                if (transportFails) {
                    position.Should().BeNull();
                } else {
                    position.Should().Be(new LockPosition(12.5f, 21.75f));
                }
                pending.Should().BeEmpty();
            } finally {
                context.Dispose();
                operation.Wait(TimeSpan.FromSeconds(5));
            }
        }

        [Test]
        public void MetaGuideListener_FailedWorkerCompletesWithoutPumpingCallerContext() {
            var listener = new MetaGuideListener();
            using var workerStopping = new ManualResetEventSlim();
            using var allowWorkerToStop = new ManualResetEventSlim();
            listener.OnDisconnected += () => {
                workerStopping.Set();
                allowWorkerToStop.Wait(TimeSpan.FromSeconds(5));
            };

            using var context = new NonPumpingContext();
            // An invalid port fails before any socket is bound. The callback gate ensures
            // the worker is still incomplete when RunListener reaches its outer await.
            Task operation = StartOnContext(context, () => listener.RunListener(false, -1, CancellationToken.None));
            try {
                workerStopping.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                allowWorkerToStop.Set();
                SpinWait.SpinUntil(() => operation.IsCompleted, TimeSpan.FromSeconds(2)).Should().BeTrue(
                    "synchronous disconnect must be able to observe listener completion without pumping its context");
                operation.IsFaulted.Should().BeTrue();
                operation.Exception!.GetBaseException().Should().BeOfType<ArgumentOutOfRangeException>();
            } finally {
                allowWorkerToStop.Set();
                context.Dispose();
                SpinWait.SpinUntil(() => operation.IsCompleted, TimeSpan.FromSeconds(5));
                _ = operation.Exception;
            }
        }

        [Test]
        public void MetaGuideListener_PreCanceledStartPreservesCancellation() {
            var listener = new MetaGuideListener();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var disconnected = false;
            listener.OnDisconnected += () => disconnected = true;

            using var context = new NonPumpingContext();
            Task operation = StartOnContext(context, () => listener.RunListener(false, 0, cancellation.Token));

            operation.IsCanceled.Should().BeTrue();
            disconnected.Should().BeFalse("the canceled worker never started");
        }

        private static void SetField(PHD2Guider guider, string name, object value) {
            typeof(PHD2Guider).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(guider, value);
        }

        private static T StartOnContext<T>(SynchronizationContext context, Func<T> start) {
            SynchronizationContext? previous = SynchronizationContext.Current;
            try {
                SynchronizationContext.SetSynchronizationContext(context);
                return start();
            } finally {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        private sealed class NonPumpingContext : SynchronizationContext, IDisposable {
            private readonly object gate = new();
            private readonly Queue<(SendOrPostCallback Callback, object? State)> callbacks = new();
            private bool released;

            public override void Post(SendOrPostCallback callback, object? state) {
                lock (gate) {
                    if (released) {
                        ThreadPool.QueueUserWorkItem(_ => callback(state));
                    } else {
                        callbacks.Enqueue((callback, state));
                    }
                }
            }

            public void Dispose() {
                lock (gate) {
                    released = true;
                    while (callbacks.TryDequeue(out var callback)) {
                        ThreadPool.QueueUserWorkItem(_ => callback.Callback(callback.State));
                    }
                }
            }
        }
    }
}
