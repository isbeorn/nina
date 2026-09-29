#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NINA.Core.Enum;
using NINA.Core.Utility.Extensions;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger.Utility;
using NINA.WPF.Base.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using MiniSequenceView = NINA.View.Sequencer.MiniSequencer.MiniSequencer;

namespace NINA.Test.Sequencer.View.MiniSequencer {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class MiniSequencerViewTest {
        private Window window = null!;
        private SequenceRootContainer root = null!;
        private SequentialContainer group = null!;
        private MiniSequenceView view = null!;

        [SetUp]
        public void Setup() {
            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            if (!app.Resources.Contains("ProfileService")) {
                foreach (string resource in new[] { "StaticResources/ProfileService", "StaticResources/SVGDictionary", "StaticResources/Brushes", "StaticResources/Converters", "Styles/Expander" }) {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary {
                        Source = new Uri($"/NINA.WPF.Base;component/Resources/{resource}.xaml", UriKind.Relative)
                    });
                }
            }
            root = new SequenceRootContainer();
            group = new SequentialContainer { Name = "Instructions" };
            root.Add(group);
            for (int index = 0; index < 60; index++) {
                group.Add(new WaitForTimeSpan { Name = $"Instruction {index}" });
            }
            view = new MiniSequenceView { DataContext = root };
            window = new Window {
                Width = 450,
                Height = 280,
                Left = -32000,
                Top = -32000,
                ShowActivated = false,
                ShowInTaskbar = false,
                Content = view
            };
            app.MainWindow = window;
            NameScope.SetNameScope(window, new NameScope());
            window.RegisterName("RootGrid", new Grid());
        }

        [TearDown]
        public void TearDown() {
            window.Close();
            Drain();
        }

        [Test]
        public void OffscreenInstructionStarts_CentersItWithoutRealizingTheEntireSequence() {
            for (int index = 60; index < 1000; index++) {
                group.Add(new WaitForTimeSpan { Name = $"Instruction {index}" });
            }
            window.Show();
            Drain();
            Row(group.Items[945]).Should().BeNull("off-screen rows should be virtualized");

            group.Status = SequenceEntityStatus.RUNNING;
            group.Items[945].Status = SequenceEntityStatus.RUNNING;
            Drain();

            AssertCentered(group.Items[945]);
            Row(group.Items[500]).Should().BeNull("following must not realize unrelated rows between the old and new viewport");
        }

        [Test]
        public void AvalonDockReloadDuringExecution_FollowsCurrentAndNextInstructions() {
            var dock = new DockingManager();
            var factory = new FrameworkElementFactory(typeof(MiniSequenceView));
            factory.SetBinding(FrameworkElement.DataContextProperty, new Binding { Source = root });
            dock.LayoutItemTemplate = new DataTemplate { VisualTree = factory };
            var pane = new LayoutAnchorablePane();
            pane.Children.Add(new LayoutAnchorable { Title = "Sequencer", ContentId = "TestSequencer", Content = root });
            dock.Layout.RootPanel.Children.Add(pane);
            window.Content = dock;
            window.Show();
            Drain();
            view = FindVisual<MiniSequenceView>(dock)!;
            group.Status = SequenceEntityStatus.RUNNING;
            for (int index = 0; index <= 45; index++) {
                if (index > 0) {
                    group.Items[index - 1].Status = SequenceEntityStatus.FINISHED;
                }
                group.Items[index].Status = SequenceEntityStatus.RUNNING;
                Drain();
            }
            AssertVisible(group.Items[45]);
            MiniSequenceView previousView = view;
            var serializer = new XmlLayoutSerializer(dock);
            using var writer = new StringWriter();
            serializer.Serialize(writer);

            serializer.Deserialize(new StringReader(writer.ToString()));
            Drain();
            view = FindVisual<MiniSequenceView>(dock)!;

            view.Should().NotBeSameAs(previousView);
            AssertCentered(group.Items[45]);
            group.Items[45].Status = SequenceEntityStatus.FINISHED;
            group.Items[46].Status = SequenceEntityStatus.RUNNING;
            Drain();
            AssertCentered(group.Items[46]);
        }

        [Test]
        public void AlreadyRunningWhenViewLoads_CentersTheInstruction() {
            group.Status = SequenceEntityStatus.RUNNING;
            group.Items[40].Status = SequenceEntityStatus.RUNNING;

            window.Show();
            Drain();

            AssertCentered(group.Items[40]);
        }

