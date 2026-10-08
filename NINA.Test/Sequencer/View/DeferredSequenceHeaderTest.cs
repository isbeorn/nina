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
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Test.Sequencer.Editing;
using NINA.View.Sequencer;
using NUnit.Framework;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;

namespace NINA.Test.Sequencer.View {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class DeferredSequenceHeaderTest {
        private static int constructions;

        [SetUp]
        public void ResetConstructionCount() => constructions = 0;

        [Test]
        public void HostedHeader_PlacesInputBeforeTemplateConstruction() {
            using var scope = new CoreEditorTestScope();
            var model = new SequentialContainer { Name = "Visible header" };
            var header = new DeferredSequenceHeader { Content = model, ContentTemplate = ProbeTemplate() };
            var scroll = Show(scope, header);

            header.IsLoaded.Should().BeTrue();
            constructions.Should().Be(0, "initial layout must measure a placeholder without constructing the real template");
            header.DesiredSize.Height.Should().BeGreaterThan(0);
            Descendants<ProbeHeader>(header).Should().BeEmpty();

            int constructionsWhenInputRan = -1;
            header.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => constructionsWhenInputRan = constructions));
            Settle(scope);

            constructionsWhenInputRan.Should().Be(0, "queued input must run before header realization");
            constructions.Should().Be(1);
            Descendants<ProbeHeader>(header).Single().Text.Should().Be(model.Name);
            scroll.Content.Should().BeSameAs(header);
        }

        [Test]
        public void OffscreenHeaders_SkipConstructionUntilScrollingExposesThemInEitherDirection() {
            using var scope = new CoreEditorTestScope();
            var template = ProbeTemplate();
            var first = new DeferredSequenceHeader { Content = new SequentialContainer { Name = "First" }, ContentTemplate = template };
            var middle = new DeferredSequenceHeader { Content = new SequentialContainer { Name = "Middle" }, ContentTemplate = template };
            var last = new DeferredSequenceHeader { Content = new SequentialContainer { Name = "Last" }, ContentTemplate = template };
            var panel = new StackPanel();
            panel.Children.Add(first);
            panel.Children.Add(new Border { Height = 700 });
            panel.Children.Add(middle);
            panel.Children.Add(new Border { Height = 700 });
            panel.Children.Add(last);
            var scroll = Show(scope, panel);
            Settle(scope);

            var originalFirst = Descendants<ProbeHeader>(first).Single();
            Descendants<ProbeHeader>(middle).Should().BeEmpty();
            Descendants<ProbeHeader>(last).Should().BeEmpty();
            constructions.Should().Be(1);

            scroll.ScrollToBottom();
            Settle(scope);
            Descendants<ProbeHeader>(last).Single().Text.Should().Be("Last");
            Descendants<ProbeHeader>(middle).Should().BeEmpty("the far jump must not construct an intermediate offscreen header");
            constructions.Should().Be(2);

            double middleTop = middle.TransformToAncestor(panel).Transform(new Point()).Y;
            scroll.ScrollToVerticalOffset(middleTop);
            Settle(scope);
            Descendants<ProbeHeader>(middle).Single().Text.Should().Be("Middle");
            constructions.Should().Be(3);

            scroll.ScrollToTop();
            Settle(scope);
            Descendants<ProbeHeader>(first).Single().Should().BeSameAs(originalFirst);
            constructions.Should().Be(3, "return scrolling must retain already constructed headers");
        }

        [Test]
        public void LayoutRelocation_RealizesAnOffscreenHeaderWithoutChangingScrollExtentOrOffset() {
            using var scope = new CoreEditorTestScope();
            var template = ProbeTemplate();
            template.VisualTree.SetValue(FrameworkElement.HeightProperty, 30.0);
            var header = new DeferredSequenceHeader {
                Content = new SequentialContainer { Name = "Relocated header" },
                ContentTemplate = template
            };
            var before = new Border { Height = 700 };
            var after = new Border { Height = 700 };
            var panel = new StackPanel();
            panel.Children.Add(before);
            panel.Children.Add(header);
            panel.Children.Add(after);
            var scroll = Show(scope, panel);
            Settle(scope);
            constructions.Should().Be(0);
            Descendants<ProbeHeader>(header).Should().BeEmpty();
            double extent = scroll.ExtentHeight;
            double offset = scroll.VerticalOffset;

            before.Height = 40;
            after.Height = 1360;
            LayoutBeforeBackground(scope);
            scroll.ExtentHeight.Should().BeApproximately(extent, 0.5, "balanced spacer changes relocate the header without changing the total content height");
            scroll.VerticalOffset.Should().BeApproximately(offset, 0.5);
            Descendants<ProbeHeader>(header).Should().BeEmpty("the relocated header must still defer construction until Background work runs");

            Settle(scope);
            Descendants<ProbeHeader>(header).Single().Text.Should().Be("Relocated header");
            constructions.Should().Be(1, "layout movement must expose a header even when no scroll geometry changes");
            scroll.ExtentHeight.Should().BeApproximately(extent, 0.5);
            scroll.VerticalOffset.Should().BeApproximately(offset, 0.5);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitTemplateAndSelector_PreserveNormalTemplateSelection(bool explicitTemplate) {
            using var scope = new CoreEditorTestScope();
            var model = new SequentialContainer { Name = "Selected model" };
            var selector = new RecordingTemplateSelector(ProbeTemplate("Selected"));
            var template = ProbeTemplate("Explicit");
            var header = new DeferredSequenceHeader { Content = model, ContentTemplateSelector = selector };
            if (explicitTemplate) header.ContentTemplate = template;
            Show(scope, header);

            constructions.Should().Be(0);
            selector.Calls.Should().Be(0, "the placeholder must not invoke a real template selector");
            Settle(scope);

            Descendants<ProbeHeader>(header).Single().Text.Should().Be(explicitTemplate ? "Explicit" : "Selected");
            constructions.Should().Be(1);
            if (explicitTemplate) {
                header.ContentTemplate.Should().BeSameAs(template);
                selector.Calls.Should().Be(0, "an explicit template takes precedence over a selector");
            } else {
                selector.Calls.Should().BeGreaterThan(0);
                selector.LastItem.Should().BeSameAs(model);
                selector.LastContainer.Should().BeSameAs(header);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReplacingContent_RealizesOnlyTheCurrentModel(bool initiallyRealized) {
            using var scope = new CoreEditorTestScope();
            var first = new SequentialContainer { Name = "Old model" };
            var current = new SequentialContainer { Name = "Current model" };
            var header = new DeferredSequenceHeader { Content = first, ContentTemplate = ProbeTemplate() };
            Show(scope, header);
            if (initiallyRealized) Settle(scope);
            int before = constructions;

            header.Content = current;
            LayoutBeforeBackground(scope);
            constructions.Should().Be(before, "replacement must defer construction of the current header");
            Descendants<ProbeHeader>(header).Should().BeEmpty("the old model's header must not remain attached");
            Settle(scope);

            constructions.Should().Be(before + 1);
            var child = Descendants<ProbeHeader>(header).Single();
            child.Text.Should().Be("Current model");
            child.DataContext.Should().BeSameAs(current);
        }

        [Test]
        public void CachedPlaceholderHeight_UsesTheMeasuredHeaderAndRemeasuresAfterResize() {
            using var scope = new CoreEditorTestScope();
            var model = new SequentialContainer { Name = string.Join(" ", Enumerable.Repeat("A wrapping header", 18)) };
            var template = ProbeTemplate();
            var first = new DeferredSequenceHeader { Content = model, ContentTemplate = template };
            var scroll = Show(scope, first);
            Settle(scope);
            double wideHeight = first.DesiredSize.Height;
            wideHeight.Should().BeGreaterThan(30, "the real multiline header must differ from the cold placeholder");

            var recreated = new DeferredSequenceHeader { Content = model, ContentTemplate = template };
            scroll.Content = recreated;
            LayoutBeforeBackground(scope);
            Descendants<ProbeHeader>(recreated).Should().BeEmpty();
            recreated.DesiredSize.Height.Should().BeApproximately(wideHeight, 0.5,
                "a recreated header at the same width should preserve the measured height before realization");

            scroll.Width = 140;
            Settle(scope);
            Descendants<ProbeHeader>(recreated).Single().Text.Should().Be(model.Name);
            recreated.DesiredSize.Height.Should().BeGreaterThan(wideHeight);

            scroll.Width = 400;
            Settle(scope);
            recreated.DesiredSize.Height.Should().BeApproximately(wideHeight, 0.5);
        }

        [Test]
        public void SharedTemplate_CreatesIndependentControlsAndBindings() {
            using var scope = new CoreEditorTestScope();
            var firstModel = new SequentialContainer { Name = "First model" };
            var secondModel = new SequentialContainer { Name = "Second model" };
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetBinding(TextBox.TextProperty, new Binding(nameof(SequentialContainer.Name)) { Mode = BindingMode.TwoWay });
            var template = new DataTemplate { VisualTree = factory };
            var first = new DeferredSequenceHeader { Content = firstModel, ContentTemplate = template };
            var second = new DeferredSequenceHeader { Content = secondModel, ContentTemplate = template };
            var panel = new StackPanel();
            panel.Children.Add(first);
            panel.Children.Add(second);
            Show(scope, panel);
            Settle(scope);

            var firstEditor = Descendants<TextBox>(first).Single();
            var secondEditor = Descendants<TextBox>(second).Single();
            firstEditor.Should().NotBeSameAs(secondEditor);
            firstEditor.DataContext.Should().BeSameAs(firstModel);
            secondEditor.DataContext.Should().BeSameAs(secondModel);
            firstEditor.SetCurrentValue(TextBox.TextProperty, "Edited first");
            firstEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            firstModel.Name.Should().Be("Edited first");
            secondModel.Name.Should().Be("Second model");
            secondEditor.Text.Should().Be("Second model");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnloadedHeader_DoesNotRetainTheModelOrTemplate(bool realized) {
            using var scope = new CoreEditorTestScope();
            var released = CreateAndRelease(scope, realized);
            Settle(scope);
            constructions.Should().Be(realized ? 1 : 0,
                "unloading an unrealized header must cancel construction rather than merely allow the result to be collected");
            for (int attempt = 0; attempt < 3; attempt++) {
                Settle(scope);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Settle(scope);
                GC.Collect();
            }

            released.Header.IsAlive.Should().BeFalse("the surviving scroll viewer must release the header's event subscription and pending work");
            released.Model.IsAlive.Should().BeFalse("the size cache must not own its model keys");
            released.Template.IsAlive.Should().BeFalse("the size cache must not own header template resources");
            GC.KeepAlive(released.Scroll);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CompiledSequenceView_RealizesDeferredHeadersWithTheOriginalContainerTemplate(bool expanded) {
            using var scope = new CoreEditorTestScope();
            var area = new TargetAreaContainer();
            scope.Root.Add(new StartAreaContainer());
            scope.Root.Add(area);
            scope.Root.Add(new EndAreaContainer());
            var model = new SequentialContainer { Name = "Compiled container", IsExpanded = expanded };
            model.Icon = new System.Windows.Media.GeometryGroup {
                Children = { new System.Windows.Media.RectangleGeometry(new Rect(0, 0, 10, 10)) }
            };
            area.Add(model);
            var view = new SequenceView { DataContext = new { Sequencer = new { Items = new[] { scope.Root } }, IsLocked = false, CanDragAndDrop = true } };
            scope.Host.Content = view;
            LayoutBeforeBackground(scope);
            var tree = ((Grid)view.Content).Children.OfType<TreeView>().Single();
            var row = Descendants<TreeViewItem>(tree).Single(item => ReferenceEquals(item.DataContext, model));
            var header = row.Template.FindName("PART_Header", row).Should().BeAssignableTo<ContentPresenter>().Subject;
            header.Content.Should().BeSameAs(model);
            Descendants<HierarchicalSequenceContainerView>(header).Should().BeEmpty();
            var preview = Descendants<TextBlock>(header).SingleOrDefault(text => text.Text == model.Name);
            preview.Should().NotBeNull("the target or instruction identity must be readable before expensive editors are built");
            preview!.IsVisible.Should().BeTrue();
            preview.RenderSize.Width.Should().BeGreaterThan(0);
            preview.RenderSize.Height.Should().BeGreaterThan(0);
            var previewToggle = Descendants<ToggleButton>(header).Should().ContainSingle().Subject;
            previewToggle.IsChecked.Should().Be(expanded);
            var headerStyle = previewToggle.Style;
            double previewHeight = header.DesiredSize.Height;
            model.Name = string.Join(" ", Enumerable.Repeat("Current container", 20));
            tree.Width = 280;
            LayoutBeforeBackground(scope);
            preview.Text.Should().Be(model.Name, "the preview must use the current model rather than a stale snapshot");
            header.DesiredSize.Height.Should().BeApproximately(previewHeight, 0.5, "a long navigation label must not move the scroll anchor");
            Descendants<System.Windows.Shapes.Path>(header).Should().ContainSingle(path => ReferenceEquals(path.Data, model.Icon));
            Descendants<HierarchicalSequenceContainerView>(header).Should().BeEmpty();

            Settle(scope);
            var containerView = Descendants<HierarchicalSequenceContainerView>(header).Single();
            containerView.DataContext.Should().BeSameAs(model);
            Descendants<NINA.CustomControlLibrary.DetachingExpander>(containerView).Single().IsExpanded.Should().Be(expanded);
            Descendants<ToggleButton>(containerView).Should().Contain(toggle => ReferenceEquals(toggle.Style, headerStyle),
                "the preview must use the same container header visuals as the full editor");
        }

        [TestCase(typeof(SequentialContainer), true, true)]
        [TestCase(typeof(SequentialContainer), false, false)]
        [TestCase(typeof(ParallelContainer), true, false)]
        [TestCase(typeof(ConditionalContainer), true, false)]
        [TestCase(typeof(LinkedTemplateContainer), true, false)]
        public void CompiledSequenceView_PreviewsLocalConditionsAndTriggersWithoutBuildingEditors(Type containerType, bool expanded, bool showDetails) {
            using var scope = new CoreEditorTestScope();
            var area = new TargetAreaContainer();
            scope.Root.Add(new StartAreaContainer());
            scope.Root.Add(area);
            scope.Root.Add(new EndAreaContainer());
            var model = (SequenceContainer)scope.Create(containerType);
            model.IsExpanded = expanded;
            var condition = (NINA.Sequencer.Conditions.LoopCondition)scope.Create(typeof(NINA.Sequencer.Conditions.LoopCondition));
            var trigger = (NINA.Sequencer.Trigger.Guider.DitherAfterExposures)scope.Create(typeof(NINA.Sequencer.Trigger.Guider.DitherAfterExposures));
            condition.Name = "Preview condition";
            trigger.Name = "Preview trigger";
            model.Add(condition);
            model.Add(trigger);
            area.Add(model);
            var view = new SequenceView { DataContext = new { Sequencer = new { Items = new[] { scope.Root } }, IsLocked = false, CanDragAndDrop = true } };
            scope.Host.Content = view;
            LayoutBeforeBackground(scope);
            var header = Descendants<DeferredSequenceHeader>(view).Single(item => ReferenceEquals(item.Content, model));
            bool HasLabel(string name) => Descendants<TextBlock>(header).Any(text => text.IsVisible && text.Text == name);
            HasLabel(condition.Name).Should().Be(showDetails);
            HasLabel(trigger.Name).Should().Be(showDetails);
            HasLabel(NINA.Core.Locale.Loc.Instance["LblTriggers"]).Should().Be(showDetails);
            HasLabel(NINA.Core.Locale.Loc.Instance["Lbl_SequenceContainer_Conditions_Header"]).Should().Be(showDetails);
            HasLabel(NINA.Core.Locale.Loc.Instance["LblInstructions"]).Should().Be(expanded && model is not LinkedTemplateContainer);
            foreach (var caption in new[] { "LblTriggers", "Lbl_SequenceContainer_Conditions_Header", "LblInstructions" }) {
                foreach (var label in Descendants<TextBlock>(header).Where(text => text.IsVisible && text.Text == NINA.Core.Locale.Loc.Instance[caption])) {
                    label.Foreground.Should().BeSameAs(view.FindResource("ButtonForegroundBrush"), "section captions must remain readable in the active theme");
                }
            }
            Descendants<SequenceBlockView>(header).Should().BeEmpty();
            Descendants<HierarchicalSequenceContainerView>(header).Should().BeEmpty();
            if (showDetails) {
                model.Conditions.Remove(condition);
                model.Triggers.Remove(trigger);
                LayoutBeforeBackground(scope);
                HasLabel(condition.Name).Should().BeFalse();
                HasLabel(trigger.Name).Should().BeFalse();
                condition.Name = "Updated condition";
                model.Add(condition);
                model.Add(trigger);
                LayoutBeforeBackground(scope);
                HasLabel(condition.Name).Should().BeTrue();
                HasLabel(trigger.Name).Should().BeTrue();
                model.IsExpanded = false;
                LayoutBeforeBackground(scope);
                HasLabel(condition.Name).Should().BeFalse();
                HasLabel(trigger.Name).Should().BeFalse();
                model.IsExpanded = true;
                LayoutBeforeBackground(scope);
                HasLabel(condition.Name).Should().BeTrue();
                HasLabel(trigger.Name).Should().BeTrue();
            }
            Descendants<SequenceBlockView>(header).Should().BeEmpty();
            Descendants<HierarchicalSequenceContainerView>(header).Should().BeEmpty();
        }

        [Test]
        public void CompiledSequenceView_InstructionPreviewsPreserveRowHeightWhenEditorsAppear() {
            using var scope = new CoreEditorTestScope();
            var area = new TargetAreaContainer();
            scope.Root.Add(new StartAreaContainer());
            scope.Root.Add(area);
            scope.Root.Add(new EndAreaContainer());
            var first = (TakeExposure)scope.Create(typeof(TakeExposure));
            var second = (TakeExposure)scope.Create(typeof(TakeExposure));
            area.Add(first);
            area.Add(second);
            var view = new SequenceView { DataContext = new { Sequencer = new { Items = new[] { scope.Root } }, IsLocked = false, CanDragAndDrop = true } };
            scope.Host.Content = view;
            LayoutBeforeBackground(scope);
            var tree = ((Grid)view.Content).Children.OfType<TreeView>().Single();
            var header = Descendants<DeferredSequenceHeader>(tree).Single(item => ReferenceEquals(item.Content, first));
            var followingHeader = Descendants<DeferredSequenceHeader>(tree).Single(item => ReferenceEquals(item.Content, second));
            Descendants<SequenceBlockView>(header).Should().BeEmpty();
            double previewHeight = header.ActualHeight;
            double previewSpacing = followingHeader.TranslatePoint(new Point(), header).Y;

            Settle(scope);

            Descendants<SequenceBlockView>(header).Should().ContainSingle();
            header.ActualHeight.Should().BeApproximately(previewHeight, 0.5, "realizing an instruction must not change its row height");
            followingHeader.TranslatePoint(new Point(), header).Y.Should().BeApproximately(previewSpacing, 0.5,
                "the next instruction must not jump when its preceding preview is replaced");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompiledSequenceView_TargetPreviewReservesExpandedPanelHeight(bool expanded) {
            using var scope = new CoreEditorTestScope();
            scope.Root.Add(new StartAreaContainer());
            var area = new TargetAreaContainer();
            scope.Root.Add(area);
            scope.Root.Add(new EndAreaContainer());
            var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
            target.Target.Expanded = expanded;
            target.ExposureInfoListExpanded = false;
            target.Add((TakeExposure)scope.Create(typeof(TakeExposure)));
            area.Add(target);
            foreach (bool state in new[] { expanded, !expanded, expanded }) {
                target.Target.Expanded = state;
                var view = new SequenceView { DataContext = new { Sequencer = new { Items = new[] { scope.Root } }, IsLocked = false, CanDragAndDrop = true } };
                scope.Host.Content = view;
                LayoutBeforeBackground(scope);
                var header = Descendants<DeferredSequenceHeader>(view).Single(item => ReferenceEquals(item.Content, target));
                Descendants<HierarchicalSequenceContainerView>(header).Should().BeEmpty();
                double previewHeight = header.ActualHeight;
                double previewCaptionY = Descendants<TextBlock>(header).Single(text => text.Text == NINA.Core.Locale.Loc.Instance["LblInstructions"]).TranslatePoint(new Point(), header).Y;
                Settle(scope);
                header.ActualHeight.Should().BeApproximately(previewHeight, 0.5, "target details must fit the reserved space even after changing expansion state between realizations");
                Descendants<TextBlock>(header).Single(text => text.Text == NINA.Core.Locale.Loc.Instance["LblInstructions"]).TranslatePoint(new Point(), header).Y
                    .Should().BeApproximately(previewCaptionY, 0.5, "the section headings must not move when target details appear");
                scope.Host.Content = null;
                Settle(scope);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompiledSequenceView_PreviewFrameStaysContinuousAcrossReservedHeightWhileScrolling(bool coloredBorders) {
            using var scope = new CoreEditorTestScope();
            var profiles = (NINA.Profile.Interfaces.IProfileService)Application.Current.FindResource("ProfileService");
            profiles.ActiveProfile.ApplicationSettings.ColoredContainerBorders = coloredBorders;
            scope.Root.Add(new StartAreaContainer());
            var area = new TargetAreaContainer();
            scope.Root.Add(area);
            scope.Root.Add(new EndAreaContainer());
            for (int index = 0; index < 12; index++) {
                var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
                target.IsExpanded = true;
                target.Target.Expanded = false;
                target.ExposureInfoListExpanded = false;
                for (int instruction = 0; instruction < 8; instruction++) target.Add((TakeExposure)scope.Create(typeof(TakeExposure)));
                area.Add(target);
            }
            var view = new SequenceView { DataContext = new { Sequencer = new { Items = new[] { scope.Root } }, IsLocked = false, CanDragAndDrop = true } };
            scope.Host.Content = view;
            LayoutBeforeBackground(scope);
            CheckPreviewFrames();
            Settle(scope);
            var tree = ((Grid)view.Content).Children.OfType<TreeView>().Single();
            var scroll = Descendants<ScrollViewer>(tree).First();
            scroll.ScrollToBottom();
            LayoutBeforeBackground(scope);
            CheckPreviewFrames();
            Settle(scope);
            scroll.ScrollToTop();
            LayoutBeforeBackground(scope);
            CheckPreviewFrames();
            Settle(scope);

            void CheckPreviewFrames() {
                var headers = Descendants<DeferredSequenceHeader>(view)
                    .Where(header => header.Content is DeepSkyObjectContainer && header.ActualHeight > 70
                        && !Descendants<HierarchicalSequenceContainerView>(header).Any()).ToArray();
                headers.Should().NotBeEmpty("scrolling must exercise cold and recreated target previews");
                var color = ((System.Windows.Media.SolidColorBrush)view.FindResource("SecondaryBackgroundBrush")).Color;
                foreach (var header in headers) {
                    int width = (int)Math.Ceiling(header.ActualWidth);
                    int height = (int)Math.Ceiling(header.ActualHeight);
                    var drawing = new System.Windows.Media.DrawingVisual();
                    using (var context = drawing.RenderOpen()) {
                        context.DrawRectangle(new System.Windows.Media.VisualBrush(header), null, new Rect(0, 0, width, height));
                    }
                    var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    bitmap.Render(drawing);
                    var pixel = new byte[4];
                    for (int y = 60; y < height - 10; y++) {
                        bitmap.CopyPixels(new Int32Rect(5, y, 1, 1), pixel, 4, 0);
                        pixel.Should().Equal(new[] { color.B, color.G, color.R, color.A },
                            $"the gray left frame must cover the reserved preview height at y={y}, with colored borders={coloredBorders}");
                    }
                }
            }
        }

        [Test]
        public void CompiledSequenceView_ThumbFarJumpsReachStablePositionsWithCorrectEditorsInBothDirections() {
            using var scope = new CoreEditorTestScope();
            var area = new TargetAreaContainer();
            scope.Root.Add(new StartAreaContainer());
            scope.Root.Add(area);
            scope.Root.Add(new EndAreaContainer());
            var exposures = new List<TakeExposure>();
            for (int targetIndex = 0; targetIndex < 12; targetIndex++) {
                var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
                target.Name = $"Target {targetIndex}";
                target.Target.Expanded = false;
                target.ExposureInfoListExpanded = false;
                var container = new SequentialContainer { IsExpanded = true };
                target.Add(container);
                for (int instructionIndex = 0; instructionIndex < 8; instructionIndex++) {
                    var exposure = (TakeExposure)scope.Create(typeof(TakeExposure));
                    exposure.ExposureTimeExpression.Definition = $"10+{exposures.Count}";
                    exposures.Add(exposure);
                    container.Add(exposure);
                }
                area.Add(target);
            }
            var originalDefinitions = exposures.ToDictionary(exposure => exposure, exposure => exposure.ExposureTimeExpression.Definition);
            var view = new SequenceView { DataContext = new { Sequencer = new { Items = new[] { scope.Root } }, IsLocked = false, CanDragAndDrop = true } };
            scope.Host.Content = view;
            Settle(scope);
            var tree = ((Grid)view.Content).Children.OfType<TreeView>().Single();
            var scroll = Descendants<ScrollViewer>(tree).First();
            var bar = (ScrollBar)scroll.Template.FindName("PART_VerticalScrollBar", scroll);
            var viewport = Descendants<ScrollContentPresenter>(scroll).First();
            scroll.ScrollableHeight.Should().BeGreaterThan(scroll.ViewportHeight * 3);
            CheckVisibleEditors();

            DragThumbTo(0.5);
            scroll.VerticalOffset.Should().BeGreaterThan(scroll.ViewportHeight);
            scroll.VerticalOffset.Should().BeLessThan(scroll.ScrollableHeight - scroll.ViewportHeight);
            var edited = CheckVisibleEditors().First();
            var editedExposure = exposures.Single(exposure => ReferenceEquals(edited.GetBindingExpression(TextBox.TextProperty)!.ResolvedSource, exposure.ExposureTimeExpression));
            CoreEditorHistoryTest.TypeText(edited, "42");
            scope.Behavior.Commit();
            editedExposure.ExposureTimeExpression.Definition.Should().Be("42");

            DragThumbTo(1);
            CheckVisibleEditors();
            DragThumbTo(0.5);
            CheckVisibleEditors();
            DragThumbTo(0);
            CheckVisibleEditors();
            editedExposure.ExposureTimeExpression.Definition.Should().Be("42");
            foreach (var exposure in exposures.Where(exposure => !ReferenceEquals(exposure, editedExposure))) {
                exposure.ExposureTimeExpression.Definition.Should().Be(originalDefinitions[exposure], "a recreated editor must remain bound to its own instruction");
            }

            void DragThumbTo(double fraction) {
                for (int attempt = 0; attempt < 10; attempt++) {
                    var thumb = bar.Track.Thumb;
                    double unitsPerPixel = bar.Track.ValueFromDistance(0, 1);
                    Math.Abs(unitsPerPixel).Should().BeGreaterThan(0);
                    double delta = (bar.Maximum * fraction - bar.Value) / unitsPerPixel;
                    thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
                    thumb.RaiseEvent(new DragDeltaEventArgs(0, delta) { RoutedEvent = Thumb.DragDeltaEvent });
                    thumb.RaiseEvent(new DragCompletedEventArgs(0, delta, false) { RoutedEvent = Thumb.DragCompletedEvent });
                    Settle(scope);
                    if (fraction != 0 && fraction != 1 || Math.Abs(scroll.VerticalOffset - scroll.ScrollableHeight * fraction) < 0.5) break;
                }
                if (fraction == 0 || fraction == 1) scroll.VerticalOffset.Should().BeApproximately(scroll.ScrollableHeight * fraction, 0.5);
                double offset = scroll.VerticalOffset;
                double extent = scroll.ExtentHeight;
                Settle(scope);
                scroll.VerticalOffset.Should().BeApproximately(offset, 0.5, "a completed thumb jump must not continue changing the viewport without input");
                scroll.ExtentHeight.Should().BeApproximately(extent, 0.5, "a completed thumb jump must settle its height estimate");
            }

            TextBox[] CheckVisibleEditors() {
                var editors = Descendants<TextBox>(tree).Where(editor => {
                    if (!editor.IsVisible || !exposures.Any(exposure => ReferenceEquals(editor.GetBindingExpression(TextBox.TextProperty)?.ResolvedSource, exposure.ExposureTimeExpression))) return false;
                    var bounds = editor.TransformToAncestor(viewport).TransformBounds(new Rect(editor.RenderSize));
                    return bounds.Width > 0 && bounds.Height > 0 && bounds.IntersectsWith(new Rect(viewport.RenderSize));
                }).ToArray();
                editors.Should().NotBeEmpty("the settled viewport must contain usable exposure editors after a far jump");
                foreach (var editor in editors) {
                    var exposure = exposures.Single(item => ReferenceEquals(editor.GetBindingExpression(TextBox.TextProperty)!.ResolvedSource, item.ExposureTimeExpression));
                    editor.Text.Should().Be(exposure.ExposureTimeExpression.Definition);
                }
                return editors;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompiledSequenceView_PreparesNearbyEditorsBeforeWheelScrolling(bool up) {
            using var scope = new CoreEditorTestScope();
            var area = new TargetAreaContainer();
            scope.Root.Add(new StartAreaContainer());
            scope.Root.Add(area);
            scope.Root.Add(new EndAreaContainer());
            var container = new SequentialContainer { IsExpanded = true };
            area.Add(container);
            for (int index = 0; index < 100; index++) container.Add((TakeExposure)scope.Create(typeof(TakeExposure)));
            var view = new SequenceView { DataContext = new { Sequencer = new { Items = new[] { scope.Root } }, IsLocked = false, CanDragAndDrop = true } };
            scope.Host.Content = view;
            Settle(scope);
            var tree = ((Grid)view.Content).Children.OfType<TreeView>().Single();
            var scroll = Descendants<ScrollViewer>(tree).First();
            var viewport = Descendants<ScrollContentPresenter>(scroll).First();
            scroll.ScrollToVerticalOffset(scroll.ScrollableHeight / 2);
            Settle(scope);

            var nearby = Descendants<DeferredSequenceHeader>(tree).Where(header => header.Content is TakeExposure).FirstOrDefault(header => {
                var bounds = header.TransformToAncestor(viewport).TransformBounds(new Rect(header.RenderSize));
                return up ? bounds.Bottom <= 0 && bounds.Bottom > -48 : bounds.Top >= viewport.ActualHeight && bounds.Top < viewport.ActualHeight + 48;
            });
            nearby.Should().NotBeNull("a small cache must generate instruction rows just outside either viewport edge");
            var editor = Descendants<SequenceBlockView>(nearby!).SingleOrDefault();
            editor.Should().NotBeNull("nearby editors must be ready before scrolling makes them visible");
            double before = scroll.VerticalOffset;
            scroll.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, up ? 120 : -120) {
                RoutedEvent = UIElement.MouseWheelEvent
            });
            LayoutBeforeBackground(scope);
            if (up) scroll.VerticalOffset.Should().BeLessThan(before); else scroll.VerticalOffset.Should().BeGreaterThan(before);
            var visibleBounds = nearby!.TransformToAncestor(viewport).TransformBounds(new Rect(nearby.RenderSize));
            visibleBounds.IntersectsWith(new Rect(viewport.RenderSize)).Should().BeTrue();
            Descendants<SequenceBlockView>(nearby).Single().Should().BeSameAs(editor,
                "wheel scrolling should reveal the prepared editor without waiting for Background construction");
        }
        private static ScrollViewer Show(CoreEditorTestScope scope, UIElement content) {
            var scroll = new ScrollViewer {
                Content = content,
                Width = 400,
                Height = 240,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible
            };
            scope.Host.Content = scroll;
            LayoutBeforeBackground(scope);
            return scroll;
        }

        private static DataTemplate ProbeTemplate(string? text = null) {
            var factory = new FrameworkElementFactory(typeof(ProbeHeader));
            factory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            if (text == null) factory.SetBinding(TextBlock.TextProperty, new Binding(nameof(SequentialContainer.Name)));
            else factory.SetValue(TextBlock.TextProperty, text);
            return new DataTemplate { VisualTree = factory };
        }

        private static void LayoutBeforeBackground(CoreEditorTestScope scope) {
            scope.Host.UpdateLayout();
            PumpTo(DispatcherPriority.Input);
            scope.Host.UpdateLayout();
        }

        private static void Settle(CoreEditorTestScope scope) {
            for (int pass = 0; pass < 3; pass++) {
                scope.Host.UpdateLayout();
                PumpTo(DispatcherPriority.SystemIdle);
            }
            scope.Host.UpdateLayout();
        }

        private static void PumpTo(DispatcherPriority priority) {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(priority, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (ScrollViewer Scroll, WeakReference Header, WeakReference Model, WeakReference Template) CreateAndRelease(CoreEditorTestScope scope, bool realized) {
            var model = new SequentialContainer { Name = "Released model" };
            var template = ProbeTemplate();
            var header = new DeferredSequenceHeader { Content = model, ContentTemplate = template };
            var scroll = Show(scope, header);
            if (realized) Settle(scope);
            var result = (scroll, new WeakReference(header), new WeakReference(model), new WeakReference(template));
            scroll.Content = null;
            LayoutBeforeBackground(scope);
            return result;
        }

        public class ProbeHeader : TextBlock {
            public ProbeHeader() => constructions++;
        }

        private sealed class RecordingTemplateSelector(DataTemplate template) : DataTemplateSelector {
            public int Calls { get; private set; }
            public object? LastItem { get; private set; }
            public DependencyObject? LastContainer { get; private set; }

            public override DataTemplate SelectTemplate(object item, DependencyObject container) {
                Calls++;
                LastItem = item;
                LastContainer = container;
                return template;
            }
        }
    }
}
