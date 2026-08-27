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
using NINA.View.Equipment.Guider;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class DirectGuiderDetailViewTest {

        [Test]
        public void Constructor_LoadsMinimumDitherBindingAndValidation() {
            EnsureApplicationResources();

            DirectGuiderDetailView view = new DirectGuiderDetailView();
            Window host = new Window { Width = 800, Height = 450, Content = view };
            host.Measure(new Size(host.Width, host.Height));
            host.Arrange(new Rect(0, 0, host.Width, host.Height));
            host.UpdateLayout();

            TextBox minimumTextBox = FindLogicalDescendants<TextBox>(view)
                .Single(textBox => BindingOperations.GetBinding(textBox, TextBox.TextProperty)?.Path?.Path
                    == "ActiveProfile.GuiderSettings.MountDitherMinimumPixels");
            Binding minimumBinding = BindingOperations.GetBinding(minimumTextBox, TextBox.TextProperty);

            minimumBinding.ValidationRules.Should().ContainSingle(rule => rule is NINA.Core.Utility.ValidationRules.DoubleRangeRule);
            host.Close();
        }

        private static void EnsureApplicationResources() {
            const string resourcesLoadedMarker = "DirectGuiderDetailViewTest.ResourcesLoaded";
            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            if (app.Resources.Contains(resourcesLoadedMarker)) {
                return;
            }

            string[] resourceSources = [
                "/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TextBlock.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/TextBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/CheckBox.xaml",
                "/NINA.WPF.Base;component/Resources/Styles/Tooltip.xaml"
            ];

            foreach (string resourceSource in resourceSources) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary {
                    Source = new Uri(resourceSource, UriKind.Relative)
                });
            }
            app.Resources[resourcesLoadedMarker] = true;
        }

        private static T[] FindLogicalDescendants<T>(DependencyObject root) where T : DependencyObject {
            return LogicalTreeHelper.GetChildren(root)
                .OfType<DependencyObject>()
                .SelectMany(child => (child is T match ? new[] { match } : Array.Empty<T>()).Concat(FindLogicalDescendants<T>(child)))
                .ToArray();
        }
    }
}