        [Test]
        public void CollapseAndExpand_UsesTheSavedContainerState() {
            window.Show();
            Drain();
            TreeViewItem row = Row(group)!;
            ToggleButton? toggle = FindVisual<ToggleButton>(Header(row));
            toggle.Should().NotBeNull("hierarchical containers need a fold toggle");

            Click(toggle!);

            group.IsExpanded.Should().BeFalse();
            ((FrameworkElement)row.Template.FindName("ItemsHost", row)).IsVisible.Should().BeFalse();
            Click(toggle!);
            group.IsExpanded.Should().BeTrue();
            ((FrameworkElement)row.Template.FindName("ItemsHost", row)).IsVisible.Should().BeTrue();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyGroup_PreservesFoldingWhenChildrenAreAddedAndRemoved(bool expanded) {
            group.Items.Clear();
            group.IsExpanded = expanded;
            window.Show();
            Drain();
            TreeViewItem row = Row(group)!;
            ToggleButton toggle = FindVisual<ToggleButton>(Header(row))!;
            Click(toggle);
            group.IsExpanded.Should().Be(!expanded);

            var child = new WaitForTimeSpan();
            group.Add(child);
            Drain();
            var items = (FrameworkElement)row.Template.FindName("ItemsHost", row);
            items.IsVisible.Should().Be(!expanded);
            Click(toggle);
            items.IsVisible.Should().Be(expanded);

            group.Remove(child);
            Drain();
            group.IsExpanded.Should().Be(expanded);
            group.Add(child);
            Drain();
            items.IsVisible.Should().Be(expanded);
            group.IsExpanded.Should().Be(expanded);
        }

        [Test]
        public void ForwardAndLoopBack_ClampAtBothEndsAndCenterInTheMiddle() {
            window.Show();
            Drain();
            group.Status = SequenceEntityStatus.RUNNING;
            ISequenceItem? previous = null;
            foreach (int index in new[] { 40, 59, 0, 40, 0, 59 }) {
                if (previous != null) {
                    previous.ResetProgress();
                }
                ISequenceItem current = group.Items[index];
                current.Status = SequenceEntityStatus.RUNNING;
                Drain();
                AssertVisible(current);
                if (index == 40) {
                    AssertCentered(current);
                } else {
                    Scroll.VerticalOffset.Should().BeApproximately(index == 0 ? 0 : Scroll.ScrollableHeight, 1);
                }
                previous = current;
            }
        }

        [Test]
        public void OffscreenRootContainer_RealizesItsNestedActivePath() {
            for (int index = 0; index < 40; index++) {
                root.Add(new SequentialContainer { Name = $"Other group {index}" });
            }
            var nested = new SequentialContainer { Name = "Nested group" };
            var target = (SequentialContainer)group.Clone();
            root.Add(nested);
            nested.Add(target);
            window.Show();
            Drain();
            Row(nested).Should().BeNull();

            nested.Status = target.Status = SequenceEntityStatus.RUNNING;
            target.Items[40].Status = SequenceEntityStatus.RUNNING;
            Drain();

            AssertCentered(target.Items[40]);
        }

        [Test]
        public void ParallelInstructions_RetainTheCurrentTargetUntilItFinishes() {
            var parallel = new ParallelContainer();
            root.Items.Clear();
            root.Add(parallel);
            foreach (ISequenceItem item in group.GetItemsSnapshot()) {
                parallel.Add(item);
            }
            parallel.Status = SequenceEntityStatus.RUNNING;
            parallel.Items[20].Status = SequenceEntityStatus.RUNNING;
            window.Show();
            Drain();
            AssertCentered(parallel.Items[20]);
            double originalOffset = Scroll.VerticalOffset;

            parallel.Items[45].Status = SequenceEntityStatus.RUNNING;
            parallel.Items[5].Status = SequenceEntityStatus.RUNNING;
            Drain();

            Scroll.VerticalOffset.Should().Be(originalOffset);
            parallel.Items[20].Status = SequenceEntityStatus.FINISHED;
            Drain();
            AssertCentered(parallel.Items[5]);
            parallel.Items[5].Status = SequenceEntityStatus.FINISHED;
            Drain();
            AssertCentered(parallel.Items[45]);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HiddenOrUnloadedView_CatchesUpWhenShownAgain(bool unload) {
            group.Status = SequenceEntityStatus.RUNNING;
            group.Items[20].Status = SequenceEntityStatus.RUNNING;
            window.Show();
            Drain();
            AssertCentered(group.Items[20]);

            if (unload) {
                window.Content = null;
            } else {
                view.Visibility = Visibility.Hidden;
            }
            Drain();
            group.Items[20].Status = SequenceEntityStatus.FINISHED;
            group.Items[45].Status = SequenceEntityStatus.RUNNING;
            Drain();
            window.Content = view;
            view.Visibility = Visibility.Visible;
            Drain();

            AssertCentered(group.Items[45]);
        }

        [Test]
        public void ReplacingSequence_CancelsPendingFollowAndIgnoresOldNotifications() {
            window.Show();
            Drain();
            group.Items[45].Status = SequenceEntityStatus.RUNNING;
            var replacementRoot = new SequenceRootContainer();
            var replacement = (SequentialContainer)group.Clone();
            replacementRoot.Add(replacement);
            replacement.Status = replacement.Items[30].Status = SequenceEntityStatus.RUNNING;

            view.DataContext = replacementRoot;
            Drain();

            AssertCentered(replacement.Items[30]);
            double offset = Scroll.VerticalOffset;
            group.Items[5].Status = SequenceEntityStatus.RUNNING;
            group.Add(new WaitForTimeSpan());
            Drain();
            Scroll.VerticalOffset.Should().Be(offset);
            Row(group.Items[5]).Should().BeNull();
        }

        [Test]
        public void AddingAndRemovingInstructions_RefreshesFollowSubscriptions() {
            window.Show();
            Drain();
            group.Status = SequenceEntityStatus.RUNNING;
            var added = new WaitForTimeSpan { Name = "Added instruction" };
            group.Add(added);
            Drain();

            added.Status = SequenceEntityStatus.RUNNING;
            Drain();
            AssertVisible(added);
            group.Remove(added);
            group.Items[25].Status = SequenceEntityStatus.RUNNING;
            Drain();
            AssertCentered(group.Items[25]);
            double offset = Scroll.VerticalOffset;

            added.Status = SequenceEntityStatus.RUNNING;
            Drain();
            Scroll.VerticalOffset.Should().Be(offset);
        }

        [Test]
        public void ExecutionThreadNotifications_ReachTheDispatcher() {
            window.Show();
            Drain();

            Task.Run(() => {
                group.Status = SequenceEntityStatus.RUNNING;
                group.Items[40].Status = SequenceEntityStatus.RUNNING;
            }).GetAwaiter().GetResult();
            Drain();

            AssertCentered(group.Items[40]);
        }

        [Test]
        public void FoldingAnActiveNestedGroup_FollowsItsHeaderAndPreservesTheFold() {
            var nested = new SequentialContainer { Name = "Nested" };
            nested.Add(new WaitForTimeSpan());
            group.InsertIntoSequenceBlocks(35, nested);
            group.Status = nested.Status = nested.Items[0].Status = SequenceEntityStatus.RUNNING;
            window.Show();
            Drain();
            AssertCentered(nested.Items[0]);
            Click(FindVisual<ToggleButton>(Header(Row(nested)!))!);

            nested.IsExpanded.Should().BeFalse();
            AssertCentered(nested);
            nested.Items[0].ResetProgress();
            nested.Items[0].Status = SequenceEntityStatus.RUNNING;
            Drain();
            nested.IsExpanded.Should().BeFalse();
            AssertCentered(nested);

            group.IsExpanded = false;
            Drain();
            AssertVisible(group);
            nested.IsExpanded.Should().BeFalse();
            group.IsExpanded = true;
            Drain();
            AssertCentered(nested);
            Click(FindVisual<ToggleButton>(Header(Row(nested)!))!);
            AssertCentered(nested.Items[0]);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisableAndEnable_PreservesFoldPreference(bool expanded) {
            group.IsExpanded = expanded;
            window.Show();
            Drain();
            TreeViewItem row = Row(group)!;
            var items = (FrameworkElement)row.Template.FindName("ItemsHost", row);

            group.Status = SequenceEntityStatus.DISABLED;
            Drain();
            items.IsVisible.Should().BeFalse();
            group.IsExpanded.Should().Be(expanded);
            group.Status = SequenceEntityStatus.CREATED;
            Drain();
            items.IsVisible.Should().Be(expanded);
            group.IsExpanded.Should().Be(expanded);
        }

        [Test]
        public void FoldedGroup_HidesConditionsAndTriggersAlongWithInstructions() {
            var condition = new LoopCondition();
            var trigger = new CustomTrigger(Mock.Of<NINA.Core.Utility.IApplicationResourceDictionary>());
            group.Add(condition);
            group.Add(trigger);
            window.Show();
            Drain();
            FrameworkElement header = Header(Row(group)!);
            NINA.View.Sequencer.MiniSequencer.MiniCondition conditionView = FindVisual<NINA.View.Sequencer.MiniSequencer.MiniCondition>(header)!;
            NINA.View.Sequencer.MiniSequencer.MiniTrigger triggerView = FindVisual<NINA.View.Sequencer.MiniSequencer.MiniTrigger>(header)!;
            conditionView.IsVisible.Should().BeTrue();
            triggerView.IsVisible.Should().BeTrue();

            Click(FindVisual<ToggleButton>(header)!);
            conditionView.IsVisible.Should().BeFalse();
            triggerView.IsVisible.Should().BeFalse();
            Click(FindVisual<ToggleButton>(header)!);
            conditionView.IsVisible.Should().BeTrue();
            triggerView.IsVisible.Should().BeTrue();
        }

        [Test]
        public void FoldState_IsSharedWithTheEditorAndRoundTripsThroughSequenceJson() {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("/NINA.Sequencer;component/Resources/Styles/SequenceContainerStyles.xaml", UriKind.Relative)
            });
            var editor = new NINA.View.Sequencer.HierarchicalSequenceContainerView { DataContext = group };
            var columns = new Grid();
            columns.ColumnDefinitions.Add(new ColumnDefinition());
            columns.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(editor, 1);
            window.Content = null;
            columns.Children.Add(view);
            columns.Children.Add(editor);
            window.Content = columns;
            window.Show();
            Drain();
            var expander = FindVisual<NINA.CustomControlLibrary.DetachingExpander>(editor)!;

            Click(FindVisual<ToggleButton>(Header(Row(group)!))!);

            expander.IsExpanded.Should().BeFalse();
            group.IsExpanded.Should().BeFalse();
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(Array.Empty<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => new SequenceRootContainer());
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => new SequentialContainer());
            factory.Setup(x => x.GetItem<WaitForTimeSpan>()).Returns(() => new WaitForTimeSpan());
            var converter = new SequenceJsonConverter(factory.Object);
            var loaded = converter.Deserialize(converter.Serialize(group));
            loaded.IsExpanded.Should().BeFalse();
            var loadedRoot = new SequenceRootContainer();
            loadedRoot.Add(loaded);
            view.DataContext = loadedRoot;
            Drain();
            Row(loaded)!.IsExpanded.Should().BeFalse();
            view.DataContext = root;
            group.ResetAll();
            group.IsExpanded.Should().BeFalse();
            expander.SetCurrentValue(Expander.IsExpandedProperty, true);
            Drain();
            Row(group)!.IsExpanded.Should().BeTrue();
            group.IsExpanded.Should().BeTrue();
            converter.Deserialize(converter.Serialize(group)).IsExpanded.Should().BeTrue();
        }

