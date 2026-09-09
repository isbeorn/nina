#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"

using ASCOM.Common.DeviceInterfaces;
using Moq;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Dome;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using System.Diagnostics;

namespace NINA.Test.Dome {
    [TestFixture]
    [NonParallelizable]
    public class AscomDomeTest {
        private Mock<IDomeV3> driver = null!;
        private TestDome dome = null!;
        private TaskCompletionSource parkRequested = null!;
        private TaskCompletionSource finalMovementRead = null!;
        private volatile bool atPark;
        private volatile bool atHome;
        private volatile bool slewing;
        private volatile bool shutterCommandSent;
        private volatile ASCOM.Common.DeviceInterfaces.ShutterState shutterState;

        [SetUp]
        public async Task SetUp() {
            atPark = false;
            atHome = false;
            slewing = false;
            shutterCommandSent = false;
            shutterState = ASCOM.Common.DeviceInterfaces.ShutterState.Open;
            parkRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            finalMovementRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            driver = new Mock<IDomeV3>();
            driver.SetupProperty(d => d.Connected, false);
            driver.SetupGet(d => d.CanPark).Returns(true);
            driver.SetupGet(d => d.AtPark).Returns(() => atPark);
            driver.SetupGet(d => d.AtHome).Returns(() => atHome);
            driver.SetupGet(d => d.Slewing).Returns(() => {
                bool value = slewing;
                if (shutterCommandSent) {
                    finalMovementRead.TrySetResult();
                }
                return value;
            });
            driver.SetupGet(d => d.ShutterStatus).Returns(() => shutterState);
            driver.Setup(d => d.Park()).Callback(() => parkRequested.TrySetResult());
            driver.Setup(d => d.AbortSlew()).Callback(() => slewing = false);
            driver.Setup(d => d.CloseShutter()).Callback(() => shutterState = ASCOM.Common.DeviceInterfaces.ShutterState.Closing);
            dome = new TestDome(driver.Object);
            Assert.That(await dome.Connect(CancellationToken.None), Is.True);
        }

