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
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ApplicationShellStateBehaviorTest {

        /// <summary>
        /// Verifies version metadata selects the architecture-specific download/checksum fields and constructs site-relative URLs.
        /// </summary>
        [Test]
        public void VersionInfo_UsesArchitectureSpecificDownloadAndChecksumFields() {
            const string json = """
                {
                  "version": "99.0.0.0",
                  "checksum": "0123456789ABCDEF0123456789ABCDEF",
                  "file": "downloads/x64/NINA.zip",
                  "checksum_x86": "FEDCBA9876543210FEDCBA9876543210",
                  "file_x86": "downloads/x86/NINA.zip",
                  "changelog": "changelog/test"
                }
                """;

            VersionCheckVM.VersionInfo sut = JsonConvert.DeserializeObject<VersionCheckVM.VersionInfo>(json);

            sut.GetChangelogUrl().Should().Be("https://nighttime-imaging.eu/changelog/test");
            if (DllLoader.IsX86()) {
                sut.GetChecksum().Should().Be("FEDCBA9876543210FEDCBA9876543210");
                sut.GetFileUrl().Should().Be("https://nighttime-imaging.eu/downloads/x86/NINA.zip");
            } else {
                sut.GetChecksum().Should().Be("0123456789ABCDEF0123456789ABCDEF");
                sut.GetFileUrl().Should().Be("https://nighttime-imaging.eu/downloads/x64/NINA.zip");
            }
            sut.IsNewer().Should().BeTrue();
        }

        /// <summary>
        /// Verifies version comparisons classify clearly older releases as not newer than the running application.
        /// </summary>
        [Test]
        public void VersionInfo_IsNewerReturnsFalseForOlderVersion() {
            var sut = new VersionCheckVM.VersionInfo {
                Version = new Version(0, 0, 0, 0)
            };

            sut.IsNewer().Should().BeFalse();
        }

        /// <summary>
        /// Verifies application status updates add new sources, update existing sources in place, and remove sources when status text is empty.
        /// </summary>
        [Test]
        public void ApplicationStatusVM_StatusUpdateAddsUpdatesAndRemovesBySource() {
            EnsureApplication();
            Application.Current.Resources["ApplicationStatusSVG"] = new GeometryGroup();
            var profileService = new Mock<IProfileService>();
            var mediator = new Mock<IApplicationStatusMediator>();
            var sut = new ApplicationStatusVM(profileService.Object, mediator.Object);
            mediator.Verify(x => x.RegisterHandler(It.Is<IApplicationStatusVM>(vm => ReferenceEquals(vm, sut))), Times.Once);

            sut.StatusUpdate(new ApplicationStatus {
                Source = "Camera",
                Status = "Downloading",
                Progress = 4,
                MaxProgress = 10,
                Status2 = "Cooling",
                Progress2 = 2,
                MaxProgress2 = 5
            });
            DrainDispatcher();

            sut.ApplicationStatus.Should().ContainSingle();
            ApplicationStatus camera = sut.ApplicationStatus.Single();
            camera.Source.Should().Be("Camera");
            camera.Status.Should().Be("Downloading");
            camera.Progress.Should().Be(4);
            camera.Status2.Should().Be("Cooling");

            sut.StatusUpdate(new ApplicationStatus {
                Source = "Camera",
                Status = "Saving",
                Progress = 9,
                MaxProgress = 10,
                Status3 = "Dither",
                Progress3 = 1,
                MaxProgress3 = 1
            });
            DrainDispatcher();

            sut.ApplicationStatus.Should().ContainSingle().Which.Should().BeSameAs(camera);
            camera.Status.Should().Be("Saving");
            camera.Progress.Should().Be(9);
            camera.Status3.Should().Be("Dither");

            sut.StatusUpdate(new ApplicationStatus {
                Source = "Camera",
                Status = string.Empty
            });
            DrainDispatcher();

            sut.ApplicationStatus.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies image geometry lookup returns the exact resource object used by WPF icon bindings.
        /// </summary>
        [Test]
        public void ImageGeometryProvider_ReturnsNamedApplicationResource() {
            EnsureApplication();
            var geometry = new GeometryGroup();
            Application.Current.Resources["CameraSVG"] = geometry;
            var sut = new ImageGeometryProvider();

            sut.GetImageGeometry("CameraSVG").Should().BeSameAs(geometry);
        }

        private static void EnsureApplication() {
            if (Application.Current == null) {
                _ = new Application {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }
        }

        private static void DrainDispatcher() {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }
}
