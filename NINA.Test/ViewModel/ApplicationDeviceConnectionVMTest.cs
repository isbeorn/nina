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
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NINA.ViewModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.Test.ViewModel {

#pragma warning disable CS0618 // ApplicationDeviceConnectionVM intentionally exposes the legacy AsyncCommand implementation.

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class ApplicationDeviceConnectionVMTest {
        private Mock<ICameraMediator> cameraMediator;
        private Mock<ITelescopeMediator> telescopeMediator;
        private Mock<IDomeMediator> domeMediator;
        private Mock<IFilterWheelMediator> filterWheelMediator;
        private Mock<IFocuserMediator> focuserMediator;
        private Mock<IRotatorMediator> rotatorMediator;
        private Mock<IFlatDeviceMediator> flatDeviceMediator;
        private Mock<IGuiderMediator> guiderMediator;
        private Mock<IWeatherDataMediator> weatherDataMediator;
        private Mock<ISwitchMediator> switchMediator;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediator;
        private Mock<IPluginLoader> pluginLoader;
        private Mock<IUsbDeviceWatcher> usbDeviceWatcher;
        private Window previousMainWindow;
        private Window ownerWindow;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();
            previousMainWindow = Application.Current.MainWindow;
            ownerWindow = new Window {
                ShowActivated = false,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                Width = 1,
                Height = 1,
                Left = -10000,
                Top = -10000
            };
            ownerWindow.Show();
            Application.Current.MainWindow = ownerWindow;

            cameraMediator = new Mock<ICameraMediator>();
            telescopeMediator = new Mock<ITelescopeMediator>();
            domeMediator = new Mock<IDomeMediator>();
            filterWheelMediator = new Mock<IFilterWheelMediator>();
            focuserMediator = new Mock<IFocuserMediator>();
            rotatorMediator = new Mock<IRotatorMediator>();
            flatDeviceMediator = new Mock<IFlatDeviceMediator>();
            guiderMediator = new Mock<IGuiderMediator>();
            weatherDataMediator = new Mock<IWeatherDataMediator>();
            switchMediator = new Mock<ISwitchMediator>();
            safetyMonitorMediator = new Mock<ISafetyMonitorMediator>();
            pluginLoader = new Mock<IPluginLoader>();
            usbDeviceWatcher = new Mock<IUsbDeviceWatcher>();

            cameraMediator.Setup(x => x.GetInfo()).Returns(new CameraInfo());
            telescopeMediator.Setup(x => x.GetInfo()).Returns(new TelescopeInfo());
            domeMediator.Setup(x => x.GetInfo()).Returns(new DomeInfo());
            filterWheelMediator.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo());
            focuserMediator.Setup(x => x.GetInfo()).Returns(new FocuserInfo());
            rotatorMediator.Setup(x => x.GetInfo()).Returns(new RotatorInfo());
            flatDeviceMediator.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo());
            guiderMediator.Setup(x => x.GetInfo()).Returns(new GuiderInfo());
            weatherDataMediator.Setup(x => x.GetInfo()).Returns(new WeatherDataInfo());
            switchMediator.Setup(x => x.GetInfo()).Returns(new SwitchInfo());
            safetyMonitorMediator.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo());

            cameraMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            telescopeMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            domeMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            filterWheelMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            focuserMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            rotatorMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            flatDeviceMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            guiderMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            weatherDataMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            switchMediator.Setup(x => x.Connect()).ReturnsAsync(true);
            safetyMonitorMediator.Setup(x => x.Connect()).ReturnsAsync(true);

            cameraMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            telescopeMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            domeMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            filterWheelMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            focuserMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            rotatorMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            flatDeviceMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            guiderMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            weatherDataMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            switchMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            safetyMonitorMediator.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);

            pluginLoader.Setup(x => x.Load()).Returns(Task.CompletedTask);
        }

        [TearDown]
        public void TearDown() {
            foreach (MyMessageBoxView dialog in Application.Current.Windows.OfType<MyMessageBoxView>().ToArray()) {
                dialog.Close();
            }
            Application.Current.MainWindow = previousMainWindow;
            ownerWindow.Close();
        }

        [Test]
        public async Task ConnectAllDevicesCommand_DisablesDisconnectWhileConnectionIsRunning() {
            TaskCompletionSource<bool> connection = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource connectionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            cameraMediator.Setup(x => x.Connect()).Returns(() => {
                connectionStarted.TrySetResult();
                return connection.Task;
            });
            ApplicationDeviceConnectionVM sut = CreateInitializedSut();

            Task execution = ExecuteConfirmed(sut.ConnectAllDevicesCommand);
            await connectionStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            AsyncCommand<bool> connectCommand = (AsyncCommand<bool>)sut.ConnectAllDevicesCommand;
            connectCommand.Execution.InnerException.Should().BeNull();
            connectCommand.IsRunning.Should().BeTrue();
            sut.ConnectAllDevicesCommand.CanExecute(null).Should().BeFalse();
            sut.DisconnectAllDevicesCommand.CanExecute(null).Should().BeFalse();

            connection.SetResult(true);
            await execution;

            sut.DisconnectAllDevicesCommand.CanExecute(null).Should().BeTrue();
        }

        [Test]
        public async Task DisconnectAllDevicesCommand_DisablesConnectWhileDisconnectionIsRunning() {
            TaskCompletionSource disconnection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource disconnectionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            guiderMediator.Setup(x => x.Disconnect()).Returns(() => {
                disconnectionStarted.TrySetResult();
                return disconnection.Task;
            });
            ApplicationDeviceConnectionVM sut = CreateInitializedSut();

            Task execution = ExecuteConfirmed(sut.DisconnectAllDevicesCommand);
            await disconnectionStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            AsyncCommand<bool> disconnectCommand = (AsyncCommand<bool>)sut.DisconnectAllDevicesCommand;
            disconnectCommand.Execution.InnerException.Should().BeNull();
            disconnectCommand.IsRunning.Should().BeTrue();
            sut.DisconnectAllDevicesCommand.CanExecute(null).Should().BeFalse();
            sut.ConnectAllDevicesCommand.CanExecute(null).Should().BeFalse();

            disconnection.SetResult();
            await execution;

            sut.ConnectAllDevicesCommand.CanExecute(null).Should().BeTrue();
        }

        [Test]
        public void DeviceConnectionCommands_RemainDisabledUntilInitializationCompletes() {
            TaskCompletionSource initialization = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            pluginLoader.Setup(x => x.Load()).Returns(initialization.Task);
            ApplicationDeviceConnectionVM sut = CreateSut();

            sut.ConnectAllDevicesCommand.CanExecute(null).Should().BeFalse();
            sut.DisconnectAllDevicesCommand.CanExecute(null).Should().BeFalse();

            initialization.SetResult();
            SpinWait.SpinUntil(() => sut.Initialized, TimeSpan.FromSeconds(2)).Should().BeTrue();

            sut.ConnectAllDevicesCommand.CanExecute(null).Should().BeTrue();
            sut.DisconnectAllDevicesCommand.CanExecute(null).Should().BeTrue();
        }

        private ApplicationDeviceConnectionVM CreateInitializedSut() {
            ApplicationDeviceConnectionVM sut = CreateSut();

            SpinWait.SpinUntil(() => sut.Initialized, TimeSpan.FromSeconds(2)).Should().BeTrue();
            return sut;
        }

        private ApplicationDeviceConnectionVM CreateSut() {
            return new ApplicationDeviceConnectionVM(
                new Mock<IProfileService>().Object,
                cameraMediator.Object,
                telescopeMediator.Object,
                focuserMediator.Object,
                filterWheelMediator.Object,
                rotatorMediator.Object,
                flatDeviceMediator.Object,
                guiderMediator.Object,
                switchMediator.Object,
                weatherDataMediator.Object,
                domeMediator.Object,
                safetyMonitorMediator.Object,
                pluginLoader.Object,
                usbDeviceWatcher.Object);
        }

        private static Task ExecuteConfirmed(ICommand command) {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                MyMessageBoxView dialog = Application.Current.Windows.OfType<MyMessageBoxView>().Single();
                dialog.DialogResult = true;
            }));
            return ((AsyncCommand<bool>)command).ExecuteAsync(null);
        }

        private static void EnsureApplicationResources() {
            Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            string[] resourceSources = [
                "/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/SVGDictionary.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Converters.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Button.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Path.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TextBlock.xaml",
                "/NINA;component/Resources/Styles/Window.xaml"
            ];

            foreach (string resourceSource in resourceSources) {
                if (!application.Resources.MergedDictionaries.Any(x => x.Source?.OriginalString == resourceSource)) {
                    application.Resources.MergedDictionaries.Add(new ResourceDictionary {
                        Source = new Uri(resourceSource, UriKind.Relative)
                    });
                }
            }

        }
    }

#pragma warning restore CS0618
}
