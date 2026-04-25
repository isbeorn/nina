using FluentAssertions;
using Moq;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [NonParallelizable]
    public class BaseAndDockableVMTest {

        /// <summary>
        /// Verifies that BaseVM reads the active profile from the profile service each time the property is queried.
        /// This protects profile-switch behavior where cached profile references can become stale.
        /// </summary>
        [Test]
        public void ActiveProfile_ReturnsCurrentProfileFromProfileService() {
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            Mock<IProfile> firstProfile = new Mock<IProfile>();
            Mock<IProfile> secondProfile = new Mock<IProfile>();
            IProfile currentProfile = firstProfile.Object;
            BaseVM vm = new BaseVM(profileService.Object);

            profileService.SetupGet(x => x.ActiveProfile).Returns(() => currentProfile);

            vm.ActiveProfile.Should().BeSameAs(firstProfile.Object);
            currentProfile = secondProfile.Object;
            vm.ActiveProfile.Should().BeSameAs(secondProfile.Object);
        }

        /// <summary>
        /// Verifies DockableVM's default dock state and hide command toggle.
        /// This covers the shared defaults inherited by dockable tools and panels throughout the application and plugins.
        /// </summary>
        [Test]
        public void DockableDefaults_AndHideCommand_ToggleVisibility() {
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            EnsurePuzzlePieceResourceIfApplicationExists();
            DockableVM vm = new DockableVM(profileService.Object);

            vm.CanClose.Should().BeTrue();
            vm.IsClosed.Should().BeFalse();
            vm.HasSettings.Should().BeFalse();
            vm.SettingsVisible.Should().BeFalse();
            vm.IsVisible.Should().BeTrue();
            vm.ContentId.Should().Be(nameof(DockableVM));
            vm.IsTool.Should().BeFalse();

            vm.HideCommand.Execute(null);
            vm.IsVisible.Should().BeFalse();

            vm.HideCommand.Execute(null);
            vm.IsVisible.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that profile location changes raise a Title notification on dockable view models.
        /// This protects derived titles that include observer location or profile-dependent display text.
        /// </summary>
        [Test]
        public void LocationChanged_RaisesTitlePropertyChanged() {
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            EnsurePuzzlePieceResourceIfApplicationExists();
            DockableVM vm = new DockableVM(profileService.Object);
            List<string> changedProperties = new List<string>();
            vm.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            profileService.Raise(x => x.LocationChanged += null, EventArgs.Empty);

            changedProperties.Should().Contain(nameof(DockableVM.Title));
        }

        /// <summary>
        /// Verifies that setting observable dock state properties raises the corresponding property names.
        /// This protects bindings in AvalonDock layouts and plugin-provided dockable views.
        /// </summary>
        [Test]
        public void DockableObservableProperties_RaisePropertyChanged() {
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            EnsurePuzzlePieceResourceIfApplicationExists();
            DockableVM vm = new DockableVM(profileService.Object);
            List<string> changedProperties = new List<string>();
            GeometryGroup geometry = new GeometryGroup();
            vm.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            vm.Title = "Guider";
            vm.ImageGeometry = geometry;
            vm.CanClose = false;
            vm.IsClosed = true;
            vm.HasSettings = true;
            vm.SettingsVisible = true;

            changedProperties.Should().Contain(nameof(DockableVM.Title));
            changedProperties.Should().Contain(nameof(DockableVM.ImageGeometry));
            changedProperties.Should().Contain(nameof(DockableVM.CanClose));
            changedProperties.Should().Contain(nameof(DockableVM.IsClosed));
            changedProperties.Should().Contain(nameof(DockableVM.HasSettings));
            changedProperties.Should().Contain(nameof(DockableVM.SettingsVisible));
            vm.ImageGeometry.Should().BeSameAs(geometry);
        }

        private static void EnsurePuzzlePieceResourceIfApplicationExists() {
            if (Application.Current != null && !Application.Current.Resources.Contains("PuzzlePieceSVG")) {
                Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            }
        }
    }
}
