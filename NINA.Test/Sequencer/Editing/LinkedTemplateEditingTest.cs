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
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Editing;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using NINA.Test.Sequencer.Container;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class LinkedTemplateEditingTest {
        [SetUp]
        public void SetUp() {
            _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task CompiledTemplate_ExistingInstructionsRemainEditableAcrossSessions(bool hierarchical, bool save) {
            using var scope = new CoreEditorTestScope();
            TemplateLinkResolver resolver = new();
            TemplateReference reference = new() { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Edit.template.json" };
            SequentialContainer source = new();
            source.Add(new WaitForTimeSpan { Time = 10 });
            IProfileService profiles = (IProfileService)Application.Current.Resources["ProfileService"];
            TemplatedSequenceContainer Template(ISequenceContainer content) => new(profiles, "Test", content, reference, resolver);
            int saves = 0;
            resolver.UpdateTemplates(new[] { Template(source) }, true, (_, content, _) => {
                saves++;
                resolver.UpdateTemplates(new[] { Template((ISequenceContainer)content.Clone()) }, true, null);
                return Task.CompletedTask;
            });
            LinkedTemplateContainer linked = new(resolver) { TemplateReference = reference, IsExpanded = true };
            Show(scope, linked, hierarchical);

            for (int session = 0; session < 2; session++) {
                SequenceContainer content = (SequenceContainer)linked.Items.Single();
                WaitForTimeSpan existing = (WaitForTimeSpan)content.Items.Single();
                Parameter(scope, existing).IsHitTestVisible.Should().BeFalse();
                Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, linked.BeginEditTemplateCommand) && b.IsVisible));
                linked.IsEditing.Should().BeTrue();
                Edit(scope, existing, "12");

                content.DropIntoCommand.Execute(new DropIntoParameters(new WaitForTimeSpan { Time = 30 }, null, DropTargetEnum.Center));
                Drain();
                WaitForTimeSpan added = content.Items.OfType<WaitForTimeSpan>().Single(i => !ReferenceEquals(i, existing));
                Edit(scope, added, "31");
                Button delete = Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.DataContext, existing)
                    && SequenceEditContext.GetOperation(b) == SequenceEditOperation.Delete);
                Click(delete);
                content.Items.Should().ContainSingle().Which.Should().BeSameAs(added);

                if (save) await linked.SaveTemplateCommand.ExecuteAsync(null);
                else linked.CancelEditTemplateCommand.Execute(null);
                Drain();
                linked.IsEditing.Should().BeFalse();
                WaitForTimeSpan restored = (WaitForTimeSpan)((SequenceContainer)linked.Items.Single()).Items.Single();
                restored.Time.Should().Be(save ? 31 : 10);
                Parameter(scope, restored).IsHitTestVisible.Should().BeFalse();

                // Exercise the behavior's unload/load lifecycle without recreating the model.
                object view = scope.Host.Content;
                scope.Host.Content = null;
                Drain();
                scope.Host.Content = view;
                Drain();
            }
            saves.Should().Be(save ? 2 : 0);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task CompiledTemplate_NestedLinkRemainsReadOnlyUntilItsOwnEditSession(bool hierarchical, bool save) {
            using var scope = new CoreEditorTestScope();
            TemplateLinkResolver resolver = new();
            TemplateReference innerReference = new() { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Inner.template.json" };
            TemplateReference outerReference = new() { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Outer.template.json" };
            IProfileService profiles = (IProfileService)Application.Current.Resources["ProfileService"];
            SequentialContainer innerSource = new();
            innerSource.Add(new WaitForTimeSpan { Time = 10 });
            TemplatedSequenceContainer innerTemplate = new(profiles, "Test", innerSource, innerReference, resolver);
            resolver.UpdateTemplates(new[] { innerTemplate }, true, null);
            SequentialContainer outerSource = new();
            outerSource.Add(new LinkedTemplateContainer(resolver) { TemplateReference = innerReference, IsExpanded = true });
            var outerTemplate = new TemplatedSequenceContainer(profiles, "Test", outerSource, outerReference, resolver);
            resolver.UpdateTemplates(new[] { innerTemplate, outerTemplate }, true, (_, content, _) => {
                resolver.UpdateTemplates(new[] { new TemplatedSequenceContainer(profiles, "Test", (ISequenceContainer)content.Clone(), innerReference, resolver), outerTemplate }, true, null);
                return Task.CompletedTask;
            });
            LinkedTemplateContainer outer = new(resolver) { TemplateReference = outerReference, IsExpanded = true };
            Show(scope, outer, hierarchical);
            LinkedTemplateContainer inner = (LinkedTemplateContainer)((SequenceContainer)outer.Items.Single()).Items.Single();
            WaitForTimeSpan instruction = (WaitForTimeSpan)((SequenceContainer)inner.Items.Single()).Items.Single();
            Parameter(scope, instruction).IsHitTestVisible.Should().BeFalse();
            Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, outer.BeginEditTemplateCommand) && b.IsVisible));
            Parameter(scope, instruction).IsHitTestVisible.Should().BeFalse();
            Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, inner.BeginEditTemplateCommand) && b.IsVisible));
            Edit(scope, instruction, "12");
            if (save) await inner.SaveTemplateCommand.ExecuteAsync(null);
            else inner.CancelEditTemplateCommand.Execute(null);
            Drain();
            instruction = (WaitForTimeSpan)((SequenceContainer)inner.Items.Single()).Items.Single();
            instruction.Time.Should().Be(save ? 12 : 10);
            Parameter(scope, instruction).IsHitTestVisible.Should().BeFalse();
            scope.History.ActiveHistory.IsEnabled.Should().BeTrue("the outer editor is still open after the inner Save or Cancel");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompiledTemplate_ReopeningKeepsStatusAndProgressBindings(bool hierarchical) {
            using var scope = new CoreEditorTestScope();
            TemplateLinkResolver resolver = new();
            TemplateReference reference = new() { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Progress.template.json" };
            SequentialContainer source = new();
            source.Add((TakeManyExposures)scope.Create(typeof(TakeManyExposures)));
            resolver.UpdateTemplates(new[] { new TemplatedSequenceContainer(
                (IProfileService)Application.Current.Resources["ProfileService"], "Test", source, reference, resolver) }, true, null);
            LinkedTemplateContainer linked = new(resolver) { TemplateReference = reference, IsExpanded = true };
            Show(scope, linked, hierarchical);
            TakeManyExposures instruction = (TakeManyExposures)((SequenceContainer)linked.Items.Single()).Items.Single();
            LoopCondition loop = (LoopCondition)instruction.Conditions.Single();
            instruction.Status = SequenceEntityStatus.RUNNING;
            linked.Status = SequenceEntityStatus.RUNNING;
            linked.IsExpanded = false;
            linked.IsExpanded = true;
            loop.CompletedIterations = 1;
            Drain();

            ((SequenceContainer)linked.Items.Single()).Items.Single().Should().BeSameAs(instruction);
            TextBlock completed = Descendants<TextBlock>(scope.Host).Single(t =>
                t.GetBindingExpression(TextBlock.TextProperty)?.ResolvedSourcePropertyName == nameof(LoopCondition.CompletedIterations)
                && ReferenceEquals(t.GetBindingExpression(TextBlock.TextProperty)?.ResolvedSource, loop));
            completed.Text.Should().Be("1");
            ContentPresenter status = Descendants<ContentPresenter>(scope.Host).Single(p =>
                ReferenceEquals(p.DataContext, instruction) && p.Style == p.TryFindResource("ProgressPresenter"));
            status.ContentTemplate.Should().BeSameAs(status.FindResource("RunningItem"));
            loop.CompletedIterations = 2;
            instruction.Status = SequenceEntityStatus.FINISHED;
            Drain();
            completed.Text.Should().Be("2");
            status.ContentTemplate.Should().NotBeSameAs(status.FindResource("RunningItem"));
        }

        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, false)]
        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        public async Task CompiledTemplate_WaitingEditorTracksOnlyDeliberateInput(bool hierarchical, bool save, bool fail) {
            using var scope = new CoreEditorTestScope();
            var resolver = new TemplateLinkResolver();
            var reference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Waiting.template.json" };
            var source = new SequentialContainer();
            source.Add(new WaitForTimeSpan { Time = 60 });
            TemplatedSequenceContainer Template(ISequenceContainer content) => new(
                (IProfileService)Application.Current.Resources["ProfileService"], "Test", content, reference, resolver);
            var saveEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var finishSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            resolver.UpdateTemplates(new[] { Template(source) }, true, async (_, content, _) => {
                saveEntered.SetResult();
                await finishSave.Task;
                if (fail) throw new IOException("Save failed for this regression");
                resolver.UpdateTemplates(new[] { Template((ISequenceContainer)content.Clone()) }, true, null);
            });
            var linked = new LinkedTemplateContainer(resolver) { TemplateReference = reference, IsExpanded = true };
            Show(scope, linked, hierarchical);
            Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, linked.BeginEditTemplateCommand) && b.IsVisible));
            var instruction = (WaitForTimeSpan)((SequenceContainer)linked.Items.Single()).Items.Single();
            var box = Parameter(scope, instruction);
            var clock = new LinkedTemplateTestClock();
            clock.Install(linked);
            using var cancellation = new CancellationTokenSource();
            Task run = linked.Run(new Progress<ApplicationStatus>(), cancellation.Token);
            try {
                await LinkedTemplateTestClock.Until(() => linked.IsWaitingForEdits && clock.PendingTimers > 0);
                clock.Advance(TimeSpan.FromSeconds(299));
                await LinkedTemplateTestClock.Until(() => linked.LinkStatusText.Contains("00:01") && clock.PendingTimers > 0);
                Drain();
                Descendants<TextBlock>(scope.Host).Should().Contain(t => t.Text == linked.LinkStatusText);
                box.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.PreviewMouseMoveEvent });
                box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, box) { RoutedEvent = Keyboard.GotKeyboardFocusEvent });
                scope.Window.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 120) { RoutedEvent = Mouse.PreviewMouseWheelEvent });
                linked.LinkStatusText.Should().Contain("00:01", "focus, motion and input outside the editor are not activity");
                Edit(scope, instruction, "61");
                linked.LinkStatusText.Should().Contain("05:00");
                clock.Advance(TimeSpan.FromSeconds(299));
                await LinkedTemplateTestClock.Until(() => linked.LinkStatusText.Contains("00:01") && clock.PendingTimers > 0);
                box.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 120) { RoutedEvent = Mouse.PreviewMouseWheelEvent });
                linked.LinkStatusText.Should().Contain("05:00");
                clock.Advance(TimeSpan.FromSeconds(299));
                await LinkedTemplateTestClock.Until(() => linked.LinkStatusText.Contains("00:01") && clock.PendingTimers > 0);
                box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(box), 0, Key.Left) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                linked.LinkStatusText.Should().Contain("05:00");
                clock.Advance(TimeSpan.FromSeconds(299));
                await LinkedTemplateTestClock.Until(() => linked.LinkStatusText.Contains("00:01") && clock.PendingTimers > 0);
                box.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });
                linked.LinkStatusText.Should().Contain("05:00");
                instruction.Status.Should().Be(SequenceEntityStatus.CREATED);
                var contents = (SequenceContainer)linked.Items.Single();
                contents.DropIntoCommand.Execute(new DropIntoParameters(new WaitForTimeSpan { Time = 30 }, null, DropTargetEnum.Center));
                Drain();
                var added = contents.Items.OfType<WaitForTimeSpan>().Single(i => !ReferenceEquals(i, instruction));
                Edit(scope, added, "31");
                Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.DataContext, added)
                    && SequenceEditContext.GetOperation(b) == SequenceEditOperation.Delete));
                contents.Items.Should().ContainSingle().Which.Should().BeSameAs(instruction);
                if (save) {
                    Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, linked.SaveTemplateCommand) && b.IsVisible));
                    await saveEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    Drain();
                    Parameter(scope, instruction).IsHitTestVisible.Should().BeFalse();
                    scope.History.ActiveHistory.CanUndo.Should().BeFalse("history must not mutate a template being saved");
                    Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, linked.CancelEditTemplateCommand) && b.IsVisible).IsEnabled.Should().BeFalse();
                    var input = new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, "9")) {
                        RoutedEvent = TextCompositionManager.PreviewTextInputEvent
                    };
                    box.RaiseEvent(input);
                    input.Handled.Should().BeTrue("a previously focused editor cannot change an accepted Save");
                    clock.Advance(TimeSpan.FromMinutes(10));
                    linked.IsEditing.Should().BeTrue();
                    instruction.Status.Should().Be(SequenceEntityStatus.CREATED);
                    finishSave.TrySetResult();
                    await LinkedTemplateTestClock.Until(() => {
                        Drain();
                        return fail ? linked.SaveTemplateCommand.CanExecute(null) : !linked.IsEditing;
                    });
                    linked.IsEditing.Should().Be(fail);
                    if (fail) {
                        scope.History.ActiveHistory.CanUndo.Should().BeTrue();
                        linked.LinkStatusText.Should().Contain("05:00");
                        Drain();
                        Parameter(scope, instruction).IsHitTestVisible.Should().BeTrue();
                        Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, linked.CancelEditTemplateCommand) && b.IsVisible));
                    }
                } else {
                    Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, linked.CancelEditTemplateCommand) && b.IsVisible));
                }
                await LinkedTemplateTestClock.Until(() => !linked.IsEditing && ((SequenceContainer)linked.Items.Single()).Items.Single().Status == SequenceEntityStatus.RUNNING);
                var executing = ((SequenceContainer)linked.Items.Single()).Items.Single();
                ((WaitForTimeSpan)executing).Time.Should().Be(save && !fail ? 61 : 60);
                linked.IsExpanded = false;
                linked.IsExpanded = true;
                Drain();
                ((SequenceContainer)linked.Items.Single()).Items.Single().Should().BeSameAs(executing);
                var status = Descendants<ContentPresenter>(scope.Host).Single(p => ReferenceEquals(p.DataContext, executing) && p.Style == p.TryFindResource("ProgressPresenter"));
                status.ContentTemplate.Should().BeSameAs(status.FindResource("RunningItem"));
                linked.BeginEditTemplateCommand.CanExecute(null).Should().BeFalse();
            } finally {
                finishSave.TrySetResult();
                cancellation.Cancel();
                try { await run.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { }
            }
            linked.BeginEditTemplateCommand.CanExecute(null).Should().BeTrue();
        }

        [Test]
        public async Task CompiledTemplate_SaveFlushFailureRetainsEditorAndRestartsDeadline() {
            using var scope = new CoreEditorTestScope();
            var resolver = new TemplateLinkResolver();
            var reference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "FlushFailure.template.json" };
            var source = new SequentialContainer();
            source.Add(new WaitForTimeSpan { Time = 60 });
            resolver.UpdateTemplates(new[] { new TemplatedSequenceContainer(
                (IProfileService)Application.Current.Resources["ProfileService"], "Test", source, reference, resolver) }, true, null);
            var linked = new LinkedTemplateContainer(resolver) { TemplateReference = reference, IsExpanded = true };
            Show(scope, linked, false);
            Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, linked.BeginEditTemplateCommand) && b.IsVisible));
            var clock = new LinkedTemplateTestClock();
            clock.Install(linked);
            using var stop = new CancellationTokenSource();
            var run = linked.Run(new Progress<ApplicationStatus>(), stop.Token);
            try {
                await LinkedTemplateTestClock.Until(() => clock.PendingTimers > 0);
                clock.Advance(TimeSpan.FromSeconds(299));
                await LinkedTemplateTestClock.Until(() => linked.LinkStatusText.Contains("00:01"));
                void FailFlush() => throw new IOException("Editor flush failed for this regression");
                scope.History.FlushRequested += FailFlush;
                try { await linked.SaveTemplateCommand.ExecuteAsync(null); }
                finally { scope.History.FlushRequested -= FailFlush; }
                linked.LinkStatusText.Should().Contain("05:00");
                linked.IsEditing.Should().BeTrue();
                linked.CanSaveTemplate.Should().BeTrue();
                linked.CanCancelTemplate.Should().BeTrue();
                linked.CanEditContents.Should().BeTrue();
            } finally {
                stop.Cancel();
                try { await run.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CompiledTemplate_NestedTimeoutHandsOffFromWorkerToEditor(bool hierarchical) {
            using var scope = new CoreEditorTestScope();
            var resolver = new TemplateLinkResolver();
            var innerReference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "InnerTimeout.template.json" };
            var outerReference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "OuterTimeout.template.json" };
            var profiles = (IProfileService)Application.Current.Resources["ProfileService"];
            var innerSource = new SequentialContainer();
            innerSource.Add(new WaitForTimeSpan { Time = 60 });
            var innerTemplate = new TemplatedSequenceContainer(profiles, "Test", innerSource, innerReference, resolver);
            resolver.UpdateTemplates(new[] { innerTemplate }, true, null);
            var outerSource = new SequentialContainer();
            outerSource.Add(new LinkedTemplateContainer(resolver) { TemplateReference = innerReference, IsExpanded = true });
            resolver.UpdateTemplates(new[] { innerTemplate, new TemplatedSequenceContainer(profiles, "Test", outerSource, outerReference, resolver) }, true, null);
            var outer = new LinkedTemplateContainer(resolver) { TemplateReference = outerReference, IsExpanded = true };
            Show(scope, outer, hierarchical);
            var inner = (LinkedTemplateContainer)((ISequenceContainer)outer.Items.Single()).Items.Single();
            foreach (var link in new[] { outer, inner }) Click(Descendants<Button>(scope.Host)
                .Single(b => ReferenceEquals(b.Command, link.BeginEditTemplateCommand) && b.IsVisible));
            var instruction = (WaitForTimeSpan)((ISequenceContainer)inner.Items.Single()).Items.Single();
            Edit(scope, instruction, "61");
            var clock = new LinkedTemplateTestClock();
            clock.Install(outer);
            using var stop = new CancellationTokenSource();
            var run = Task.Run(() => outer.Run(new Progress<ApplicationStatus>(), stop.Token));
            try {
                await LinkedTemplateTestClock.Until(() => { Drain(); return outer.IsWaitingForEdits && clock.PendingTimers > 0; });
                outer.CanCancelTemplate.Should().BeFalse();
                clock.Advance(TimeSpan.FromSeconds(299));
                await LinkedTemplateTestClock.Until(() => { Drain(); return outer.LinkStatusText.Contains("00:01") && clock.PendingTimers > 0; });
                Parameter(scope, instruction).RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 120) { RoutedEvent = Mouse.PreviewMouseWheelEvent });
                outer.LinkStatusText.Should().Contain("05:00");
                clock.Advance(TimeSpan.FromMinutes(5));
                await LinkedTemplateTestClock.Until(() => { Drain(); return !outer.IsEditing; });
                inner.IsEditing.Should().BeFalse();
                var executingInner = (LinkedTemplateContainer)((ISequenceContainer)outer.Items.Single()).Items.Single();
                await LinkedTemplateTestClock.Until(() => { Drain(); return executingInner.IsMaterialized && ((ISequenceContainer)executingInner.Items.Single()).Items.Single().Status == SequenceEntityStatus.RUNNING; });
                var executing = (WaitForTimeSpan)((ISequenceContainer)executingInner.Items.Single()).Items.Single();
                executing.Time.Should().Be(60);
                Parameter(scope, executing).IsHitTestVisible.Should().BeFalse();
                outer.BeginEditTemplateCommand.CanExecute(null).Should().BeFalse();
            } finally {
                stop.Cancel();
                await LinkedTemplateTestClock.Until(() => { Drain(); return run.IsCompleted; });
                try { await run; } catch (OperationCanceledException) { }
            }
            outer.BeginEditTemplateCommand.CanExecute(null).Should().BeTrue();
        }

        private static void Show(CoreEditorTestScope scope, LinkedTemplateContainer linked, bool hierarchical) {
            if (!hierarchical) {
                scope.Show(linked);
                return;
            }
            scope.Root.Add(linked);
            TreeView tree = new() { ItemContainerStyle = new Style(typeof(TreeViewItem)) };
            tree.ItemContainerStyle.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, true));
            tree.Items.Add(linked);
            scope.Host.Content = tree;
            Drain();
        }

        private static TextBox Parameter(CoreEditorTestScope scope, WaitForTimeSpan item) => Descendants<TextBox>(scope.Host)
            .Single(t => ReferenceEquals(t.GetBindingExpression(TextBox.TextProperty)?.ResolvedSource, item.TimeExpression));

        private static void Edit(CoreEditorTestScope scope, WaitForTimeSpan item, string text) {
            TextBox box = Parameter(scope, item);
            box.IsHitTestVisible.Should().BeTrue();
            box.IsEnabled.Should().BeTrue();
            box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, text)) {
                RoutedEvent = TextCompositionManager.PreviewTextInputEvent
            });
            box.SetCurrentValue(TextBox.TextProperty, text);
            box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            scope.Behavior.Commit();
            item.TimeExpression.Definition.Should().Be(text);
        }

        private static void Click(Button button) {
            button.IsHitTestVisible.Should().BeTrue();
            button.IsEnabled.Should().BeTrue();
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            Drain();
        }
    }
}
