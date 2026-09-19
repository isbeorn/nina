#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using NINA.Sequencer;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Editing;
using NUnit.Framework;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class PluginEditorStructureTest {
        public sealed class SlotOwner : SequenceEditHistoryTest.PluginItem, ISequenceEditorChildProvider {
            public SequenceEditHistoryTest.PluginItem? Child { get; set; }
            public ICommand ReplaceSelection => new RelayCommand<DropIntoParameters>(drop => Child = (SequenceEditHistoryTest.PluginItem)drop!.Source);
            public IEnumerable<ISequenceEditorChildList> GetEditorChildLists() { yield return new Slot(this); }
            private sealed class Slot(SlotOwner owner) : ISequenceEditorChildList {
                public string Name => nameof(Child);
                public bool IsReadOnly => false;
                public IReadOnlyList<ISequenceEntity> Read() => owner.Child == null ? Array.Empty<ISequenceEntity>() : new[] { owner.Child };
                public void Insert(int index, ISequenceEntity entity) => owner.Child = (SequenceEditHistoryTest.PluginItem)entity;
                public void Remove(ISequenceEntity entity) { if (ReferenceEquals(owner.Child, entity)) owner.Child = null; }
                public void Move(ISequenceEntity entity, int index) { }
            }
        }

        private sealed class SessionBoundary : SequentialContainer, ISequenceEditSessionBoundary {
            private bool editing;
            public bool IsEditing {
                get => editing;
                set { editing = value; RaisePropertyChanged(); }
            }
        }

        [Test]
        public void PluginChildSlot_HostDropAndFieldGesturesUseTheSameOwnership() {
            var root = new SequenceRootContainer();
            var before = new SequenceEditHistoryTest.PluginItem();
            var after = new SequenceEditHistoryTest.PluginItem();
            var owner = new SlotOwner { Child = before };
            root.Add(owner);
            using var history = new SequenceEditHistory(root);
            var host = new StackPanel { DataContext = owner };
            var drop = new DropIntoBehavior { OnDropCommand = nameof(owner.ReplaceSelection), RecordSequenceStructure = true };
            drop.Attach(host);
            try { drop.ExecuteDropInto(new DropIntoParameters(after)); }
            finally { drop.Detach(); }
            owner.Child.Should().BeSameAs(after);
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            owner.Child.Should().BeSameAs(before);
            history.Redo().Should().BeTrue();
            owner.Child.Should().BeSameAs(after);

            var box = new TextBox { DataContext = after };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(after.Setting)));
            host.Children.Add(box);
            SequenceEditContext.SetHistory(host, history);
            SequenceEditContext.GetHistory(box).Should().BeSameAs(history);
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(host);
            try {
                box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, "4")) {
                    RoutedEvent = TextCompositionManager.PreviewTextInputEvent
                });
                box.Text = "4";
                box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, box, host) { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
                CoreEditorTestScope.Drain();
                history.Position.Should().Be(2);
                history.Undo().Should().BeTrue();
                after.Setting.Should().Be(0);
                history.Redo().Should().BeTrue();
                after.Setting.Should().Be(4);
            } finally { behavior.Detach(); }
        }

        [Test]
        public void PluginSessionBoundary_SeparatesInstanceAndContentsAndDisposesOnExit() {
            var root = new SequenceRootContainer();
            var boundary = new SessionBoundary();
            var child = new SequenceEditHistoryTest.PluginItem();
            boundary.Add(child);
            root.Add(boundary);
            using var history = new SequenceEditHistory(root);
            var editor = new Button { DataContext = child };
            SequenceEditContext.SetHistory(editor, history);
            SequenceEditContext.GetHistory(editor).Should().BeNull("preview contents are not editable");
            boundary.IsEditing = true;
            var contents = (SequenceEditHistory)SequenceEditContext.GetHistory(editor);
            contents.Should().NotBeSameAs(history);
            child.DisableEnableCommand.Execute(null);
            contents.Position.Should().Be(1);
            history.Position.Should().Be(0);
            contents.Undo().Should().BeTrue();
            contents.Redo().Should().BeTrue();
            boundary.IsEditing = false;
            contents.IsEnabled.Should().BeFalse();
            history.ActiveHistory.Should().BeSameAs(history);
            boundary.DetachCommand.Execute(null);
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            root.Items.Should().ContainSingle().Which.Should().BeSameAs(boundary);
        }
    }
}