        [Test]
        public void LinkedTemplateToggle_MaterializesChildrenAndObservesTheReplacementCollection() {
            using var profile = new NINA.Profile.Profile();
            profile.SequenceSettings.CollapseSequencerTemplatesByDefault = false;
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            var reference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Mini.template.json", DisplayName = "Mini" };
            var resolver = new TemplateLinkResolver();
            resolver.UpdateTemplates(new[] { new TemplatedSequenceContainer(profiles.Object, "Test", group, reference, resolver) }, true, null);
            var linked = new LinkedTemplateContainer(resolver) { TemplateReference = reference };
            root.Items.Clear();
            root.Add(linked);
            window.Show();
            Drain();
            linked.Items.Should().BeEmpty();

            Click(FindVisual<ToggleButton>(Header(Row(linked)!))!);
            ISequenceContainer materialized = (ISequenceContainer)linked.Items.Single();
            materialized.Status = linked.Status = materialized.Items[40].Status = SequenceEntityStatus.RUNNING;
            Drain();

            AssertCentered(materialized.Items[40]);
            var json = new SequenceJsonConverter(Mock.Of<ISequencerFactory>()).Serialize(linked);
            JObject.Parse(json).Property(nameof(ISequenceContainer.IsExpanded)).Should().BeNull();
        }

