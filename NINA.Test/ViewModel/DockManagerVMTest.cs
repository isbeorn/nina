#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using AvalonDock;
using AvalonDock.Layout;
using FluentAssertions;
using Moq;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Imaging;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Linq;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [NonParallelizable]
    [Apartment(ApartmentState.STA)]
    public class DockManagerVMTest {
        private string originalProfileFolder;
        private bool originalSingleDockLayout;
        private string testDirectory;
        private NINA.Profile.Profile profile;

        [SetUp]
        public void SetUp() {
            _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            testDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, nameof(DockManagerVMTest), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            originalProfileFolder = ProfileService.PROFILEFOLDER;
            originalSingleDockLayout = NINA.Properties.Settings.Default.SingleDockLayout;
            ProfileService.PROFILEFOLDER = testDirectory;
            NINA.Properties.Settings.Default.SingleDockLayout = false;
            profile = new NINA.Profile.Profile();
        }

        [TearDown]
        public void TearDown() {
            profile.Dispose();
            ProfileService.PROFILEFOLDER = originalProfileFolder;
            NINA.Properties.Settings.Default.SingleDockLayout = originalSingleDockLayout;
            if (Directory.Exists(testDirectory)) {
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        public void LoadImagingLayout_WhenDockIsInitialized_AppliesAndPersistsLayout() {
            WriteProfileLayout(Orientation.Horizontal);
            DockManagerVM sut = CreateSut();
            var dockManager = new DockingManager();
            WaitForTask(sut.InitializeAvalonDockLayout(dockManager));
            string requestedLayout = WriteLayoutFile("requested.dock.config", Orientation.Vertical);

            WaitForTask(sut.LoadImagingLayout(requestedLayout, CancellationToken.None));

            dockManager.Layout.RootPanel.Orientation.Should().Be(Orientation.Vertical);
            ReadLayoutOrientation(DockManagerVM.GetDockConfigPath(profile.Id)).Should().Be(Orientation.Vertical);
        }

        [Test]
        public void LoadImagingLayout_WhenDockIsDeferred_AppliesLayoutOnFirstInitialization() {
            WriteProfileLayout(Orientation.Horizontal);
            DockManagerVM sut = CreateSut();
            string requestedLayout = WriteLayoutFile("deferred.dock.config", Orientation.Vertical);

            WaitForTask(sut.LoadImagingLayout(requestedLayout, CancellationToken.None));
            var dockManager = new DockingManager();
            WaitForTask(sut.InitializeAvalonDockLayout(dockManager));

            dockManager.Layout.RootPanel.Orientation.Should().Be(Orientation.Vertical);
            ReadLayoutOrientation(DockManagerVM.GetDockConfigPath(profile.Id)).Should().Be(Orientation.Vertical);
        }

        [Test]
        public void LoadImagingLayout_WhenLiveDeserializationFails_RestoresPreviousLayout() {
            WriteProfileLayout(Orientation.Horizontal);
            DockManagerVM sut = CreateSut();
            var dockManager = new DockingManager();
            WaitForTask(sut.InitializeAvalonDockLayout(dockManager));
            string invalidLayoutPath = Path.Combine(testDirectory, "invalid.dock.config");
            File.WriteAllText(invalidLayoutPath, "<LayoutRoot><RootPanel Orientation=\"Diagonal\" /></LayoutRoot>");

            Action act = () => WaitForTask(sut.LoadImagingLayout(invalidLayoutPath, CancellationToken.None));

            act.Should().Throw<Exception>();

            dockManager.Layout.RootPanel.Orientation.Should().Be(Orientation.Horizontal);
            ReadLayoutOrientation(DockManagerVM.GetDockConfigPath(profile.Id)).Should().Be(Orientation.Horizontal);
        }

        private DockManagerVM CreateSut() {
            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile);
            var pluginLoader = new Mock<IPluginLoader>();
            pluginLoader.Setup(x => x.Load()).Returns(Task.CompletedTask);
            pluginLoader.SetupGet(x => x.DockableVMs).Returns(CreateLayoutDockables());

            return new DockManagerVM(
                profileService.Object,
                Mock.Of<ICameraVM>(),
                Mock.Of<ISequenceNavigationVM>(),
                Mock.Of<IThumbnailVM>(),
                Mock.Of<ISwitchVM>(),
                Mock.Of<IFilterWheelVM>(),
                Mock.Of<IFocuserVM>(),
                Mock.Of<IRotatorVM>(),
                Mock.Of<IWeatherDataVM>(),
                Mock.Of<IDomeVM>(),
                Mock.Of<IAnchorableSnapshotVM>(),
                Mock.Of<IAnchorablePlateSolverVM>(),
                Mock.Of<ITelescopeVM>(),
                Mock.Of<IGuiderVM>(),
                Mock.Of<IFocusTargetsVM>(),
                Mock.Of<IAutoFocusToolVM>(),
                Mock.Of<IImageHistoryVM>(),
                Mock.Of<IImageControlVM>(),
                Mock.Of<IImageStatisticsVM>(),
                Mock.Of<IFlatDeviceVM>(),
                Mock.Of<ISafetyMonitorVM>(),
                pluginLoader.Object);
        }

        private void WriteProfileLayout(Orientation orientation) {
            File.WriteAllText(DockManagerVM.GetDockConfigPath(profile.Id), CreateLayoutXml(orientation));
        }

        private string WriteLayoutFile(string fileName, Orientation orientation) {
            string filePath = Path.Combine(testDirectory, fileName);
            File.WriteAllText(filePath, CreateLayoutXml(orientation));
            return filePath;
        }

        private static string CreateLayoutXml(Orientation orientation) {
            var document = XDocument.Parse(NINA.Properties.Resources.avalondock);
            document.Root.Element("RootPanel").SetAttributeValue("Orientation", orientation);
            return document.ToString();
        }

        private static Orientation ReadLayoutOrientation(string filePath) {
            var document = XDocument.Load(filePath);
            return Enum.Parse<Orientation>(document.Root.Element("RootPanel").Attribute("Orientation").Value);
        }

        private static List<NINA.Equipment.Interfaces.ViewModel.IDockableVM> CreateLayoutDockables() {
            return XDocument.Parse(NINA.Properties.Resources.avalondock)
                .Descendants()
                .Select(x => x.Attribute("ContentId")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Select(x => {
                    var dockable = new Mock<NINA.Equipment.Interfaces.ViewModel.IDockableVM>();
                    dockable.SetupGet(y => y.ContentId).Returns(x);
                    dockable.SetupProperty(y => y.IsVisible);
                    return dockable.Object;
                })
                .ToList();
        }

        private static void WaitForTask(Task task) {
            if (!task.IsCompleted) {
                var dispatcher = Dispatcher.CurrentDispatcher;
                var frame = new DispatcherFrame();
                _ = task.ContinueWith(
                    _ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)),
                    TaskScheduler.Default);
                Dispatcher.PushFrame(frame);
            }

            task.GetAwaiter().GetResult();
        }
    }
}