        [TearDown]
        public void TearDown() {
            dome.Dispose();
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task Park_MovementStoppedButNotParked_FailsRegardlessOfAtHome(bool home, bool canSetShutter) {
            atHome = home;
            driver.SetupGet(d => d.CanSetShutter).Returns(canSetShutter);
            using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Task parking = dome.Park(cancellation.Token);
            try {
                Assert.ThrowsAsync<InvalidOperationException>(async () => await parking.WaitAsync(TimeSpan.FromSeconds(8)));
                driver.Verify(d => d.Park(), Times.Once);
                driver.Verify(d => d.CloseShutter(), canSetShutter ? Times.Once() : Times.Never());
                driver.Verify(d => d.AbortSlew(), Times.Never);
            } finally {
                await CancelAndObserve(cancellation, parking);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task Park_MovementStoppedAndParked_SucceedsRegardlessOfAtHome(bool home, bool canSetShutter) {
            atHome = home;
            driver.SetupGet(d => d.CanSetShutter).Returns(canSetShutter);
            driver.Setup(d => d.Park()).Callback(() => atPark = true);

            await dome.Park(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(8));

            driver.Verify(d => d.Park(), Times.Once);
            driver.Verify(d => d.CloseShutter(), canSetShutter ? Times.Once() : Times.Never());
            driver.Verify(d => d.AbortSlew(), Times.Never);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Park_AlreadyParked_DoesNotSendParkButPreservesShutterClosure(bool canSetShutter) {
            atPark = true;
            driver.SetupGet(d => d.CanSetShutter).Returns(canSetShutter);

            await dome.Park(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(8));

            driver.Verify(d => d.Park(), Times.Never);
            driver.Verify(d => d.CloseShutter(), canSetShutter ? Times.Once() : Times.Never());
            driver.Verify(d => d.AbortSlew(), Times.Never);
        }

        [TestCase(ASCOM.Common.DeviceInterfaces.ShutterState.Closed)]
        [TestCase(ASCOM.Common.DeviceInterfaces.ShutterState.Closing)]
        public async Task Park_ShutterAlreadyClosedOrClosing_DoesNotSendCloseShutter(ASCOM.Common.DeviceInterfaces.ShutterState state) {
            atPark = true;
            shutterState = state;
            driver.SetupGet(d => d.CanSetShutter).Returns(true);

            await dome.Park(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(8));

            driver.Verify(d => d.CloseShutter(), Times.Never);
        }

        [Test]
        public async Task Park_InitiallyMoving_AbortsBeforeIssuingPark() {
            slewing = true;
            driver.Setup(d => d.Park()).Callback(() => {
                Assert.That(slewing, Is.False);
                atPark = true;
            });

            await dome.Park(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

            driver.Verify(d => d.AbortSlew(), Times.Once);
            driver.Verify(d => d.Park(), Times.Once);
        }

        [Test]
        public async Task Park_StillMoving_DoesNotCheckAtParkUntilMovementStops() {
            KeepMovingAfterShutterCommand();
            using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Task parking = dome.Park(cancellation.Token);
            try {
                await finalMovementRead.Task.WaitAsync(TimeSpan.FromSeconds(8));
                atPark = true;
                Task first = await Task.WhenAny(parking, Task.Delay(TimeSpan.FromSeconds(1)));
                Assert.That(first, Is.Not.SameAs(parking), "AtPark cannot complete parking while the driver is still moving.");
                driver.VerifyGet(d => d.AtPark, Times.Once, "Only the pre-command already-parked check may run before movement stops.");
                slewing = false;
                await parking.WaitAsync(TimeSpan.FromSeconds(5));
                driver.VerifyGet(d => d.AtPark, Times.Exactly(2));
            } finally {
                await CancelAndObserve(cancellation, parking);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task Park_CancelledDuringHelperOrConfirmation_AbortsAndPreservesCancellation(bool duringHelper, bool abortFails) {
            if (!duringHelper) {
                KeepMovingAfterShutterCommand();
            }
            driver.Setup(d => d.Park()).Callback(() => {
                slewing = duringHelper;
                parkRequested.TrySetResult();
            });
            if (abortFails) {
                driver.Setup(d => d.AbortSlew()).Throws(new InvalidOperationException("Abort failed"));
            }
            using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Task parking = dome.Park(cancellation.Token);
            try {
                await (duringHelper ? parkRequested.Task : finalMovementRead.Task).WaitAsync(TimeSpan.FromSeconds(8));
                await cancellation.CancelAsync();

                OperationCanceledException? error = Assert.CatchAsync<OperationCanceledException>(async () => await parking.WaitAsync(TimeSpan.FromSeconds(5)));
                Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token));
                driver.Verify(d => d.AbortSlew(), Times.Once);
            } finally {
                await CancelAndObserve(cancellation, parking);
            }
        }

        [Test]
        public void Park_AlreadyCancelled_DoesNotSendCommands() {
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.CatchAsync<OperationCanceledException>(() => dome.Park(cancellation.Token));

            driver.Verify(d => d.Park(), Times.Never);
            driver.Verify(d => d.AbortSlew(), Times.Never);
            driver.Verify(d => d.CloseShutter(), Times.Never);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Park_ConnectionLostDuringConfirmation_Fails(bool dispose) {
            KeepMovingAfterShutterCommand();
            using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Task parking = dome.Park(cancellation.Token);
            try {
                await finalMovementRead.Task.WaitAsync(TimeSpan.FromSeconds(8));
                if (dispose) {
                    dome.Disconnect();
                } else {
                    driver.Object.Connected = false;
                }
                atPark = true;

                Assert.ThrowsAsync<ASCOM.NotConnectedException>(async () => await parking.WaitAsync(TimeSpan.FromSeconds(5)));
            } finally {
                await CancelAndObserve(cancellation, parking);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Park_ConfirmationReadFails_DoesNotUseCachedSuccess(bool failSlewing) {
            KeepMovingAfterShutterCommand();
            using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Task parking = dome.Park(cancellation.Token);
            try {
                await finalMovementRead.Task.WaitAsync(TimeSpan.FromSeconds(8));
                atPark = true;
                Assert.That(dome.AtPark, Is.True);
                Assert.That(dome.Slewing, Is.True);
                InvalidOperationException expected = new InvalidOperationException("Driver state unavailable");
                if (failSlewing) {
                    driver.SetupGet(d => d.Slewing).Throws(expected);
                } else {
                    driver.SetupGet(d => d.AtPark).Throws(expected);
                    slewing = false;
                }

                Exception? actual = Assert.ThrowsAsync<InvalidOperationException>(async () => await parking.WaitAsync(TimeSpan.FromSeconds(5)));
                Assert.That(actual, Is.SameAs(expected));
            } finally {
                await CancelAndObserve(cancellation, parking);
            }
        }

        [Test]
        public async Task ParkDome_Sequence_WaitsBeforeRunningNextInstructionAndRaisingParked() {
            KeepMovingAfterShutterCommand();
            shutterState = ASCOM.Common.DeviceInterfaces.ShutterState.Closed;
            driver.SetupGet(d => d.CanSetShutter).Returns(true);
            driver.SetupGet(d => d.CanSetAzimuth).Returns(true);
            driver.Setup(d => d.OpenShutter()).Callback(() => shutterState = ASCOM.Common.DeviceInterfaces.ShutterState.Open);
            (DomeVM vm, DomeMediator mediator) = await CreateViewModel();
            int parkedEvents = 0;
            vm.Parked += (_, _) => { Interlocked.Increment(ref parkedEvents); return Task.CompletedTask; };
            ParkDome park = new ParkDome(mediator);
            Mock<SequenceItem> next = new Mock<SequenceItem> { CallBase = true };
            next.Setup(i => i.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            SequentialContainer sequence = new SequentialContainer();
            sequence.Items.Add(new OpenDomeShutter(mediator));
            sequence.Items.Add(new SlewDomeAzimuth(mediator) { AzimuthDegrees = 160 });
            sequence.Items.Add(park);
            sequence.Items.Add(next.Object);
            using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Task running = sequence.Execute(null, cancellation.Token);
            try {
                await finalMovementRead.Task.WaitAsync(TimeSpan.FromSeconds(12));
                atHome = true;
                Task first = await Task.WhenAny(running, Task.Delay(TimeSpan.FromSeconds(1)));
                Assert.That(first, Is.Not.SameAs(running));
                Assert.That(parkedEvents, Is.Zero);
                driver.Verify(d => d.OpenShutter(), Times.Once);
                driver.Verify(d => d.SlewToAzimuth(160), Times.Once);
                driver.Verify(d => d.CloseShutter(), Times.Once);
                next.Verify(i => i.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);

                shutterState = ASCOM.Common.DeviceInterfaces.ShutterState.Closed;
                atPark = true;
                slewing = false;
                await running.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.That(parkedEvents, Is.EqualTo(1));
                next.Verify(i => i.Execute(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            } finally {
                await CancelAndObserve(cancellation, running);
                await vm.Disconnect();
            }
        }

        [TestCase("cancel")]
        [TestCase("driver error")]
        [TestCase("not parked")]
        public async Task ParkDome_CancellationOrFailure_DoesNotRaiseParked(string outcome) {
            KeepMovingAfterShutterCommand();
            (DomeVM vm, DomeMediator mediator) = await CreateViewModel();
            int parkedEvents = 0;
            vm.Parked += (_, _) => { Interlocked.Increment(ref parkedEvents); return Task.CompletedTask; };
            using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Task parking = new ParkDome(mediator).Execute(null, cancellation.Token);
            try {
                await finalMovementRead.Task.WaitAsync(TimeSpan.FromSeconds(8));
                if (outcome == "cancel") {
                    await cancellation.CancelAsync();
                    Assert.CatchAsync<OperationCanceledException>(async () => await parking.WaitAsync(TimeSpan.FromSeconds(5)));
                } else {
                    if (outcome == "driver error") {
                        driver.SetupGet(d => d.AtPark).Throws(new InvalidOperationException("Driver state unavailable"));
                    }
                    slewing = false;
                    Assert.ThrowsAsync<InvalidOperationException>(async () => await parking.WaitAsync(TimeSpan.FromSeconds(5)));
                }
                Assert.That(parkedEvents, Is.Zero);
            } finally {
                await CancelAndObserve(cancellation, parking);
                await vm.Disconnect();
            }
        }

        [Test]
        [Explicit("Exercises the actual fixed 10-minute park timeout. Run explicitly before merging changes to parking.")]
        public async Task ParkDome_MovementNeverStops_TimesOutAfterTenMinutesAndAborts() {
            KeepMovingAfterShutterCommand();
            (DomeVM vm, DomeMediator mediator) = await CreateViewModel();
            int parkedEvents = 0;
            vm.Parked += (_, _) => { Interlocked.Increment(ref parkedEvents); return Task.CompletedTask; };
            using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(11));
            Stopwatch elapsed = Stopwatch.StartNew();
            Task parking = new ParkDome(mediator).Execute(null, cancellation.Token);
            try {
                TimeoutException? error = Assert.ThrowsAsync<TimeoutException>(async () => await parking);

                Assert.That(elapsed.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(599)));
                Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromSeconds(630)));
                Assert.That(error!.Message, Does.Contain("10"));
                Assert.That(parkedEvents, Is.Zero);
                driver.Verify(d => d.AbortSlew(), Times.Once);
            } finally {
                await CancelAndObserve(cancellation, parking);
                await vm.Disconnect();
            }
        }

        private void KeepMovingAfterShutterCommand() {
            driver.SetupGet(d => d.CanSetShutter).Returns(true);
            driver.Setup(d => d.CloseShutter()).Callback(() => {
                shutterState = ASCOM.Common.DeviceInterfaces.ShutterState.Closing;
                slewing = true;
                shutterCommandSent = true;
            });
        }

        private async Task<(DomeVM, DomeMediator)> CreateViewModel() {
            Mock<IProfileService> profile = new Mock<IProfileService>();
            profile.SetupProperty(p => p.ActiveProfile.DomeSettings.Id);
            profile.SetupGet(p => p.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(1);
            Mock<IDeviceChooserVM> chooser = new Mock<IDeviceChooserVM>();
            chooser.SetupGet(c => c.SelectedDevice).Returns(dome);
            Mock<IDeviceUpdateTimer> timer = new Mock<IDeviceUpdateTimer>();
            Mock<IDeviceUpdateTimerFactory> timerFactory = new Mock<IDeviceUpdateTimerFactory>();
            timerFactory.Setup(f => f.Create(It.IsAny<Func<Dictionary<string, object>>>(), It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<double>(), It.IsAny<string>()))
                .Returns(timer.Object);
            DomeMediator mediator = new DomeMediator();
            DomeVM vm = new DomeVM(profile.Object, mediator, Mock.Of<IApplicationStatusMediator>(), Mock.Of<ITelescopeMediator>(),
                chooser.Object, Mock.Of<IDomeFollower>(), Mock.Of<ISafetyMonitorMediator>(), Mock.Of<IApplicationResourceDictionary>(), timerFactory.Object);
            Assert.That(await vm.Connect(), Is.True);
            return (vm, mediator);
        }

        private static async Task CancelAndObserve(CancellationTokenSource cancellation, Task operation) {
            await cancellation.CancelAsync();
            // Observe failures during cleanup without replacing the assertion or driver error under test.
            try { await operation.WaitAsync(TimeSpan.FromSeconds(5)); } catch (Exception) { }
        }

        private sealed class TestDome : AscomDome {
            private readonly IDomeV3 driver;

            public TestDome(IDomeV3 driver) : base("Test.Dome", "Test dome") {
                this.driver = driver;
            }

            protected override IDomeV3 GetInstance() {
                return driver;
            }
        }
    }
}