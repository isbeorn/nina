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
using Newtonsoft.Json;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.Container;
using NINA.Sequencer.Editing;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NUnit.Framework;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class PluginEditProviderTest {
        private sealed class PluginTrigger : SequenceTrigger, ISequenceTriggerEditor {
            public ISequenceContainer Actions { get; } = new SequentialContainer();
            public IEnumerable<ISequenceContainer> GetAdditionalEditorContainers() { yield return Actions; }
            public override object Clone() => new PluginTrigger();
            public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) => false;
            public override Task Execute(ISequenceContainer context, IProgress<NINA.Core.Model.ApplicationStatus> progress, CancellationToken token) =>
                throw new InvalidOperationException("Editing must never execute a trigger.");
        }

        // Uses only public extension contracts, with no built-in coordinate or condition types.
        public class PluginItem : SequenceEditHistoryTest.PluginItem, ISequenceCustomPropertyEditProvider, ISequenceAttachmentStateProvider {
            [JsonProperty]
            public CoupledSettings Settings { get; private set; } = new();
            public int RuntimeValue { get; set; }
            public bool FailNextRestore { get; set; }

            public override void AfterParentChanged() {
                base.AfterParentChanged();
                Settings = new CoupledSettings { Primary = Settings.Primary };
            }

            public bool TryCapturePropertyState(object source, string propertyName, out ISequenceEditSnapshot? snapshot) {
                snapshot = null;
                if (ReferenceEquals(source, this) && propertyName == nameof(RuntimeValue)) return true;
                if (!ReferenceEquals(source, Settings)) return false;
                snapshot = new Snapshot(this);
                return true;
            }
            public ISequenceEditSnapshot CaptureAttachmentState() => new Snapshot(this);

            private sealed class Snapshot : ISequenceEditSnapshot {
                private readonly PluginItem owner;
                private readonly int primary;
                private readonly int secondary;
                public Snapshot(PluginItem owner) {
                    this.owner = owner;
                    primary = owner.Settings.Primary;
                    secondary = owner.Settings.Secondary;
                }
                public string Description => $"{primary}, {secondary}";
                public bool IsCurrent => owner.Settings.Primary == primary && owner.Settings.Secondary == secondary;
                public void Restore() {
                    owner.Settings.Primary = primary;
                    if (owner.FailNextRestore) {
                        owner.FailNextRestore = false;
                        throw new InvalidOperationException("Plugin setter failed after its first update.");
                    }
                    owner.Settings.Secondary = secondary;
                }
            }
        }
        public class CoupledSettings {
            private int primary = 2;
            public int Primary {
                get => primary;
                set { primary = value; Secondary = value * 10; }
            }
            public int Secondary { get; set; } = 7;
        }

        [TestCase(UpdateSourceTrigger.PropertyChanged)]
        [TestCase(UpdateSourceTrigger.LostFocus)]
        public void CustomCoupledBinding_UndoRedoRestoresBothValues(UpdateSourceTrigger trigger) {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            item.Settings.Secondary = 7;
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            var binding = new Binding("Settings.Primary") { UpdateSourceTrigger = trigger };
            box.SetBinding(TextBox.TextProperty, binding);
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(box);
            try {
                Type(box, "5");
                behavior.Commit();
                history.Position.Should().Be(1);
                item.RuntimeValue = 123;
                history.Undo().Should().BeTrue();
                item.Settings.Primary.Should().Be(2);
                item.Settings.Secondary.Should().Be(7);
                box.Text.Should().Be("2");
                history.Redo().Should().BeTrue();
                item.Settings.Primary.Should().Be(5);
                item.Settings.Secondary.Should().Be(50);
                item.RuntimeValue.Should().Be(123);
                box.Text.Should().Be("5");
                BindingOperations.GetBinding(box, TextBox.TextProperty).Should().BeSameAs(binding);
            } finally { behavior.Detach(); }
        }

        [Test]
        public void AttachmentProvider_ReplaysAfterParentHooksAndResolvesReplacementSettings() {
            var root = new SequenceRootContainer();
            var first = new SequentialContainer();
            var second = new SequentialContainer();
            root.Add(first);
            root.Add(second);
            var item = new PluginItem();
            first.Add(item);
            item.Settings.Secondary = 7;
            using var history = new SequenceEditHistory(root);
            history.CaptureStructure("Move plugin item", () => second.Add(item));
            item.Settings.Secondary.Should().Be(20);
            for (int i = 0; i < 3; i++) {
                history.Undo().Should().BeTrue();
                item.Parent.Should().BeSameAs(first);
                item.Settings.Secondary.Should().Be(7);
                history.Redo().Should().BeTrue();
                item.Parent.Should().BeSameAs(second);
                item.Settings.Secondary.Should().Be(20);
            }
        }

        [Test]
        public void Provider_CanExcludeRuntimeFields() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(item.RuntimeValue)));
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(box);
            try {
                Type(box, "5");
                behavior.Commit();
                item.RuntimeValue.Should().Be(5);
                history.Position.Should().Be(0);
            } finally { behavior.Detach(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SnapshotConflict_DoesNotMoveCursorOrOverwriteExternalValue(bool redo) {
            WithEdit((item, history, box) => {
                if (redo) history.Undo().Should().BeTrue();
                int position = history.Position;
                item.Settings.Secondary = 99;
                (redo ? history.Redo() : history.Undo()).Should().BeFalse();
                history.Position.Should().Be(position);
                item.Settings.Secondary.Should().Be(99);
                item.Settings.Primary.Should().Be(redo ? 2 : 5);
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedSnapshotRestore_CompensatesPartialSetterAndPreservesHistory(bool redo) {
            WithEdit((item, history, box) => {
                if (redo) history.Undo().Should().BeTrue();
                item.FailNextRestore = true;
                (redo ? history.Redo() : history.Undo()).Should().BeFalse();
                item.Settings.Primary.Should().Be(redo ? 2 : 5);
                item.Settings.Secondary.Should().Be(redo ? 7 : 50);
                box.Text.Should().Be(redo ? "2" : "5");
                history.CanUndo.Should().Be(!redo);
                history.CanRedo.Should().Be(redo);
            });
        }

        [TestCase(7, 1)]
        [TestCase(20, 0)]
        public void SamePrimaryValue_RecordsOnlyEffectiveCoupledChanges(int secondary, int expectedEdits) {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            item.Settings.Secondary = secondary;
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding("Settings.Primary"));
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            box.SetCurrentValue(TextBox.TextProperty, "2");
            box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            history.RecordApplied(capture!.Complete());
            history.Position.Should().Be(expectedEdits);
            if (expectedEdits > 0) {
                history.Undo().Should().BeTrue();
                item.Settings.Secondary.Should().Be(7);
                history.Redo().Should().BeTrue();
                item.Settings.Secondary.Should().Be(20);
            }
        }

        [Test]
        public void UnhandledProperty_UsesOrdinaryBindingCapture() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(item.Text)));
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            item.Text = "new text";
            history.RecordApplied(capture!.Complete());
            history.Undo().Should().BeTrue();
            item.Text.Should().Be("before");
            history.Redo().Should().BeTrue();
            item.Text.Should().Be("new text");
        }

        private static void WithEdit(Action<PluginItem, SequenceEditHistory, TextBox> verify) {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            item.Settings.Secondary = 7;
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding("Settings.Primary"));
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(box);
            try {
                Type(box, "5");
                behavior.Commit();
                history.Position.Should().Be(1);
                verify(item, history, box);
            } finally { behavior.Detach(); }
        }

        [Test]
        public void PluginTrigger_AdditionalActionContainerSupportsFieldAndStructuralEdits() {
            var root = new SequenceRootContainer();
            var trigger = new PluginTrigger();
            var item = new PluginItem();
            root.Add(trigger);
            trigger.Actions.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(item.Text)));
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(box);
            try {
                Type(box, "changed");
                behavior.Commit();
                history.Position.Should().Be(1);
                history.Undo().Should().BeTrue();
                item.Text.Should().Be("before");
                history.Redo().Should().BeTrue();
                item.Text.Should().Be("changed");
                item.DetachCommand.Execute(null);
                trigger.Actions.Items.Should().BeEmpty();
                history.Undo().Should().BeTrue();
                trigger.Actions.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
                history.Redo().Should().BeTrue();
                trigger.Actions.Items.Should().BeEmpty();
            } finally { behavior.Detach(); }
        }

        private static void Type(TextBox box, string value) {
            box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, value)) {
                RoutedEvent = TextCompositionManager.PreviewTextInputEvent
            });
            box.SetCurrentValue(TextBox.TextProperty, value);
        }
    }
}