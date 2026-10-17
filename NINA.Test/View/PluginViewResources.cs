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
using NINA.Core.Utility.ColorSchema;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Utility;
using System.Collections;
using System.Windows;

namespace NINA.Test.View {

    internal sealed class PluginViewResources : IDisposable {
        private readonly Application application;
        private readonly Dictionary<object, object> originalResources;
        private readonly ResourceDictionary[] originalDictionaries;
        private readonly Dictionary<Uri, WeakReference> originalSharedDictionaries;
        private readonly Window? originalMainWindow;
        private readonly NINA.Profile.Profile profile;
        private readonly ColorSchema initialColorSchema;
        private readonly Dictionary<object, object> fixtureResources;
        private readonly ResourceDictionary[] fixtureDictionaries;
        private readonly Dictionary<Uri, WeakReference> fixtureSharedDictionaries;

        public PluginViewResources() {
            application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.Dispatcher.VerifyAccess();
            originalResources = application.Resources.Cast<DictionaryEntry>().ToDictionary(entry => entry.Key, entry => entry.Value!);
            originalDictionaries = application.Resources.MergedDictionaries.ToArray();
            originalSharedDictionaries = SharedResourceDictionary.SharedDictionaries.ToDictionary(entry => entry.Key, entry => entry.Value);
            originalMainWindow = application.MainWindow;
            profile = new NINA.Profile.Profile();
            initialColorSchema = profile.ColorSchemaSettings.ColorSchema;

            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
            SharedResourceDictionary.SharedDictionaries.Clear();
            var profiles = Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
            application.Resources["ProfileService"] = profiles;
            var profileResources = new ResourceDictionary { ["ProfileService"] = profiles };
            application.Resources.MergedDictionaries.Add(profileResources);
            SharedResourceDictionary.SharedDictionaries[new Uri("/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml", UriKind.Relative)] = new WeakReference(profileResources);
            string[] sources = [
                "/NINA.WPF.Base;component/Resources/StaticResources/SVGDictionary.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Converters.xaml",
                "/NINA;component/Resources/StaticResources/DataTemplates.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Button.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Path.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TextBlock.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TextBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TabControl.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/CheckBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ListView.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/GroupBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ScrollViewer.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ComboBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/GridSplitter.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/ProgressBar.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Tooltip.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Hyperlink.xaml",
                "/NINA;component/Resources/Styles/Markdown.xaml"
            ];
            try {
                foreach (string source in sources) {
                    application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
                }

                fixtureResources = application.Resources.Cast<DictionaryEntry>().ToDictionary(entry => entry.Key, entry => entry.Value!);
                fixtureDictionaries = application.Resources.MergedDictionaries.ToArray();
                fixtureSharedDictionaries = SharedResourceDictionary.SharedDictionaries.ToDictionary(entry => entry.Key, entry => entry.Value);
            } catch {
                Dispose();
                throw;
            }
        }

        public void Reset() {
            Restore(fixtureResources, fixtureDictionaries, fixtureSharedDictionaries);
            profile.ColorSchemaSettings.ColorSchema = initialColorSchema;
        }

        public void Dispose() {
            Restore(originalResources, originalDictionaries, originalSharedDictionaries);
            profile.Dispose();
        }

        private void Restore(
            IReadOnlyDictionary<object, object> resources,
            IEnumerable<ResourceDictionary> dictionaries,
            IReadOnlyDictionary<Uri, WeakReference> sharedDictionaries) {
            application.Dispatcher.VerifyAccess();
            application.MainWindow = originalMainWindow;
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
            foreach (var entry in resources) {
                application.Resources[entry.Key] = entry.Value;
            }

            foreach (var dictionary in dictionaries) {
                application.Resources.MergedDictionaries.Add(dictionary);
            }

            SharedResourceDictionary.SharedDictionaries.Clear();
            foreach (var entry in sharedDictionaries) {
                SharedResourceDictionary.SharedDictionaries.Add(entry.Key, entry.Value);
            }
        }
    }
}
