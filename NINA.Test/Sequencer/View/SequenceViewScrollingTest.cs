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
using NINA.CustomControlLibrary;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Test.Sequencer.Editing;
using NINA.View.Sequencer;
using NUnit.Framework;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;

namespace NINA.Test.Sequencer.View {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequenceViewScrollingTest {
        [Test]
        public void ContainerHeader_SurvivesScrollingAndStillExpandsAndCollapses() {
            using var scope = new CoreEditorTestScope();
            var area = AddAreas(scope);
            var containers = new List<SequentialContainer>();
            for (int index = 0; index < 12; index++) {
                var container = new SequentialContainer { Name = $"Container {index}", IsExpanded = true };
                area.Add(container);
                containers.Add(container);
                for (int instruction = 0; instruction < 12; instruction++) container.Add((TakeExposure)scope.Create(typeof(TakeExposure)));
            }
            var (view, tree, scroll) = ShowSequence(scope);
            var first = containers[0];
            var firstInstruction = first.Items[0];
            HierarchicalSequenceContainerView? Header() => Descendants<HierarchicalSequenceContainerView>(tree)
                .SingleOrDefault(header => ReferenceEquals(header.DataContext, first));
            var originalHeader = Header()!;
            originalHeader.Should().NotBeNull();
            double initialBottom = originalHeader.TransformToAncestor(tree).TransformBounds(new Rect(originalHeader.RenderSize)).Bottom;

            ScrollInSteps(view, scroll, down: true);
            scroll.VerticalOffset.Should().BeGreaterThan(initialBottom + VirtualizingPanel.GetCacheLength(tree).CacheBeforeViewport,
                "the first header must leave both the viewport and its preceding cache");
            ReferenceEquals(Header(), originalHeader).Should().BeTrue("scrolling must retain the existing container header");
            ScrollInSteps(view, scroll, down: false);
            ReferenceEquals(Header(), originalHeader).Should().BeTrue();

            var expander = Descendants<DetachingExpander>(originalHeader).Single();
            var toggle = (ToggleButton)expander.Template.FindName("HeaderSite", expander);
            bool InstructionIsVisible() => Descendants<SequenceBlockView>(tree)
                .Any(block => ReferenceEquals(block.DataContext, firstInstruction) && block.IsVisible);
            InstructionIsVisible().Should().BeTrue();
            toggle.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
            Refresh(view);
            first.IsExpanded.Should().BeFalse();
            InstructionIsVisible().Should().BeFalse();
            toggle.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
            Refresh(view);
            first.IsExpanded.Should().BeTrue();
            InstructionIsVisible().Should().BeTrue();

            area.Remove(first).Should().BeTrue();
            Refresh(view);
            Header().Should().BeNull();
            PresentationSource.FromVisual(originalHeader).Should().BeNull();
            area.Add(first);
            JumpToEnd(view, scroll, bottom: true);
            Header().Should().NotBeNull();
        }

        [Test]
        public void LargeInstructionList_EvictsOffscreenRowsInBothDirections() {
            using var scope = new CoreEditorTestScope();
            var container = new SequentialContainer { IsExpanded = true };
            AddAreas(scope).Add(container);
            const int count = 500;
            for (int index = 0; index < count; index++) container.Add((TakeExposure)scope.Create(typeof(TakeExposure)));
            var (view, tree, scroll) = ShowSequence(scope);
            TreeViewItem? Row(object item) => Descendants<TreeViewItem>(tree).SingleOrDefault(row => ReferenceEquals(row.DataContext, item));
            int RealizedInstructions() => Descendants<TreeViewItem>(tree).Count(row => row.DataContext is TakeExposure);
            Row(container.Items[0]).Should().NotBeNull();
            Row(container.Items[count - 1]).Should().BeNull();
            RealizedInstructions().Should().BeLessThan(count / 2);

            JumpToEnd(view, scroll, bottom: true);
            Row(container.Items[count - 1]).Should().NotBeNull();
            Row(container.Items[0]).Should().BeNull("offscreen instructions must still be virtualized inside a retained container");
            RealizedInstructions().Should().BeLessThan(count / 2);

            JumpToEnd(view, scroll, bottom: false);
            Row(container.Items[0]).Should().NotBeNull();
            Row(container.Items[count - 1]).Should().BeNull();
            RealizedInstructions().Should().BeLessThan(count / 2);
        }

        private static TargetAreaContainer AddAreas(CoreEditorTestScope scope) {
            var area = new TargetAreaContainer();
            scope.Root.Add(new StartAreaContainer());
            scope.Root.Add(area);
            scope.Root.Add(new EndAreaContainer());
            return area;
        }

        private static (SequenceView View, TreeView Tree, ScrollViewer Scroll) ShowSequence(CoreEditorTestScope scope) {
            var view = new SequenceView { DataContext = new { Sequencer = new { Items = new[] { scope.Root } }, IsLocked = false, CanDragAndDrop = true } };
            var tree = ((Grid)view.Content).Children.OfType<TreeView>().Single();
            scope.Host.Content = view;
            Refresh(view);
            return (view, tree, Descendants<ScrollViewer>(tree).First());
        }

        private static void ScrollInSteps(FrameworkElement view, ScrollViewer scroll, bool down) {
            var visited = new Dictionary<(int Offset, int Extent), int>();
            for (int step = 0; step < 1000; step++) {
                scroll.ScrollToVerticalOffset(down ? scroll.VerticalOffset + 48 : Math.Max(0, scroll.VerticalOffset - 48));
                Refresh(view);
                if (down ? scroll.VerticalOffset >= scroll.ScrollableHeight - 0.5 : scroll.VerticalOffset < 0.5) break;
                var position = ((int)Math.Round(scroll.VerticalOffset), (int)Math.Round(scroll.ExtentHeight));
                visited.TryGetValue(position, out int visits);
                visited[position] = visits + 1;
                if (visits == 2) break;
            }
            scroll.VerticalOffset.Should().BeApproximately(down ? scroll.ScrollableHeight : 0, 0.5,
                "continuous scrolling must reach either end without repeating intermediate offsets");
        }

        private static void JumpToEnd(FrameworkElement view, ScrollViewer scroll, bool bottom) {
            for (int attempt = 0; attempt < 10; attempt++) {
                if (bottom) scroll.ScrollToBottom(); else scroll.ScrollToTop();
                Refresh(view);
                if (Math.Abs(scroll.VerticalOffset - (bottom ? scroll.ScrollableHeight : 0)) < 0.5) break;
            }
            scroll.VerticalOffset.Should().BeApproximately(bottom ? scroll.ScrollableHeight : 0, 0.5);
        }

        private static void Refresh(FrameworkElement view) {
            view.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.SystemIdle);
            view.UpdateLayout();
        }
    }
}
