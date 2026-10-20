using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.WPF.Base.Interfaces.Utility;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.View.Options;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System.Diagnostics;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using ProfileModel = NINA.Profile.Profile;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class PlateSolverViewTest {
        private string originalTempPath = null!;
        private string root = null!;
        private ProfileModel profile = null!;
        private IProfile activeProfile = null!;
        private Mock<IProfileService> service = null!;
        private OptionsVM options = null!;
        private PlateSolverView view = null!;
        private Window window = null!;
        private BindingErrors errors = null!;

        [SetUp]
        public void Setup() {
            originalTempPath = CoreUtil.APPLICATIONTEMPPATH;
            root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PlateSolverView", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Profiles"));
            CoreUtil.APPLICATIONTEMPPATH = root;
            profile = new ProfileModel("Solver view");
            activeProfile = profile;
            service = new Mock<IProfileService>();
            service.SetupGet(x => x.ActiveProfile).Returns(() => activeProfile);
            service.SetupGet(x => x.Profiles).Returns(new AsyncObservableCollection<ProfileMeta>());
            PlateSolverFactory.RegisterProviders(Array.Empty<IPlateSolverProvider>());
            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.Resources["ProfileService"] = service.Object;
            if (!app.Resources.Contains("StandardGroupBox")) {
                foreach (string resource in new[] {
                    "StaticResources/SVGDictionary", "StaticResources/Brushes", "StaticResources/Converters",
                    "Styles/Button", "Styles/ToggleButton", "Styles/GroupBox", "Styles/Expander", "Styles/ComboBox"
                }) {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary {
                        Source = new Uri($"/NINA.WPF.Base;component/Resources/{resource}.xaml", UriKind.Relative)
                    });
                }
            }
            options = new OptionsVM(service.Object, Mock.Of<IAllDeviceConsumer>(), Mock.Of<IVersionCheckVM>(), null!,
                Mock.Of<IPlanetariumFactory>(), Mock.Of<IGnssFactory>(),
                Mock.Of<IPluggableBehaviorSelector<IStarDetection>>(), Mock.Of<IPluggableBehaviorSelector<IStarAnnotator>>(),
                Mock.Of<IPluggableBehaviorSelector<IAutoFocusVMFactory>>());
            errors = new BindingErrors();
            PresentationTraceSources.DataBindingSource.Listeners.Add(errors);
            view = new PlateSolverView { DataContext = options };
            window = new Window { Content = view, Width = 1200, Height = 900, Left = -32000, Top = -32000, ShowActivated = false, ShowInTaskbar = false };
            app.MainWindow = window;
            window.Show();
            Drain();
        }

        [TearDown]
        public void Teardown() {
            window?.Close();
            Drain();
            PresentationTraceSources.DataBindingSource.Listeners.Remove(errors);
            errors?.Dispose();
            PlateSolverFactory.RegisterProviders(Array.Empty<IPlateSolverProvider>());
            profile?.Dispose();
            CoreUtil.APPLICATIONTEMPPATH = originalTempPath;
            Directory.Delete(root, true);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealComboBox_SelectsPluginAndBuiltinWithoutChangingOppositeSelection(bool blind) {
            var provider = new ViewProvider("test.solver");
            PlateSolverFactory.RegisterProviders(new[] { provider });
            Drain();
            var settings = profile.PlateSolveSettings;
            settings.PlateSolverProviderId = "unavailable-primary";
            settings.BlindSolverProviderId = "unavailable-blind";
            Drain();
            ComboBox combo = Choice(blind);
            combo.SelectedItem = combo.Items.Cast<KeyValuePair<string, object>>().Single(c => ReferenceEquals(c.Value, provider));
            Drain();

            (blind ? settings.BlindSolverProviderId : settings.PlateSolverProviderId).Should().Be(provider.Id);
            (blind ? settings.PlateSolverProviderId : settings.BlindSolverProviderId).Should().Be(blind ? "unavailable-primary" : "unavailable-blind");
            combo.SelectedItem = combo.Items.Cast<KeyValuePair<string, object>>().Single(c => Equals(c.Value, blind ? (object)BlindSolverEnum.LOCAL : PlateSolverEnum.LOCAL));
            Drain();

            (blind ? settings.BlindSolverProviderId : settings.PlateSolverProviderId).Should().BeNull();
            (blind ? settings.PlateSolverProviderId : settings.BlindSolverProviderId).Should().Be(blind ? "unavailable-primary" : "unavailable-blind");
            errors.Text.Should().BeEmpty();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealComboBox_KeepsSelectedItemAndVisibleLabelAfterBuiltinChange(bool blind) {
            var combo = Choice(blind);
            var expected = combo.Items.Cast<KeyValuePair<string, object>>().Single(c => Equals(c.Value,
                blind ? (object)BlindSolverEnum.LOCAL : PlateSolverEnum.LOCAL));

            var choices = combo.ItemsSource;
            expected.Key.Should().NotBeNullOrWhiteSpace();
            combo.SetCurrentValue(ComboBox.SelectedItemProperty, expected);
            Drain();

            combo.ItemsSource.Should().BeSameAs(choices, "saving a selection must not rebuild the dropdown during its input event");
            combo.SelectedItem.Should().NotBeNull("the selected solver must stay visible after its setting is saved");
            combo.SelectedItem.Should().Be(blind ? options.SelectedBlindSolver : options.SelectedPlateSolver);
            Descendants<TextBlock>(combo).Should().Contain(text => text.Text == expected.Key,
                "the closed selector must display the selected solver's name");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealComboBox_KeyboardSelectionDoesNotLeaveTheFieldBlank(bool blind) {
            var combo = Choice(blind);
            combo.Focus();
            combo.IsDropDownOpen = true;
            Drain();
            foreach (var key in new[] { Key.Home, Key.Down, Key.Enter }) {
                combo.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(combo), 0, key) {
                    RoutedEvent = Keyboard.KeyDownEvent
                });
                Drain();
            }

            combo.SelectedItem.Should().NotBeNull("choosing a solver in the dropdown must not leave its field blank");
            var selected = (KeyValuePair<string, object>)combo.SelectedItem;
            Descendants<TextBlock>(combo).Should().Contain(text => text.Text == selected.Key);
            selected.Value.Should().Be(blind ? (object)BlindSolverEnum.LOCAL : PlateSolverEnum.LOCAL);
        }

        [Test]
        public void RegistryLocaleAndProfileRefresh_PreserveMissingAndUnsupportedChoices() {
            var settings = profile.PlateSolveSettings;
            settings.PlateSolverProviderId = "primary-only";
            settings.BlindSolverProviderId = "primary-only";
            Task.Run(() => PlateSolverFactory.RegisterProviders(new[] { new ViewProvider("primary-only", false) })).GetAwaiter().GetResult();
            Drain();
            options.PlateSolverSettings.Should().BeOfType<ViewProvider>();
            options.BlindSolverSettings.Should().Be("primary-only");
            options.BlindSolverSettings.Should().Be("primary-only");
            service.Raise(x => x.LocaleChanged += null, EventArgs.Empty);
            Drain();
            settings.BlindSolverProviderId.Should().Be("primary-only");
            Choice(false).SelectedItem.Should().Be(options.SelectedPlateSolver);
            Choice(true).SelectedItem.Should().Be(options.SelectedBlindSolver);
            using var second = new ProfileModel("Second profile");
            second.PlateSolveSettings.PlateSolverProviderId = "missing-after-switch";
            activeProfile = second;
            service.Raise(x => x.ProfileChanged += null, EventArgs.Empty);
            Drain();

            options.PlateSolverSettings.Should().Be("missing-after-switch");
            Choice(false).SelectedItem.Should().Be(options.SelectedPlateSolver);
            second.PlateSolveSettings.PlateSolverProviderId.Should().Be("missing-after-switch");
            activeProfile = profile;
            service.Raise(x => x.ProfileChanged += null, EventArgs.Empty);
            Drain();
            settings.PlateSolverProviderId.Should().Be("primary-only");
            settings.BlindSolverProviderId.Should().Be("primary-only");
            errors.Text.Should().BeEmpty();
        }

        [TestCase(PlateSolverEnum.ASTAP, BlindSolverEnum.ASTAP, false)]
        [TestCase(PlateSolverEnum.PINPONT, BlindSolverEnum.PINPOINT, false)]
        [TestCase(PlateSolverEnum.PLATESOLVE3, BlindSolverEnum.ASTAP, true)]
        public void BuiltinSettings_DeduplicateBySolverIdentity(PlateSolverEnum primary, BlindSolverEnum blind, bool visible) {
            profile.PlateSolveSettings.PlateSolverType = primary;
            profile.PlateSolveSettings.BlindSolverType = blind;
            Drain();

            BlindGroup.Visibility.Should().Be(visible ? Visibility.Visible : Visibility.Collapsed);
            BlindGroup.Style.BasedOn.Should().BeSameAs(Application.Current.Resources[typeof(GroupBox)]);
            errors.Text.Should().BeEmpty();
        }

        [Test]
        public void ProviderSettings_UseActualExportTemplateAndDedupeByInstanceInsteadOfName() {
            var first = new ViewProvider("first");
            var second = new ViewProvider("second");
            using var catalog = new TypeCatalog(typeof(ExportedProviderSettingsDictionary));
            using var container = new CompositionContainer(catalog);
            var dictionary = container.GetExportedValue<ResourceDictionary>();
            Application.Current.Resources.MergedDictionaries.Add(dictionary);
            try {
                PlateSolverFactory.RegisterProviders(new[] { first, second });
                var settings = profile.PlateSolveSettings;
                settings.PlateSolverProviderId = first.Id;
                settings.BlindSolverProviderId = first.Id;
                Drain();
                BlindGroup.Visibility.Should().Be(Visibility.Collapsed);
                var editor = Descendants<TextBox>(Settings(false)).Should().ContainSingle().Subject;
                editor.DataContext.Should().BeSameAs(first);
                editor.Text = "changed by editor";
                first.Value.Should().Be("changed by editor");

                settings.BlindSolverProviderId = second.Id;
                Drain();

                BlindGroup.Visibility.Should().Be(Visibility.Visible, "two providers with the same name have different settings");
                Descendants<TextBox>(Settings(true)).Should().ContainSingle().Which.DataContext.Should().BeSameAs(second);
                errors.Text.Should().BeEmpty();
            } finally {
                Application.Current.Resources.MergedDictionaries.Remove(dictionary);
            }
        }

        [Test]
        public void ProviderWithoutTemplate_HasEmptyBodyInsteadOfToString() {
            var provider = new ViewProvider("no-template");
            PlateSolverFactory.RegisterProviders(new[] { provider });
            profile.PlateSolveSettings.PlateSolverProviderId = provider.Id;
            Drain();

            Settings(false).Content.Should().BeSameAs(provider);
            Descendants<TextBlock>(Settings(false)).Should().BeEmpty();
            errors.Text.Should().BeEmpty();
        }

        private ComboBox Choice(bool blind) => (ComboBox)view.FindName(blind ? "BlindSolverChoice" : "PrimarySolverChoice");

        private ContentControl Settings(bool blind) => (ContentControl)view.FindName(blind ? "BlindSolverSettings" : "PrimarySolverSettings");

        private GroupBox BlindGroup => (GroupBox)Settings(true).Parent;

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) {
                    yield return match;
                }

                foreach (var descendant in Descendants<T>(child)) {
                    yield return descendant;
                }
            }
        }

        private static void Drain() {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private sealed class BindingErrors : TraceListener {
            private readonly StringBuilder text = new StringBuilder();

            public string Text => text.ToString();

            public override void Write(string? message) => text.Append(message);

            public override void WriteLine(string? message) => text.AppendLine(message);
        }

        [Export(typeof(ResourceDictionary))]
        public sealed class ExportedProviderSettingsDictionary : ResourceDictionary {
            public ExportedProviderSettingsDictionary() {
                var factory = new FrameworkElementFactory(typeof(TextBox));
                factory.SetBinding(TextBox.TextProperty, new Binding(nameof(ViewProvider.Value)) {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                Add(new DataTemplateKey(typeof(ViewProvider)), new DataTemplate {
                    DataType = typeof(ViewProvider),
                    VisualTree = factory
                });
            }
        }

        public sealed class ViewProvider : IPlateSolverProvider {
            public ViewProvider(string id, bool blind = true) {
                Id = id;
                SupportsBlind = blind;
            }

            public string Id { get; }

            public string Name => "Same display name";

            public bool SupportsPrimary => true;

            public bool SupportsBlind { get; }

            public string Value { get; set; } = "provider option";

            public IPlateSolver CreateSolver(IPlateSolveSettings settings) => throw new InvalidOperationException("The settings view must not create an engine");
        }
    }
}
