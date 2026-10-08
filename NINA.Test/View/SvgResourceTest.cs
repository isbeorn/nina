using FluentAssertions;
using NINA.CustomControlLibrary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NINA.Test.View {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SvgResourceTest {
        private static ResourceDictionary LoadIcons() => new() {
            Source = new Uri("/NINA.WPF.Base;component/Resources/StaticResources/SVGDictionary.xaml", UriKind.Relative)
        };

        [Test]
        public void CompiledIcons_AreFrozenAndCanBeClonedForModification() {
            var icons = LoadIcons();
            icons.Count.Should().BeGreaterThan(0);
            foreach (var key in icons.Keys) {
                var resource = icons[key].Should().BeAssignableTo<Freezable>().Subject;
                resource.IsFrozen.Should().BeTrue($"shared icon {key} must not retain consumer inheritance contexts");
                resource.Clone().IsFrozen.Should().BeFalse("callers can explicitly request a mutable copy");
            }
        }

        [Test]
        public void FrozenIcon_AllowsSpinnerRotationAndIconReplacement() {
            var icons = LoadIcons();
            var styles = new ResourceDictionary {
                Source = new Uri("/NINA.CustomControlLibrary;component/Themes/Generic.xaml", UriKind.Relative)
            };
            var spinner = new LoadingControl {
                Style = (Style)styles[typeof(LoadingControl)],
                LoadingImage = (Geometry)icons["TrashCanSVG"],
                Width = 40,
                Height = 40
            };
            spinner.ApplyTemplate();
            spinner.Measure(new Size(40, 40));
            spinner.Arrange(new Rect(0, 0, 40, 40));
            var grid = (Grid)spinner.Template.FindName("PART_Grid", spinner);
            var storyboard = (Storyboard)spinner.Template.Resources["Storyboard"];
            storyboard.Begin(spinner, spinner.Template, true);
            try {
                storyboard.SeekAlignedToLastTick(spinner, TimeSpan.FromSeconds(2.5), TimeSeekOrigin.BeginTime);
                ((RotateTransform)grid.RenderTransform).Angle.Should().BeApproximately(180, 5);
                spinner.LoadingImage.Should().BeSameAs(icons["TrashCanSVG"]);
                spinner.LoadingImage = (Geometry)icons["TrashCanOpenSVG"];
                spinner.LoadingImage.Should().BeSameAs(icons["TrashCanOpenSVG"]);
                spinner.LoadingImage.IsFrozen.Should().BeTrue();
                spinner.LoadingImage = (Geometry)icons["TrashCanSVG"];
                spinner.LoadingImage.Should().BeSameAs(icons["TrashCanSVG"]);
            } finally {
                storyboard.Remove(spinner);
            }
        }
    }
}