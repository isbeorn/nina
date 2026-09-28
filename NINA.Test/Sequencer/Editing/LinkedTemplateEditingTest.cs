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
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Editing;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
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

        [TestCase(false)]
        [TestCase(true)]
        public void CompiledTemplate_NestedLinkRemainsReadOnlyUntilItsOwnEditSession(bool hierarchical) {
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
            resolver.UpdateTemplates(new[] { innerTemplate, new TemplatedSequenceContainer(profiles, "Test", outerSource, outerReference, resolver) }, true, null);
            LinkedTemplateContainer outer = new(resolver) { TemplateReference = outerReference, IsExpanded = true };
            Show(scope, outer, hierarchical);
            LinkedTemplateContainer inner = (LinkedTemplateContainer)((SequenceContainer)outer.Items.Single()).Items.Single();
            WaitForTimeSpan instruction = (WaitForTimeSpan)((SequenceContainer)inner.Items.Single()).Items.Single();
            Parameter(scope, instruction).IsHitTestVisible.Should().BeFalse();
            Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, outer.BeginEditTemplateCommand) && b.IsVisible));
            Parameter(scope, instruction).IsHitTestVisible.Should().BeFalse();
            Click(Descendants<Button>(scope.Host).Single(b => ReferenceEquals(b.Command, inner.BeginEditTemplateCommand) && b.IsVisible));
            Edit(scope, instruction, "12");
            inner.CancelEditTemplateCommand.Execute(null);
            Drain();
            instruction = (WaitForTimeSpan)((SequenceContainer)inner.Items.Single()).Items.Single();
            instruction.Time.Should().Be(10);
            Parameter(scope, instruction).IsHitTestVisible.Should().BeFalse();
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
