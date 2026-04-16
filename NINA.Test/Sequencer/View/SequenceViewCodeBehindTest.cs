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
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.View.Sequencer;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.Test.Sequencer.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class SequenceViewCodeBehindTest {
        private static bool resourcesLoaded;

        /// <summary>
        /// Verifies the lightweight view properties and hit-test overrides that keep sequencer containers usable as drag targets.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void ContainerViews_ExposeDependencyPropertiesAndReturnSelfHitTests() {
            EnsureApplicationResources();
            HierarchicalSequenceContainerView hierarchical = new HierarchicalSequenceContainerView();
            SequenceContainerView legacy = new SequenceContainerView();
            object content = new TextBlock { Text = "details" };

            hierarchical.SequenceContainerContent = content;
            hierarchical.ShowDetails = false;
            legacy.SequenceContainerContent = content;
            legacy.ShowDetails = false;

            hierarchical.SequenceContainerContent.Should().BeSameAs(content);
            hierarchical.ShowDetails.Should().BeFalse();
            legacy.SequenceContainerContent.Should().BeSameAs(content);
            legacy.ShowDetails.Should().BeFalse();

            InvokeHitTest(hierarchical).VisualHit.Should().BeSameAs(hierarchical);
            InvokeHitTest(legacy).VisualHit.Should().BeSameAs(legacy);
        }

        /// <summary>
        /// Verifies the root and main sequencer views can be constructed and expose their custom hit-test behavior without requiring a shell window.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void RootAndMainViews_ConstructAndExposeHitTestBehavior() {
            EnsureApplicationResources();
            SequenceRootContainerView root = new SequenceRootContainerView();
            SequenceView view = (SequenceView)FormatterServices.GetUninitializedObject(typeof(SequenceView));

            root.Should().NotBeNull();
            InvokeHitTest(view).VisualHit.Should().BeSameAs(view);
        }

        /// <summary>
        /// Verifies the deprecated container style selector still routes mutable container types to their historical templates.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void DeprecatedContainerStyleSelector_SelectsTemplatesForMutableContainers() {
            DataTemplate containerTemplate = new DataTemplate();
            DataTemplate parallelTemplate = new DataTemplate();
            DataTemplate dsoTemplate = new DataTemplate();
            DeprecatedContainerStyleSelector sut = new DeprecatedContainerStyleSelector {
                Container = containerTemplate,
                ParallelContainer = parallelTemplate,
                DeepSkyObjectContainer = dsoTemplate
            };

            sut.SelectTemplate(new SequentialContainer(), new FrameworkElement()).Should().BeSameAs(containerTemplate);
            sut.SelectTemplate(new ParallelContainer(), new FrameworkElement()).Should().BeSameAs(parallelTemplate);
            sut.SelectTemplate(FormatterServices.GetUninitializedObject(typeof(DeepSkyObjectContainer)), new FrameworkElement()).Should().BeSameAs(dsoTemplate);
            sut.SelectTemplate(new StartAreaContainer(), new FrameworkElement()).Should().BeSameAs(containerTemplate);
            sut.SelectTemplate(new UnknownSequenceItem("missing"), new FrameworkElement()).Should().BeNull();
        }

        /// <summary>
        /// Verifies the legacy container view menu handlers add target, template, instruction, trigger, and condition prototypes through their container commands.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SequenceContainerView_MenuHandlersDropSupportedPrototypeTypes() {
            EnsureApplicationResources();
            SequenceContainerView view = new SequenceContainerView();
            SequentialContainer container = new SequentialContainer();
            view.DataContext = container;

            InvokeMenuDropHandlers(view, container);
        }

        /// <summary>
        /// Verifies the hierarchical container view uses the same drop-command routing for all sequencer palette entity types.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void HierarchicalSequenceContainerView_MenuHandlersDropSupportedPrototypeTypes() {
            EnsureApplicationResources();
            HierarchicalSequenceContainerView view = new HierarchicalSequenceContainerView();
            SequentialContainer container = new SequentialContainer();
            view.DataContext = container;

            InvokeMenuDropHandlers(view, container);
        }

        /// <summary>
        /// Verifies the root container view menu handlers route palette entities into the root container's item, trigger, and condition collections.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SequenceRootContainerView_MenuHandlersDropSupportedPrototypeTypes() {
            EnsureApplicationResources();
            SequenceRootContainerView view = new SequenceRootContainerView();
            SequenceRootContainer container = new SequenceRootContainer();
            TargetAreaContainer targetArea = new TargetAreaContainer();
            container.Add(new StartAreaContainer());
            container.Add(targetArea);
            container.Add(new EndAreaContainer());
            view.DataContext = container;

            IProfileService profileService = CreateProfileService();

            InvokePrivate(view, "MenuItemTarget_Click", new MenuItem { DataContext = CreateTargetSequenceContainer(profileService) }, new RoutedEventArgs());
            InvokePrivate(view, "MenuItemTemplate_Click", new MenuItem { DataContext = new TemplatedSequenceContainer(profileService, TemplateController.DefaultTemplatesGroup, new SequentialContainer { Name = "Template" }) }, new RoutedEventArgs());
            InvokePrivate(view, "MenuItemInstruction_Click", new MenuItem { DataContext = CreateSidebarEntity(new UnknownSequenceItem("Instruction")) }, new RoutedEventArgs());
            InvokePrivate(view, "MenuItemTrigger_Click", new MenuItem { DataContext = CreateSidebarEntity(new UnknownSequenceTrigger("Trigger")) }, new RoutedEventArgs());
            InvokePrivate(view, "MenuItemCondition_Click", new MenuItem { DataContext = CreateSidebarEntity(new UnknownSequenceCondition("Condition")) }, new RoutedEventArgs());

            targetArea.Items.Should().HaveCount(3);
            container.Triggers.Should().HaveCount(1);
            container.Conditions.Should().HaveCount(1);
        }

        /// <summary>
        /// Verifies the save-as-template and save-as-target buttons pass the selected container to their controller commands as center drop parameters.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void ContainerViews_SaveButtonsExecuteControllerCommandsWithSelectedContainers() {
            EnsureApplicationResources();
            HierarchicalSequenceContainerView hierarchical = new HierarchicalSequenceContainerView();
            SequenceContainerView legacy = new SequenceContainerView();
            SequentialContainer templateContainer = new SequentialContainer();
            Mock<IDeepSkyObjectContainer> targetContainerMock = new Mock<IDeepSkyObjectContainer>();

            CommandHost hierarchicalHost = AttachCommandHost(hierarchical);
            CommandHost legacyHost = AttachCommandHost(legacy);

            InvokePrivate(hierarchical, "TemplateContainerButton_Click", new Button { DataContext = templateContainer }, new RoutedEventArgs());
            InvokePrivate(hierarchical, "TargetContainerButton_Click", new Button { DataContext = targetContainerMock.Object }, new RoutedEventArgs());
            InvokePrivate(legacy, "TemplateContainerButton_Click", new Button { DataContext = templateContainer }, new RoutedEventArgs());
            InvokePrivate(legacy, "TargetContainerButton_Click", new Button { DataContext = targetContainerMock.Object }, new RoutedEventArgs());

            hierarchicalHost.AddTemplateCommand.ReceivedParameter.Should().BeOfType<DropIntoParameters>()
                .Which.Source.Should().BeSameAs(templateContainer);
            hierarchicalHost.AddTargetToControllerCommand.ReceivedParameter.Should().BeOfType<DropIntoParameters>()
                .Which.Source.Should().BeSameAs(targetContainerMock.Object);
            legacyHost.AddTemplateCommand.ReceivedParameter.Should().BeOfType<DropIntoParameters>()
                .Which.Position.Should().Be(DropTargetEnum.Center);
            legacyHost.AddTargetToControllerCommand.ReceivedParameter.Should().BeOfType<DropIntoParameters>()
                .Which.Position.Should().Be(DropTargetEnum.Center);
        }

        private static HitTestResult InvokeHitTest(UIElement element) {
            MethodInfo method = element.GetType().GetMethod(
                "HitTestCore",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(PointHitTestParameters) },
                null);
            return (HitTestResult)method.Invoke(element, new object[] { new PointHitTestParameters(new Point(10, 20)) });
        }

        private static void InvokeMenuDropHandlers(object view, SequenceContainer container) {
            IProfileService profileService = CreateProfileService();
            int initialItemCount = container.Items.Count;

            InvokePrivate(view, "MenuItemTarget_Click", new MenuItem { DataContext = CreateTargetSequenceContainer(profileService) }, new RoutedEventArgs());
            container.Items.Should().HaveCount(initialItemCount + 1);

            InvokePrivate(view, "MenuItemTemplate_Click", new MenuItem { DataContext = new TemplatedSequenceContainer(profileService, TemplateController.DefaultTemplatesGroup, new SequentialContainer { Name = "Template" }) }, new RoutedEventArgs());
            container.Items.Should().HaveCount(initialItemCount + 2);

            InvokePrivate(view, "MenuItemInstruction_Click", new MenuItem { DataContext = CreateSidebarEntity(new UnknownSequenceItem("Instruction")) }, new RoutedEventArgs());
            container.Items.Should().HaveCount(initialItemCount + 3);

            InvokePrivate(view, "MenuItemTrigger_Click", new MenuItem { DataContext = CreateSidebarEntity(new UnknownSequenceTrigger("Trigger")) }, new RoutedEventArgs());
            container.Triggers.Should().HaveCount(1);

            InvokePrivate(view, "MenuItemCondition_Click", new MenuItem { DataContext = CreateSidebarEntity(new UnknownSequenceCondition("Condition")) }, new RoutedEventArgs());
            container.Conditions.Should().HaveCount(1);
        }

        private static void InvokePrivate(object instance, string methodName, params object[] parameters) {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(instance, parameters);
        }

        private static SidebarEntity CreateSidebarEntity(ISequenceEntity entity) {
            return new SidebarEntity(entity, new PluginOptionsAccessor(CreateProfileService(), Guid.NewGuid()));
        }

        private static IProfileService CreateProfileService() {
            NINA.Profile.Profile profile = new NINA.Profile.Profile();
            Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            return profileServiceMock.Object;
        }

        private static TargetSequenceContainer CreateTargetSequenceContainer(IProfileService profileService) {
            Mock<IDeepSkyObjectContainer> sourceContainerMock = new Mock<IDeepSkyObjectContainer>();
            Mock<IDeepSkyObjectContainer> cloneContainerMock = new Mock<IDeepSkyObjectContainer>();
            cloneContainerMock.SetupGet(x => x.Parent).Returns((ISequenceContainer)null);
            sourceContainerMock.SetupGet(x => x.Name).Returns("Target");
            sourceContainerMock.Setup(x => x.Clone()).Returns(cloneContainerMock.Object);
            return new TargetSequenceContainer(profileService, sourceContainerMock.Object);
        }

        private static CommandHost AttachCommandHost(HierarchicalSequenceContainerView view) {
            CommandHost host = new CommandHost();
            GetContentPresenter(view).Resources["ViewModel"] = new BindingProxy { Data = host };
            return host;
        }

        private static CommandHost AttachCommandHost(SequenceContainerView view) {
            CommandHost host = new CommandHost();
            GetContentPresenter(view).Resources["ViewModel"] = new BindingProxy { Data = host };
            return host;
        }

        private static ContentPresenter GetContentPresenter(object view) {
            FieldInfo field = view.GetType().GetField("Content", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (ContentPresenter)field.GetValue(view);
        }

        private static void EnsureApplicationResources() {
            if (resourcesLoaded) {
                return;
            }

            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
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

        private class CommandHost {
            public CaptureCommand AddTemplateCommand { get; } = new CaptureCommand();
            public CaptureCommand AddTargetToControllerCommand { get; } = new CaptureCommand();
        }

        private class CaptureCommand : ICommand {
            public object ReceivedParameter { get; private set; }

            public bool CanExecute(object parameter) {
                return true;
            }

            public void Execute(object parameter) {
                ReceivedParameter = parameter;
            }

            public event EventHandler CanExecuteChanged;
        }
    }
}
