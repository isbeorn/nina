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
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Profile.Interfaces;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class ManualDeviceBehaviorTest {

        /// <summary>
        /// Verifies manual filter wheel connection creates a deterministic default filter when the profile has no filter definitions.
        /// </summary>
        [Test]
        public async Task ManualFilterWheel_ConnectCreatesDefaultFilterForEmptyProfile() {
            var filters = new ObserveAllCollection<FilterInfo>();
            var profile = new Mock<IProfileService>();
            profile.SetupGet(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(filters);
            var sut = new ManualFilterWheel(profile.Object);

            bool connected = await sut.Connect(CancellationToken.None);

            connected.Should().BeTrue();
            sut.Connected.Should().BeTrue();
            filters.Should().ContainSingle();
            filters[0].Position.Should().Be(0);
            filters[0].AutoFocusBinning.X.Should().Be(1);
            sut.Names.Should().ContainSingle(filters[0].Name);
            sut.FocusOffsets.Should().Equal(0);

            sut.Disconnect();
            sut.Connected.Should().BeFalse();
        }

        /// <summary>
        /// Verifies manual rotator sync and relative moves normalize absolute position and select the shortest rotation direction.
        /// </summary>
        [Test]
        public async Task ManualRotator_MoveNormalizesTargetAndUsesShortestPath() {
            var profile = new Mock<IProfileService>();
            var windowService = new Mock<IWindowService>();
            windowService
                .Setup(x => x.ShowDialog(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(), It.IsAny<ICommand>()))
                .Returns(new CompletedDispatcherOperationWrapper());
            var sut = new ManualRotator(profile.Object) {
                WindowService = windowService.Object
            };

            bool connected = await sut.Connect(CancellationToken.None);
            sut.Sync(350);
            bool moved = await sut.Move(20, CancellationToken.None);

            connected.Should().BeTrue();
            moved.Should().BeTrue();
            sut.Position.Should().BeApproximately(10, 1e-6f);
            sut.AbsTargetPosition.Should().BeApproximately(10, 1e-6f);
            sut.TargetPosition.Should().BeApproximately(sut.Position, 1e-6f);
            sut.Rotation.Should().BeApproximately(0, 1e-6f);
            sut.Synced.Should().BeTrue();
            sut.IsMoving.Should().BeFalse();
            windowService.Verify(x => x.ShowDialog(sut, It.IsAny<string>(), ResizeMode.NoResize, WindowStyle.ToolWindow, null), Times.Once);
        }

        /// <summary>
        /// Verifies manual rotator cancellation closes the dialog and leaves the current position unchanged.
        /// </summary>
        [Test]
        public async Task ManualRotator_MoveCancellationClosesDialogAndThrows() {
            var profile = new Mock<IProfileService>();
            var pendingDialog = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var windowService = new Mock<IWindowService>();
            windowService
                .Setup(x => x.ShowDialog(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>(), It.IsAny<ICommand>()))
                .Returns(new TestDispatcherOperationWrapper(pendingDialog.Task));
            windowService.Setup(x => x.Close()).Returns(Task.CompletedTask);
            var sut = new ManualRotator(profile.Object) {
                WindowService = windowService.Object,
                Position = 90
            };
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => sut.Move(45, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            sut.Position.Should().Be(90);
            sut.IsMoving.Should().BeFalse();
            windowService.Verify(x => x.Close(), Times.Once);
        }

        private sealed class CompletedDispatcherOperationWrapper : TestDispatcherOperationWrapper {
            public CompletedDispatcherOperationWrapper() : base(Task.CompletedTask) {
            }
        }

        private class TestDispatcherOperationWrapper : IDispatcherOperationWrapper {
            public TestDispatcherOperationWrapper(Task task) {
                Task = task;
            }

            public Dispatcher Dispatcher => null;
            public DispatcherPriority Priority { get; set; }
            public DispatcherOperationStatus Status => Task.IsCompleted ? DispatcherOperationStatus.Completed : DispatcherOperationStatus.Pending;
            public Task Task { get; }
            public object Result => null;
            public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
            public DispatcherOperationStatus Wait() => Status;
            public DispatcherOperationStatus Wait(TimeSpan timeout) => Status;
            public bool Abort() => false;
            public event EventHandler Aborted { add { } remove { } }
            public event EventHandler Completed { add { } remove { } }
        }
    }
}