        [Test]
        public void ManualScrolling_IsNotUndoneByUnrelatedStatusChanges() {
            group.Status = group.Items[45].Status = SequenceEntityStatus.RUNNING;
            window.Show();
            Drain();
            Scroll.ScrollToTop();
            Drain();

            group.Items[5].Status = SequenceEntityStatus.FINISHED;
            Drain();

            Scroll.VerticalOffset.Should().Be(0);
        }

        [Test]
        public void ResizingThePanel_RecentersTheRunningHeader() {
            group.Status = group.Items[40].Status = SequenceEntityStatus.RUNNING;
            window.Show();
            Drain();

            window.Height = 500;
            Drain();

            AssertCentered(group.Items[40]);
        }

        [Test]
        public void FollowingAnotherInstruction_PreservesHorizontalScroll() {
            foreach (ISequenceItem item in group.Items) {
                item.Name = new string('X', 150);
            }
            group.Status = group.Items[20].Status = SequenceEntityStatus.RUNNING;
            window.Show();
            Drain();
            Scroll.ScrollToHorizontalOffset(50);
            Drain();
            Scroll.HorizontalOffset.Should().Be(50);

            group.Items[20].Status = SequenceEntityStatus.FINISHED;
            group.Items[45].Status = SequenceEntityStatus.RUNNING;
            Drain();

            AssertCentered(group.Items[45]);
            Scroll.HorizontalOffset.Should().Be(50);
        }

