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
using NINA.Core.Enum;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.ViewModel.FramingAssistant;
using NINA.WPF.Base.Behaviors;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.SkySurvey;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ProfileModel = NINA.Profile.Profile;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class FramingAssistantThreadingTest {

        [TestCase(false)]
        [TestCase(true)]
        public void SetCoordinates_MapInteractionUsesUiOwnedRenderer(bool callFromBackgroundThread) {
            EnsureApplication();
            SynchronizationContext? previousSynchronizationContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Application.Current!.Dispatcher));
            string cachePath = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"FramingAssistantThreadingTest-{Guid.NewGuid():N}");
            Directory.CreateDirectory(cachePath);

            SkyMapRasterRenderer? renderer = null;
            FramingAssistantVM? sut = null;
            try {
                ProfileModel profile = CreateProfile(cachePath);
                Mock<IProfileService> profileService = new Mock<IProfileService>();
                profileService.SetupGet(x => x.ActiveProfile).Returns(profile);

                sut = CreateViewModel(profileService.Object);
                Mock<ISkyMapAnnotator> annotator = CreateAnnotator(
                    () => renderer = new SkyMapRasterRenderer(100, 100),
                    () => RenderEmptyScene(renderer!));
                ReplaceAnnotator(sut, annotator.Object);
                sut.SkySurveyFactory = CreateSkySurveyFactory();
                sut.BoundWidth = 100;
                sut.BoundHeight = 100;

                Coordinates coordinates = new Coordinates(333.7, 73.4, Epoch.J2000, Coordinates.RAType.Degrees);
                DeepSkyObject dso = new DeepSkyObject("API target", coordinates, profile.AstrometrySettings.Horizon);
                Task<bool> setCoordinatesTask = callFromBackgroundThread
                    ? Task.Run(() => sut.SetCoordinates(dso))
                    : sut.SetCoordinates(dso);

                PumpDispatcherUntilCompleted(setCoordinatesTask);
                setCoordinatesTask.GetAwaiter().GetResult().Should().BeTrue();
                renderer.Should().NotBeNull();

                Action interact = () => {
                    sut.MouseWheelCommand.Execute(new MouseWheelResult { Delta = 1 });
                    sut.MouseWheelCommand.Execute(new MouseWheelResult { Delta = -1 });
                    sut.DragStartCommand.Execute(null);
                    sut.DragMoveCommand.Execute(new DragResult { Mode = DragMode.Move, Delta = new Vector(5, -3) });
                    sut.DragStopCommand.Execute(null);
                };

                interact.Should().NotThrow();
            } finally {
                sut?.Dispose();
                renderer?.Dispose();
                try {
                    Directory.Delete(cachePath, recursive: true);
                } catch {
                }
                SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
            }
        }

        private static Mock<ISkyMapAnnotator> CreateAnnotator(
            Action initializeRenderer,
            Action render) {
            Coordinates center = new Coordinates(333.7, 73.4, Epoch.J2000, Coordinates.RAType.Degrees);
            ViewportFoV viewport = new ViewportFoV(center, 5, 100, 100, 0);
            Mock<ISkyMapAnnotator> annotator = new Mock<ISkyMapAnnotator>();
            annotator.SetupGet(x => x.DynamicFoV).Returns(true);
            annotator.SetupGet(x => x.ViewportFoV).Returns(viewport);
            annotator.SetupGet(x => x.Projection).Returns((SkyMapViewportProjection)null!);
            annotator.Setup(x => x.ChangeFoV(It.IsAny<double>())).Returns(viewport);
            annotator.Setup(x => x.ShiftViewport(It.IsAny<Vector>())).Returns(center);
            annotator.Setup(x => x.Initialize(
                    It.IsAny<Coordinates>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CacheSkySurvey>(),
                    It.IsAny<CancellationToken>()))
                .Callback(initializeRenderer)
                .Returns(Task.CompletedTask);
            annotator.Setup(x => x.UpdateSkyMap()).Callback(render);
            annotator.Setup(x => x.EndInteraction()).Callback(render);
            return annotator;
        }

        private static ProfileModel CreateProfile(string cachePath) {
            ProfileModel profile = new ProfileModel();
            profile.ApplicationSettings.SkySurveyCacheDirectory = cachePath;
            profile.FramingAssistantSettings.LastSelectedImageSource = SkySurveySource.NASA;
            profile.FramingAssistantSettings.FieldOfView = 5;
            profile.FramingAssistantSettings.CameraWidth = 1000;
            profile.FramingAssistantSettings.CameraHeight = 800;
            profile.CameraSettings.PixelSize = 3.76;
            profile.TelescopeSettings.FocalLength = 500;
            return profile;
        }

        private static ISkySurveyFactory CreateSkySurveyFactory() {
            byte[] pixels = new byte[100 * 100 * 4];
            BitmapSource image = BitmapSource.Create(
                100,
                100,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                100 * 4);
            image.Freeze();
            SkySurveyImage surveyImage = new SkySurveyImage {
                Coordinates = new Coordinates(333.7, 73.4, Epoch.J2000, Coordinates.RAType.Degrees),
                FoVHeight = 300,
                FoVWidth = 300,
                Image = image,
                Rotation = 0
            };
            Mock<ISkySurvey> survey = new Mock<ISkySurvey>();
            survey.Setup(x => x.GetImage(
                    It.IsAny<string>(),
                    It.IsAny<Coordinates>(),
                    It.IsAny<double>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IProgress<int>>()))
                .ReturnsAsync(surveyImage);
            Mock<ISkySurveyFactory> factory = new Mock<ISkySurveyFactory>();
            factory.Setup(x => x.Create(SkySurveySource.NASA)).Returns(survey.Object);
            return factory.Object;
        }

        private static FramingAssistantVM CreateViewModel(IProfileService profileService) {
            return new FramingAssistantVM(
                profileService,
                Mock.Of<ICameraMediator>(),
                Mock.Of<ITelescopeMediator>(),
                Mock.Of<IApplicationStatusMediator>(),
                Mock.Of<INighttimeCalculator>(),
                Mock.Of<IPlanetariumFactory>(),
                Mock.Of<ISequenceMediator>(),
                Mock.Of<IApplicationMediator>(),
                Mock.Of<IDeepSkyObjectSearchVM>(),
                Mock.Of<IImagingMediator>(),
                Mock.Of<IFilterWheelMediator>(),
                Mock.Of<IGuiderMediator>(),
                Mock.Of<IRotatorMediator>(),
                Mock.Of<IDomeMediator>(),
                Mock.Of<IDomeFollower>(),
                Mock.Of<IImageDataFactory>(),
                Mock.Of<IWindowServiceFactory>());
        }

        private static void EnsureApplication() {
            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.Dispatcher.CheckAccess().Should().BeTrue();
        }

        private static void PumpDispatcherUntilCompleted(Task task) {
            Stopwatch timeout = Stopwatch.StartNew();
            while (!task.IsCompleted) {
                if (timeout.Elapsed > TimeSpan.FromSeconds(10)) {
                    throw new TimeoutException("The Framing Assistant coordinate update did not complete.");
                }
                DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.ApplicationIdle,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
        }

        private static void RenderEmptyScene(SkyMapRasterRenderer renderer) {
            renderer.Should().NotBeNull();
            SkyMapScene scene = new SkyMapScene([], [], [], [], []);
            renderer.Render(scene, [], null);
        }

        private static void ReplaceAnnotator(FramingAssistantVM sut, ISkyMapAnnotator replacement) {
            FieldInfo field = typeof(FramingAssistantVM).GetField(
                "skyMapAnnotator",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            ((ISkyMapAnnotator?)field.GetValue(sut))?.Dispose();
            field.SetValue(sut, replacement);
        }
    }
}
