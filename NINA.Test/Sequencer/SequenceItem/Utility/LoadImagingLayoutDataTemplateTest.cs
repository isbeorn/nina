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
using NINA.View.Sequencer;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    [NonParallelizable]
    [Apartment(ApartmentState.STA)]
    public class LoadImagingLayoutDataTemplateTest {
        private static bool resourcesLoaded;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            EnsureApplicationResources();
        }

        [Test]
        public void DataTemplate_LoadsWithFilePathAndBrowseCommandBindings() {
            var resources = new NINA.Sequencer.SequenceItem.Utility.Datatemplates();
            var template = (DataTemplate)resources[new DataTemplateKey(typeof(NINA.Sequencer.SequenceItem.Utility.LoadImagingLayout))];
            var view = (SequenceBlockView)template.LoadContent();
            var content = (Grid)view.SequenceItemContent;
            var textBox = content.Children.OfType<TextBox>().Single();
            var browseButton = content.Children.OfType<Button>().Single();
            var item = new NINA.Sequencer.SequenceItem.Utility.LoadImagingLayout(Mock.Of<IApplicationMediator>());

            view.DataContext = item;
            content.DataContext = item;
            textBox.DataContext = item;
            browseButton.DataContext = item;
            item.FilePath = @"C:\Layouts\Imaging.dock.config";
            textBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            browseButton.GetBindingExpression(Button.CommandProperty).UpdateTarget();

            BindingOperations.GetBinding(textBox, TextBox.TextProperty).Path.Path.Should().Be(nameof(item.FilePath));
            BindingOperations.GetBinding(browseButton, Button.CommandProperty).Path.Path.Should().Be(nameof(item.OpenDialogCommand));
            item.OpenDialogCommand.Should().NotBeNull();
        }

        private static void EnsureApplicationResources() {
            if (resourcesLoaded) {
                return;
            }

            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            if (app.MainWindow == null) {
                var rootGrid = new Grid { Name = "RootGrid" };
                var mainWindow = new Window { Content = rootGrid };
                NameScope.SetNameScope(mainWindow, new NameScope());
                mainWindow.RegisterName(rootGrid.Name, rootGrid);
                app.MainWindow = mainWindow;
            }
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/StaticResources/SVGDictionary.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/StaticResources/Converters.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/Styles/Expander.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/Styles/Button.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/Styles/TabControl.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/Styles/ListView.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.Sequencer;component/Resources/Styles/ProgressStyle.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.Sequencer;component/Resources/Styles/SequenceContainerStyles.xaml", UriKind.Relative) });
            resourcesLoaded = true;
        }
    }
}