        [Test]
        public void CompoundInstruction_RemainsOneRowWithoutAFoldToggle() {
            using var profile = new NINA.Profile.Profile();
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            var camera = new Mock<ICameraMediator>();
            camera.Setup(x => x.GetInfo()).Returns(new NINA.Equipment.Equipment.MyCamera.CameraInfo());
            var compound = new TakeManyExposures(profiles.Object, camera.Object,
                Mock.Of<IImagingMediator>(), Mock.Of<IImageSaveMediator>(), Mock.Of<IImageHistoryVM>());
            group.InsertIntoSequenceBlocks(35, compound);
            group.Status = compound.Status = compound.Items[0].Status = SequenceEntityStatus.RUNNING;
            window.Show();
            Drain();

            AssertCentered(compound);
            FindVisual<ToggleButton>(Header(Row(compound)!)).Should().BeNull();
            Row(compound.Items[0]).Should().BeNull();
            compound.IsExpanded.Should().BeFalse();
        }

        [Test]
        public void PluginMiniTemplate_RemainsInControlOfItsPresentation() {
            string key = typeof(SequentialContainer).FullName + DataTemplatePostfix.MiniSequence;
            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(TextBlock.TextProperty, "Plugin header");
            Application.Current.Resources[key] = new DataTemplate { VisualTree = text };
            try {
                group.Status = group.Items[45].Status = SequenceEntityStatus.RUNNING;
                window.Show();
                Drain();

                AssertVisible(group);
                FindVisual<TextBlock>(Header(Row(group)!))!.Text.Should().Be("Plugin header");
                FindVisual<ToggleButton>(Header(Row(group)!)).Should().BeNull();
                Row(group.Items[45]).Should().BeNull();
            } finally {
                Application.Current.Resources.Remove(key);
            }
        }

        [Test]
        public void UnloadedView_IsNotRetainedByTheLiveSequence() {
            WeakReference detached = CreateAndUnloadView();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            detached.IsAlive.Should().BeFalse();
            GC.KeepAlive(root);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WeakReference CreateAndUnloadView() {
            var detached = new MiniSequenceView { DataContext = root };
            window.Content = detached;
            window.Show();
            Drain();
            window.Content = null;
            Drain();
            return new WeakReference(detached);
        }

        private TreeView Tree => FindVisual<TreeView>(view)!;
        private ScrollViewer Scroll => FindVisual<ScrollViewer>(Tree)!;

        private TreeViewItem? Row(ISequenceItem item) {
            return Tree.ItemContainerGenerator.ContainerFromItemRecursive(item);
        }

        private static FrameworkElement Header(TreeViewItem row) {
            return (FrameworkElement)row.Template.FindName("PART_Header", row);
        }

        private void AssertVisible(ISequenceItem item) {
            TreeViewItem? row = Row(item);
            row.Should().NotBeNull($"{item.Name} should have a realized row");
            FrameworkElement header = Header(row!);
            double y = header.TransformToAncestor(Scroll).Transform(new Point()).Y;
            y.Should().BeGreaterThanOrEqualTo(-1);
            (y + Math.Min(20, header.ActualHeight)).Should().BeLessThanOrEqualTo(Scroll.ViewportHeight + 2);
        }

        private void AssertCentered(ISequenceItem item) {
            AssertVisible(item);
            FrameworkElement header = Header(Row(item)!);
            double y = header.TransformToAncestor(Scroll).Transform(new Point()).Y;
            (y + Math.Min(20, header.ActualHeight) / 2).Should().BeApproximately(Scroll.ViewportHeight / 2, 2);
        }

        private static void Click(ToggleButton toggle) {
            typeof(ToggleButton).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(toggle, null);
            Drain();
        }

        private static T? FindVisual<T>(DependencyObject node) where T : DependencyObject {
            if (node is T match) {
                return match;
            }
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) {
                T? child = FindVisual<T>(VisualTreeHelper.GetChild(node, index));
                if (child != null) {
                    return child;
                }
            }
            return null;
        }

        private static void Drain() {
            for (int index = 0; index < 4; index++) {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }
        }
    }
}