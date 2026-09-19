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
using NINA.Sequencer.Container;
using NINA.Sequencer.Editing;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NUnit.Framework;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Trigger;
using NINA.Astrometry;
using System.Diagnostics;
using Expression = NINA.Sequencer.Logic.Expression;
using NINA.Sequencer;
using NINA.Sequencer.DragDrop;
using NINA.Profile.Interfaces;
using Moq;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class SequenceEditHistoryTest {
        public class PluginItem : NINA.Sequencer.SequenceItem.SequenceItem {
            public double Setting { get; set; }
            public Expression Formula { get; set; } = new Expression("1", null);
            public int Progress { get; set; }
            public string Text { get; set; } = "before";
            public bool Enabled { get; set; }
            public InputCoordinates Coordinates { get; set; } = new();
            public TaskCompletionSource<bool>? Completion { get; set; }
            public ManualResetEventSlim? Started { get; set; }
            public int Executions { get; private set; }
            public override object Clone() => new PluginItem { Setting = Setting };
            public override Task Execute(IProgress<NINA.Core.Model.ApplicationStatus> progress, CancellationToken token) {
                Executions++;
                Started?.Set();
                return Completion?.Task.WaitAsync(token) ?? Task.CompletedTask;
            }
        }

        private sealed class CustomCommandItem : PluginItem {
            public override ICommand DetachCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(() => Parent.Remove(this));
        }

        private sealed class CountingRoot : SequenceRootContainer, ISequenceContainer {
            public int Reads { get; set; }
            ICollection<ISequenceItem> ISequenceContainer.GetItemsSnapshot() { Reads++; return base.GetItemsSnapshot(); }
        }

        private static void Record(SequenceEditHistory history, PluginItem item, double value) {
            var capture = SequencePropertyCapture.Capture("Setting", () => item.Setting, v => item.Setting = v);
            item.Setting = value;
            history.RecordApplied(capture.Complete());
        }

        [Test]
        public void Journal_BranchingSavedMarkerNoOpsAndCapacity() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            Record(history, item, 1);
            history.MarkSaved();
            Record(history, item, 2);
            Record(history, item, 2);
            history.Position.Should().Be(2);
            history.Undo().Should().BeTrue();
            history.Entries.Single(x => x.IsCurrent).IsSaved.Should().BeTrue();
            Record(history, item, 3);
            history.CanRedo.Should().BeFalse();
            for (int i = 4; i <= 110; i++) Record(history, item, i);
            history.Entries.Should().HaveCount(101);
            history.MoveTo(0);
            item.Setting.Should().Be(10);
            history.CanUndo.Should().BeFalse();
            history.MoveTo(100);
            item.Setting.Should().Be(110);
        }

        [Test]
        public void Journal_StateChangesKeepExistingRowsAndNotifyBindings() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            Record(history, item, 1);
            Record(history, item, 2);
            SequenceEditEntry[] rows = history.Entries.ToArray();
            int collectionChanges = 0;
            ((System.Collections.Specialized.INotifyCollectionChanged)history.Entries).CollectionChanged += (_, _) => collectionChanges++;
            var rowChanges = new List<string>();
            rows[1].PropertyChanged += (_, e) => rowChanges.Add(e.PropertyName!);

            history.HasPendingEdit = true;
            history.HasPendingEdit = false;
            history.IsEnabled = false;
            history.IsEnabled = true;
            history.Undo().Should().BeTrue();
            history.MarkSaved();
            history.Redo().Should().BeTrue();

            collectionChanges.Should().Be(0, "state changes must not rebuild the history sidebar");
            for (int i = 0; i < rows.Length; i++) history.Entries[i].Should().BeSameAs(rows[i]);
            rows[1].IsSaved.Should().BeTrue();
            rows[1].IsCurrent.Should().BeFalse();
            rows[2].IsCurrent.Should().BeTrue();
            rowChanges.Should().Contain(nameof(SequenceEditEntry.IsCurrent)).And.Contain(nameof(SequenceEditEntry.IsSaved));
        }

        [Test]
        public void HistoryNavigation_FlushesPendingEditBeforeResolvingTheSelectedRow() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            for (int i = 1; i <= 100; i++) Record(history, item, i);
            SequenceEditEntry selected = history.Entries[50];
            bool pending = true;
            history.FlushRequested += () => {
                if (!pending) return;
                pending = false;
                Record(history, item, 101);
            };

            history.MoveToCommand.Execute(selected);

            item.Setting.Should().Be(50, "committing the field evicts the oldest entry but must not change the selected state");
            history.Entries.Single(entry => entry.IsCurrent).Should().BeSameAs(selected);
        }

        [Test]
        public void Journal_AppendBranchAndEvictionRetainUnaffectedRowsAndSavedState() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            Record(history, item, 1);
            history.MarkSaved();
            Record(history, item, 2);
            SequenceEditEntry retained = history.Entries[2];
            for (int i = 3; i <= 101; i++) Record(history, item, i);
            history.Entries[0].IsSaved.Should().BeTrue("the evicted edit becomes the retained baseline");
            history.Entries[1].Should().BeSameAs(retained);
            retained.Position.Should().Be(1);

            history.MoveToCommand.Execute(retained);
            item.Setting.Should().Be(2);
            Record(history, item, 200);
            history.Entries[1].Should().BeSameAs(retained);
            history.Entries.Should().HaveCount(3);
            history.CanRedo.Should().BeFalse();
            for (int i = 201; i <= 299; i++) Record(history, item, i);
            history.Entries.Should().HaveCount(101);
            history.Entries.Should().NotContain(entry => entry.IsSaved, "branching must not reuse an evicted state's saved marker");
        }

        [Test]
        public void Composite_OverlappingPropertiesReplayInOrder() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            using (history.BeginTransaction("Both")) {
                Record(history, item, 1);
                using (history.BeginTransaction("Nested")) Record(history, item, 2);
            }
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            item.Setting.Should().Be(0);
            history.Redo().Should().BeTrue();
            item.Setting.Should().Be(2);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Conflict_DoesNotMoveCursorOrChangeExternalValue(bool undo) {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            string? error = null;
            history.ReplayFailed += value => error = value;
            Record(history, item, 1);
            if (!undo) history.Undo().Should().BeTrue();
            item.Setting = 99;
            (undo ? history.Undo() : history.Redo()).Should().BeFalse();
            history.Position.Should().Be(undo ? 1 : 0);
            item.Setting.Should().Be(99);
            error.Should().NotBeNullOrEmpty();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReplayFailure_CompensatesEarlierEditsAndPreservesHistory(bool undo) {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            int other = 1;
            bool fail = false;
            var throwingEdit = new PropertySequenceEdit<int>("Throw", () => other, value => {
                other = value;
                if (fail && value == (undo ? 0 : 1)) throw new InvalidOperationException("setter failed");
            }, 0, 1);
            using (history.BeginTransaction("Composite")) {
                if (undo) history.RecordApplied(throwingEdit);
                Record(history, item, 4);
                if (!undo) history.RecordApplied(throwingEdit);
            }
            if (!undo) history.Undo().Should().BeTrue();
            fail = true;
            (undo ? history.Undo() : history.Redo()).Should().BeFalse();
            item.Setting.Should().Be(undo ? 4 : 0);
            other.Should().Be(undo ? 1 : 0);
            history.Entries.Should().HaveCount(2);
            history.Position.Should().Be(undo ? 1 : 0);
            fail = false;
            (undo ? history.Undo() : history.Redo()).Should().BeTrue("a verified rollback allows retry");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReplayFailure_UnverifiableCompensationInvalidatesHistory(bool redo) {
            using var history = new SequenceEditHistory(new SequenceRootContainer());
            int value = 1;
            bool fail = false;
            history.RecordApplied(new PropertySequenceEdit<int>("Setting", () => value, next => {
                value = fail ? -1 : next;
                if (fail) throw new InvalidOperationException("Both restore and compensation fail");
            }, 0, 1));
            if (redo) history.Undo().Should().BeTrue();
            string? error = null;
            history.ReplayFailed += message => error = message;
            fail = true;
            (redo ? history.Redo() : history.Undo()).Should().BeFalse();
            value.Should().Be(-1);
            history.Entries.Should().HaveCount(1);
            history.CanUndo.Should().BeFalse();
            history.CanRedo.Should().BeFalse();
            error.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void Commands_DuplicateDeleteAndMoveBothDirections_PreserveInstances() {
            var root = new SequenceRootContainer();
            var container = new SequentialContainer();
            root.Add(container);
            var first = new PluginItem();
            var second = new PluginItem();
            container.Add(first);
            container.Add(second);
            using var history = new SequenceEditHistory(root);
            first.MoveUpCommand.Execute(null);
            // At the boundary this moves into the root before its parent container.
            history.Undo();
            container.Items.Should().Equal(first, second);
            first.MoveDownCommand.Execute(null);
            container.Items.Should().Equal(second, first);
            history.Undo().Should().BeTrue();
            container.Items.Should().Equal(first, second);
            history.Redo().Should().BeTrue();
            container.Items.Should().Equal(second, first);
            first.AddCloneToParentCommand.Execute(null);
            ISequenceItem clone = container.Items.Last();
            history.Undo().Should().BeTrue();
            history.Redo().Should().BeTrue();
            container.Items.Last().Should().BeSameAs(clone);
            clone.DetachCommand.Execute(null);
            history.Undo().Should().BeTrue();
            container.Items.Last().Should().BeSameAs(clone);
            clone.Parent.Should().BeSameAs(container);
        }

        [Test]
        public void ConditionsAndTriggers_AddRemoveAndReorder() {
            var root = new SequenceRootContainer();
            var condition = new LoopCondition();
            var other = new LoopCondition();
            root.Add(condition);
            root.Add(other);
            using var history = new SequenceEditHistory(root);
            condition.DetachCommand.Execute(null);
            history.Undo().Should().BeTrue();
            root.Conditions.Should().Equal(condition, other);
            condition.Parent.Should().BeSameAs(root);
            history.Redo().Should().BeTrue();
            root.Conditions.Should().Equal(other);
        }

        [Test]
        public void EnableDisable_BothDirections_DoNotRecordRuntimeStatus() {
            var root = new SequenceRootContainer();
            var item = new PluginItem { Status = SequenceEntityStatus.FINISHED };
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            item.DisableEnableCommand.Execute(null);
            item.Status.Should().Be(SequenceEntityStatus.DISABLED);
            history.Undo().Should().BeTrue();
            item.Status.Should().Be(SequenceEntityStatus.CREATED);
            item.Status = SequenceEntityStatus.RUNNING;
            history.Position.Should().Be(0);
            history.Redo().Should().BeTrue();
            item.Status.Should().Be(SequenceEntityStatus.DISABLED);
            item.DisableEnableCommand.Execute(null);
            history.Undo().Should().BeTrue();
            item.Status.Should().Be(SequenceEntityStatus.DISABLED);
        }

        [TestCase(23, 59, 59.9, 0, 0, 0.1)]
        [TestCase(0, 0, 0.1, 23, 59, 59.9)]
        public void Coordinates_RestoreWholeValueAcrossBoundaries(int h1, int m1, double s1, int h2, int m2, double s2) {
            var item = new PluginItem();
            item.Coordinates.RAHours = h1;
            item.Coordinates.RAMinutes = m1;
            item.Coordinates.RASeconds = s1;
            var root = new SequenceRootContainer();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding("Coordinates.RASeconds"));
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            Coordinates before = item.Coordinates.Coordinates.Clone();
            item.Coordinates.RAHours = h2;
            item.Coordinates.RAMinutes = m2;
            item.Coordinates.RASeconds = s2;
            item.Coordinates.NegativeDec = true;
            item.Coordinates.DecDegrees = 90;
            Coordinates after = item.Coordinates.Coordinates.Clone();
            history.RecordApplied(capture!.Complete());
            history.Undo().Should().BeTrue();
            (item.Coordinates.Coordinates.RA, item.Coordinates.Coordinates.Dec, item.Coordinates.Coordinates.Epoch).Should().Be((before.RA, before.Dec, before.Epoch));
            history.Redo().Should().BeTrue();
            (item.Coordinates.Coordinates.RA, item.Coordinates.Coordinates.Dec, item.Coordinates.Coordinates.Epoch).Should().Be((after.RA, after.Dec, after.Epoch));
        }

        [Test]
        public void PropertyReplay_ResolvesReplacementExpression() {
            var item = new PluginItem();
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding("Formula.Definition"));
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            item.Formula.Definition = "5";
            ISequenceEdit edit = capture!.Complete();
            item.Formula = new Expression("5", null);
            edit.Undo();
            item.Formula.Definition.Should().Be("1");
        }

        [Test]
        public void EditorBehavior_GroupsTypingAndFlushesBeforeUndo() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var panel = new StackPanel { DataContext = item };
            var box = new TextBox();
            panel.Children.Add(box);
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(PluginItem.Text)) { Mode = BindingMode.TwoWay });
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(panel);
            try {
                Type(box, "a");
                Type(box, "after");
                history.CanUndo.Should().BeTrue();
                history.Undo().Should().BeTrue();
                item.Text.Should().Be("before");
                history.Entries.Should().HaveCount(2);
                history.Redo().Should().BeTrue();
                item.Text.Should().Be("after");
            } finally { behavior.Detach(); }
        }

        [Test]
        public void BackgroundChangesAndRuntimeControls_DoNotRecord() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var panel = new StackPanel { DataContext = item };
            var box = new TextBox();
            panel.Children.Add(box);
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(PluginItem.Text)) { Mode = BindingMode.TwoWay });
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(panel);
            try {
                box.Text = "background";
                box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                behavior.Commit();
                history.CanUndo.Should().BeFalse();
                SequenceEditContext.SetIsRecordingEnabled(box, false);
                Type(box, "runtime");
                behavior.Commit();
                history.CanUndo.Should().BeFalse();
            } finally { behavior.Detach(); }
        }

        private static void Type(TextBox box, string text) {
            box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, text)) {
                RoutedEvent = TextCompositionManager.PreviewTextInputEvent
            });
            box.Text = text;
        }

        [Test]
        public void LiveInstruction_KeepsRunningAcrossPropertyAndPlacementUndoRedo() {
            var root = new SequenceRootContainer();
            var first = new SequentialContainer();
            var second = new SequentialContainer();
            root.Add(first);
            root.Add(second);
            using var started = new ManualResetEventSlim();
            var item = new PluginItem { Completion = new(TaskCreationOptions.RunContinuationsAsynchronously), Started = started };
            first.Add(item);
            using var history = new SequenceEditHistory(root);
            Task running = Task.Run(() => item.Run(new Progress<NINA.Core.Model.ApplicationStatus>(), CancellationToken.None));
            try {
                started.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                Record(history, item, 10);
                history.CaptureStructure("Move", () => second.Add(item));
                history.Undo().Should().BeTrue();
                history.Undo().Should().BeTrue();
                history.Redo().Should().BeTrue();
                history.Redo().Should().BeTrue();
                running.IsCompleted.Should().BeFalse();
                item.Status.Should().Be(SequenceEntityStatus.RUNNING);
                item.Executions.Should().Be(1);
                item.Parent.Should().BeSameAs(second);
                root.GetCurrentRunningItems().Should().Contain(item);
            } finally {
                item.Completion.TrySetResult(true);
                running.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            }
        }

        [Test]
        public void DropCommands_GroupCrossContainerMovesAndIgnoreRejectedDuplicates() {
            var root = new SequenceRootContainer();
            var first = new SequentialContainer();
            var second = new SequentialContainer();
            root.Add(first);
            root.Add(second);
            var item = new PluginItem();
            first.Add(item);
            using var history = new SequenceEditHistory(root);
            second.DropIntoCommand.Execute(new DropIntoParameters(item, second, DropTargetEnum.Center));
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            first.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
            history.Redo().Should().BeTrue();
            second.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
            var condition = new LoopCondition { Name = "Loop" };
            second.DropIntoConditionsCommand.Execute(new DropIntoParameters(condition));
            int position = history.Position;
            second.DropIntoConditionsCommand.Execute(new DropIntoParameters(condition));
            history.Position.Should().Be(position);
            second.Conditions.Should().HaveCount(1);
        }

        [Test]
        public void TriggerRunnerCommands_UseTheOwningHistory() {
            var root = new SequenceRootContainer();
            var trigger = new UnknownSequenceTrigger("Test trigger");
            var item = new PluginItem();
            trigger.TriggerRunner.Add(item);
            root.Add(trigger);
            using var history = new SequenceEditHistory(root);
            item.DetachCommand.Execute(null);
            history.Undo().Should().BeTrue();
            trigger.TriggerRunner.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
            trigger.DetachCommand.Execute(null);
            history.Undo().Should().BeTrue();
            root.Triggers.Should().ContainSingle().Which.Should().BeSameAs(trigger);
            trigger.TriggerRunner.Items.Single().Should().BeSameAs(item);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LinkedTemplate_EditSessionIsIsolatedAndClosingDiscardsIt(bool save) {
            var root = new SequenceRootContainer();
            var resolver = new TemplateLinkResolver();
            var reference = new TemplateReference { SourceKind = TemplateReferenceSourceKind.User, RelativePath = "Test.template.json", DisplayName = "Test" };
            using var profile = new NINA.Profile.Profile();
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            var template = new SequentialContainer { Name = "Test" };
            template.Add(new PluginItem());
            var wrapped = new TemplatedSequenceContainer(profiles.Object, "LblTemplate_UserTemplates", template, reference, resolver);
            int saves = 0;
            resolver.UpdateTemplates(new[] { wrapped }, true, (_, _, _) => { saves++; return Task.CompletedTask; });
            var linked = new LinkedTemplateContainer(resolver) { TemplateReference = reference };
            root.Add(linked);
            linked.TryResolveTemplate().Should().BeTrue();
            using var history = new SequenceEditHistory(root);
            linked.BeginEditTemplateCommand.Execute(null);
            SequenceEditHistory nested = history.ActiveHistory;
            nested.Should().NotBeSameAs(history);
            var materialized = (SequenceContainer)linked.Items.Single();
            materialized.DropIntoCommand.Execute(new DropIntoParameters(new PluginItem(), materialized, DropTargetEnum.Center));
            nested.CanUndo.Should().BeTrue();
            history.Position.Should().Be(0);
            nested.Undo().Should().BeTrue();
            materialized.Items.Should().HaveCount(1);
            if (save) linked.SaveTemplateCommand.Execute(null);
            else linked.CancelEditTemplateCommand.Execute(null);
            history.ActiveHistory.Should().BeSameAs(history);
            nested.CanUndo.Should().BeFalse();
            saves.Should().Be(save ? 1 : 0);
            resolver.UpdateTemplates(new[] { wrapped }, true, null);
            linked.TryResolveTemplate().Should().BeTrue();
            history.Position.Should().Be(0);
        }

        [Test]
        public void LockedHistory_DoesNotRecordOrReplay() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            Record(history, item, 1);
            history.IsEnabled = false;
            history.Undo().Should().BeFalse();
            history.RecordApplied(new PropertySequenceEdit<double>("Ignored", () => item.Setting, v => item.Setting = v, 0, 1));
            history.Position.Should().Be(1);
            history.IsEnabled = true;
            history.Undo().Should().BeTrue();
        }

        [Test]
        public void HostCommandWrapper_CapturesUnmodifiedPluginOverrideOnce() {
            var root = new SequenceRootContainer();
            var item = new CustomCommandItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var button = new Button { DataContext = item };
            SequenceEditContext.SetOperation(button, SequenceEditOperation.Delete);
            button.SetBinding(SequenceEditContext.CommandProperty, new Binding(nameof(item.DetachCommand)));
            button.Command.Execute(null);
            root.Items.Should().BeEmpty();
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            root.Items.Single().Should().BeSameAs(item);
            SequenceEditContext.SetOperation(button, SequenceEditOperation.Toggle);
            button.SetBinding(SequenceEditContext.CommandProperty, new Binding(nameof(item.DisableEnableCommand)));
            button.Command.Execute(null);
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            item.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public void EditorBehavior_ImmediateBindingsAcceptNativeTextUndoAndIgnoreInvalidNumbers() {
            var root = new SequenceRootContainer();
            var item = new PluginItem { Setting = 5 };
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var panel = new StackPanel { DataContext = item };
            var box = new TextBox();
            panel.Children.Add(box);
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(PluginItem.Setting)) {
                Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, ValidatesOnExceptions = true
            });
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(panel);
            try {
                Type(box, "invalid");
                behavior.Commit();
                item.Setting.Should().Be(5);
                history.Position.Should().Be(0);
                Type(box, "10");
                box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                behavior.Commit();
                history.Undo().Should().BeTrue();
                box.Text.Should().Be("5");
                history.Redo().Should().BeTrue();
                box.Text.Should().Be("10");
            } finally { behavior.Detach(); }
        }

        [Test]
        public void Eviction_ReleasesRemovedEntities() {
            var root = new SequenceRootContainer();
            using var history = new SequenceEditHistory(root);
            WeakReference removed = AddAndDelete(root, history);
            var remaining = new PluginItem();
            root.Add(remaining);
            for (int i = 1; i <= 100; i++) Record(history, remaining, i);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            removed.IsAlive.Should().BeFalse();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static WeakReference AddAndDelete(SequenceRootContainer root, SequenceEditHistory history) {
            var item = new PluginItem();
            root.Add(item);
            item.DetachCommand.Execute(null);
            return new WeakReference(item);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ThousandEntitySequence_FieldGesturesDoNotTraverseTree(bool triggerOwned) {
            var root = new CountingRoot();
            for (int i = 0; i < 999; i++) root.Add(new PluginItem());
            var item = new PluginItem();
            if (triggerOwned) {
                var trigger = new UnknownSequenceTrigger();
                trigger.TriggerRunner.Add(item);
                root.Add(trigger);
            } else root.Add(item);
            using var history = new SequenceEditHistory(root);
            root.Reads = 0; // Initial ownership indexing is separate from ordinary editing.
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(item.Setting)) { Mode = BindingMode.TwoWay });
            var panel = new StackPanel();
            panel.Children.Add(box);
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(panel);
            var timer = Stopwatch.StartNew();
            try {
                for (int i = 1; i <= 100; i++) {
                    box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, i.ToString())) {
                        RoutedEvent = TextCompositionManager.PreviewTextInputEvent
                    });
                    box.Text = i.ToString();
                    box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, box, panel) {
                        RoutedEvent = Keyboard.LostKeyboardFocusEvent
                    });
                    CoreEditorTestScope.Drain();
                }
                history.Position.Should().Be(100);
                history.MoveTo(0);
                history.Position.Should().Be(0);
                item.Setting.Should().Be(0);
                history.MoveTo(100);
                history.Position.Should().Be(100);
                item.Setting.Should().Be(100);
                item.Parent.Items.Should().ContainSingle(x => ReferenceEquals(x, item));
                root.Reads.Should().Be(0);
                timer.Stop();
                TestContext.Out.WriteLine($"100 UI edits + 100 undo + 100 redo with 1000 entities (trigger-owned: {triggerOwned}): {timer.ElapsedMilliseconds} ms");
            } finally { behavior.Detach(); }
        }

        [Test]
        public void BoundPluginProperty_UndoRedoWhileRunning_PreservesIdentityAndProgress() {
            var root = new SequenceRootContainer();
            var item = new PluginItem { Setting = 1, Status = SequenceEntityStatus.RUNNING };
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(PluginItem.Setting)) { Mode = BindingMode.TwoWay });
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            capture.Should().NotBeNull();
            box.Text = "2";
            box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            history.RecordApplied(capture!.Complete());
            item.Progress = 42;

            history.Undo().Should().BeTrue();
            item.Setting.Should().Be(1);
            history.Redo().Should().BeTrue();
            item.Setting.Should().Be(2);
            root.Items.Single().Should().BeSameAs(item);
            item.Status.Should().Be(SequenceEntityStatus.RUNNING);
            item.Progress.Should().Be(42);
        }

        [Test]
        public void ExpressionBinding_RecordsDefinitionWithoutRewindingEvaluation() {
            var root = new SequenceRootContainer();
            var item = new PluginItem { Status = SequenceEntityStatus.RUNNING };
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding("Formula.Definition") { Mode = BindingMode.TwoWay });
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            box.Text = "3";
            box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            history.RecordApplied(capture!.Complete());
            history.Undo().Should().BeTrue();
            item.Formula.Definition.Should().Be("1");
            history.Redo().Should().BeTrue();
            item.Formula.Definition.Should().Be("3");
            item.Status.Should().Be(SequenceEntityStatus.RUNNING);
        }

        [Test]
        public void CrossContainerMove_UndoRedo_RetainsRunningEntity() {
            var root = new SequenceRootContainer();
            var first = new SequentialContainer();
            var second = new SequentialContainer();
            root.Add(first);
            root.Add(second);
            var item = new PluginItem { Status = SequenceEntityStatus.RUNNING };
            first.Add(item);
            using var history = new SequenceEditHistory(root);
            history.CaptureStructure("Move", () => second.Add(item));
            item.Progress = 7;
            history.Undo().Should().BeTrue();
            first.Items.Single().Should().BeSameAs(item);
            item.Parent.Should().BeSameAs(first);
            history.Redo().Should().BeTrue();
            second.Items.Single().Should().BeSameAs(item);
            item.Parent.Should().BeSameAs(second);
            item.Progress.Should().Be(7);
            item.Status.Should().Be(SequenceEntityStatus.RUNNING);
        }
    }
}