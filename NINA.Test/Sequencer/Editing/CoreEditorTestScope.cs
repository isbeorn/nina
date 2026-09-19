#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Profile.Interfaces;
using NINA.Profile;
using NINA.Sequencer;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Editing;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.WPF.Base.Utility;
using NINA.Core.Interfaces;
using NUnit.Framework;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Test.Sequencer.Editing {
    // Real core entities and compiled templates, with device/service boundaries mocked.
    internal sealed class CoreEditorTestScope : IDisposable {
        private readonly ResourceDictionary previousResources;
        private readonly Window previousWindow;
        private readonly NINA.Profile.ProfileService sharedProfiles;
        private readonly IProfile previousProfile;
        private readonly NINA.Profile.Profile profile = new();
        private readonly Services services;
        public Window Window { get; }
        public ContentControl Host { get; } = new();
        public SequenceRootContainer Root { get; } = new();
        public SequenceEditHistory History { get; }
        public SequenceEditBehavior Behavior { get; }

        public CoreEditorTestScope() {
            var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            previousResources = app.Resources;
            previousWindow = app.MainWindow;
            app.Resources = new ResourceDictionary();
            profile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("Red", 0, 0));
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("Green", 0, 1));
            services = new Services(profile);
            var shared = new SharedResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml", UriKind.Relative) };
            sharedProfiles = (NINA.Profile.ProfileService)shared["ProfileService"];
            app.Resources.MergedDictionaries.Add(shared);
            previousProfile = sharedProfiles.ActiveProfile;
            typeof(NINA.Profile.ProfileService).GetProperty(nameof(IProfileService.ActiveProfile))!.SetValue(sharedProfiles, profile);
            foreach (string resource in new[] { "StaticResources/SVGDictionary", "StaticResources/Brushes", "StaticResources/Converters", "Styles/TextBox", "Styles/CheckBox", "Styles/Expander", "Styles/Button", "Styles/TabControl", "Styles/ListView" }) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"/NINA.WPF.Base;component/Resources/{resource}.xaml", UriKind.Relative) });
            }
            foreach (string resource in new[] { "ProgressStyle", "SequenceContainerStyles" }) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"/NINA.Sequencer;component/Resources/Styles/{resource}.xaml", UriKind.Relative) });
            }
            // Same exported resource dictionaries the plugin loader merges for built-in entities.
            foreach (Type type in typeof(ISequenceEntity).Assembly.GetTypes().Where(t => typeof(ResourceDictionary).IsAssignableFrom(t)
                && t.GetCustomAttributes<ExportAttribute>().Any())) {
                app.Resources.MergedDictionaries.Add((ResourceDictionary)Activator.CreateInstance(type)!);
            }
            Window = new Window { Width = 1600, Height = 900, Left = -32000, Top = -32000, ShowActivated = false, ShowInTaskbar = false, Content = Host };
            app.MainWindow = Window;
            NameScope.SetNameScope(Window, new NameScope());
            Window.RegisterName("RootGrid", new Grid());
            History = new SequenceEditHistory(Root);
            Behavior = new SequenceEditBehavior { History = History };
            Behavior.Attach(Host);
            SequenceEditContext.SetHistory(Host, History);
            Window.Show();
        }

        public ISequenceEntity Create(Type type) {
            ConstructorInfo constructor = type.GetConstructors().Single(c => c.IsDefined(typeof(ImportingConstructorAttribute)));
            var entity = (ISequenceEntity)constructor.Invoke(constructor.GetParameters().Select(p => services.Get(p.ParameterType)).ToArray());
            entity.Name = type.Name;
            if (entity is SequenceCondition condition) condition.ConditionWatchdog = Moq.Mock.Of<NINA.Sequencer.Interfaces.IConditionWatchdog>();
            if (entity is NINA.Sequencer.Logic.UserSymbol symbol && symbol.Expr == null) {
                symbol.Expr = new NINA.Sequencer.Logic.Expression("1", symbol);
            }
            return entity;
        }

        public void Show(ISequenceEntity entity) {
            if (entity is ISequenceCondition condition) Root.Add(condition);
            else if (entity is ISequenceTrigger trigger) Root.Add(trigger);
            else if (entity is ISequenceItem item && !ReferenceEquals(item, Root)) Root.Add(item);
            Host.DataContext = entity;
            Host.Content = entity;
            Host.ContentTemplate = Application.Current.TryFindResource(new DataTemplateKey(entity.GetType())) as DataTemplate;
            Drain();
        }

        public static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        public static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject {
            if (parent is T match) yield return match;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                foreach (T child in Descendants<T>(VisualTreeHelper.GetChild(parent, i))) yield return child;
            }
        }

        public void Dispose() {
            Behavior.Detach();
            History.Dispose();
            Window.Close();
            foreach (ISequenceItem item in Root.GetItemsSnapshot()) item.Detach();
            foreach (ISequenceCondition condition in Root.GetConditionsSnapshot()) condition.Detach();
            foreach (ISequenceTrigger trigger in Root.GetTriggersSnapshot()) trigger.Detach();
            Application.Current.MainWindow = previousWindow;
            typeof(NINA.Profile.ProfileService).GetProperty(nameof(IProfileService.ActiveProfile))!.SetValue(sharedProfiles, previousProfile);
            Application.Current.Resources = previousResources;
            profile.Dispose();
        }

        private sealed class Services : DefaultValueProvider {
            private readonly Dictionary<Type, object?> values = new();
            public Services(IProfile profile) {
                values[typeof(IProfileService)] = Moq.Mock.Of<IProfileService>(s => s.ActiveProfile == profile && s.Profiles == new AsyncObservableCollection<ProfileMeta> {
                    new ProfileMeta { Id = Guid.NewGuid(), Name = "First" }, new ProfileMeta { Id = Guid.NewGuid(), Name = "Second" }
                });
                values[typeof(IApplicationResourceDictionary)] = Moq.Mock.Of<IApplicationResourceDictionary>(r => r[It.IsAny<string>()] == new GeometryGroup());
                values[typeof(CameraInfo)] = new CameraInfo {
                    Connected = true, CanSetGain = true, CanSetOffset = true, GainMin = 0, GainMax = 300,
                    DefaultGain = 100, Gains = new List<int> { 100, 200 }, XSize = 8000, YSize = 6000,
                    BinningModes = new AsyncObservableCollection<BinningMode> { new(1, 1), new(2, 2) },
                    ReadoutModes = new AsyncObservableCollection<string> { "First", "Second" }
                };
                values[typeof(FlatDeviceInfo)] = new FlatDeviceInfo { Connected = true, SupportsOnOff = true, MinBrightness = 0, MaxBrightness = 255 };
                values[typeof(IList<IDateTimeProvider>)] = new List<IDateTimeProvider> {
                    new NINA.Sequencer.Utility.DateTimeProvider.TimeProvider((INighttimeCalculator)Get(typeof(INighttimeCalculator))!),
                    Moq.Mock.Of<IDateTimeProvider>(p => p.Name == "Fixed time" && p.GetDateTime(It.IsAny<ISequenceEntity>()) == new DateTime(2026, 9, 18, 20, 30, 40))
                };
            }
            public object? Get(Type type) {
                if (values.TryGetValue(type, out object? value)) return value;
                if (type.IsInterface) {
                    var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!;
                    mock.DefaultValueProvider = this;
                    value = mock.Object;
                } else if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null) {
                    value = Activator.CreateInstance(type);
                } else if (type.IsValueType) value = Activator.CreateInstance(type);
                values[type] = value;
                return value;
            }
            protected override object GetDefaultValue(Type type, Mock mock) => Get(type)!;
        }
    }
}