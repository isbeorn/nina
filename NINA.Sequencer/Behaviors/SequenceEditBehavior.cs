#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Xaml.Behaviors;
using NINA.Sequencer.Editing;
using static NINA.Sequencer.Editing.SequenceEditBindingResolver;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.Sequencer.Behaviors {
    /// <summary>
    /// Attributes accepted binding changes to an explicit editor gesture. Does not subscribe to
    /// model PropertyChanged, replace bindings or observe background execution updates.
    /// </summary>
    public sealed class SequenceEditBehavior : Behavior<FrameworkElement> {
        public static readonly DependencyProperty HistoryProperty = DependencyProperty.Register(
            nameof(History), typeof(ISequenceEditHistory), typeof(SequenceEditBehavior), new PropertyMetadata(null, HistoryChanged));
        public ISequenceEditHistory History {
            get => (ISequenceEditHistory)GetValue(HistoryProperty);
            set => SetValue(HistoryProperty, value);
        }
        private PendingSequenceEdit pending;
        private FrameworkElement Editor => pending?.Binding.Editor;

        private static void HistoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var behavior = (SequenceEditBehavior)d;
            behavior.Cancel();
            if (e.OldValue is SequenceEditHistory old) old.FlushRequested -= behavior.Commit;
            if (e.NewValue is SequenceEditHistory current) current.FlushRequested += behavior.Commit;
        }
        protected override void OnAttached() {
            base.OnAttached();
            if (History is SequenceEditHistory history) {
                history.FlushRequested -= Commit;
                history.FlushRequested += Commit;
            }
            AssociatedObject.AddHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(GotFocus), true);
            AssociatedObject.AddHandler(Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(LostFocus), true);
            AssociatedObject.AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(MouseDown), true);
            AssociatedObject.AddHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler(MouseUp), true);
            AssociatedObject.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(KeyDown), true);
            AssociatedObject.AddHandler(Keyboard.PreviewKeyUpEvent, new KeyEventHandler(KeyUp), true);
            AssociatedObject.AddHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(MouseWheel), true);
            AssociatedObject.AddHandler(TextCompositionManager.PreviewTextInputEvent, new TextCompositionEventHandler(TextInput), true);
            AssociatedObject.AddHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(EditingCommand), true);
            AssociatedObject.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler(SelectionChanged), true);
            AssociatedObject.Unloaded += Unloaded;
        }
        protected override void OnDetaching() {
            Commit();
            if (History is SequenceEditHistory history) history.FlushRequested -= Commit;
            AssociatedObject.RemoveHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(GotFocus));
            AssociatedObject.RemoveHandler(Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(LostFocus));
            AssociatedObject.RemoveHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(MouseDown));
            AssociatedObject.RemoveHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler(MouseUp));
            AssociatedObject.RemoveHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(KeyDown));
            AssociatedObject.RemoveHandler(Keyboard.PreviewKeyUpEvent, new KeyEventHandler(KeyUp));
            AssociatedObject.RemoveHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(MouseWheel));
            AssociatedObject.RemoveHandler(TextCompositionManager.PreviewTextInputEvent, new TextCompositionEventHandler(TextInput));
            AssociatedObject.RemoveHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(EditingCommand));
            AssociatedObject.RemoveHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler(SelectionChanged));
            AssociatedObject.Unloaded -= Unloaded;
            base.OnDetaching();
        }
        private void Unloaded(object sender, RoutedEventArgs e) => Commit();
        private void GotFocus(object sender, KeyboardFocusChangedEventArgs e) => Begin(e.NewFocus as DependencyObject);
        private void LostFocus(object sender, KeyboardFocusChangedEventArgs e) => ScheduleCommit();
        private void MouseDown(object sender, MouseButtonEventArgs e) {
            if (Editor != null && !IsWithin(e.OriginalSource as DependencyObject, Editor)) Commit();
            Begin(e.OriginalSource as DependencyObject);
            if (Editor is not TextBoxBase && !IsTextEditor(e.OriginalSource as DependencyObject)) MarkInteracted();
        }
        private void MouseUp(object sender, MouseButtonEventArgs e) {
            if (Editor != null && Editor is not TextBoxBase && Editor is not ComboBox { IsEditable: true }) ScheduleCommit();
        }
        private void KeyDown(object sender, KeyEventArgs e) {
            if (History is not SequenceEditHistory history) return;
            bool textEditor = IsTextEditor(e.OriginalSource as DependencyObject);
            if (!textEditor && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && (e.Key == Key.Z || e.Key == Key.Y)) {
                Commit();
                SequenceEditHistory active = history.ActiveHistory;
                if (e.Key == Key.Y || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) active.Redo(); else active.Undo();
                e.Handled = true;
                return;
            }
            Begin(e.OriginalSource as DependencyObject);
            if (!textEditor || e.Key is Key.Back or Key.Delete || IsComboSelectionKey(e.Key)) MarkInteracted();
            if (e.Key == Key.Enter) ScheduleCommit();
        }
        private void KeyUp(object sender, KeyEventArgs e) {
            if (Editor != null && Editor is not TextBoxBase
                && (!IsTextEditor(e.OriginalSource as DependencyObject) || IsComboSelectionKey(e.Key))) ScheduleCommit();
        }
        private bool IsComboSelectionKey(Key key) => Editor is ComboBox && key is Key.Up or Key.Down or Key.PageUp or Key.PageDown;
        private static bool IsTextEditor(DependencyObject source) => Ancestors(source).Any(x => x is TextBoxBase);
        private void MouseWheel(object sender, MouseWheelEventArgs e) {
            Begin(e.OriginalSource as DependencyObject);
            MarkInteracted();
            pending?.CommitAfterWheelGesture();
        }
        private void TextInput(object sender, TextCompositionEventArgs e) {
            Begin(e.OriginalSource as DependencyObject);
            MarkInteracted();
        }
        private void EditingCommand(object sender, ExecutedRoutedEventArgs e) {
            if (e.Command == ApplicationCommands.Paste || e.Command == ApplicationCommands.Cut
                || e.Command == ApplicationCommands.Undo || e.Command == ApplicationCommands.Redo) {
                Begin(e.OriginalSource as DependencyObject);
                MarkInteracted();
            }
        }
        private void MarkInteracted() => pending?.MarkInteracted();
        private void SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Editable combo boxes also select matches while typing. Their popup and key
            // handlers commit selections; typed text stays grouped until an explicit commit.
            if (Editor is Selector && Editor is not ComboBox { IsEditable: true }
                && IsWithin(e.OriginalSource as DependencyObject, Editor)) ScheduleCommit();
        }
        private void ScheduleCommit() {
            PendingSequenceEdit gesture = pending;
            if (gesture == null) return;
            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                if (ReferenceEquals(pending, gesture) && !gesture.HasOpenPopup) Commit();
            }));
        }

        private void Begin(DependencyObject source) {
            if (pending?.IsCommitting == true || History is not SequenceEditHistory history || !history.IsRecording || source == null) return;
            if (!SequenceEditContext.GetIsRecordingEnabled(source)) return;
            if (pending?.Interacted == true && IsWithin(source, Editor)) return;
            Commit();
            SequenceEditBinding binding = Resolve(source, AssociatedObject, history);
            if (binding != null) pending = new PendingSequenceEdit(binding, ScheduleCommit);
        }

        internal void Commit() {
            if (pending == null || pending.IsCommitting) return;
            try { pending.Commit(); }
            finally { Cancel(); }
        }
        private void Cancel() {
            pending?.Dispose();
            pending = null;
        }
    }
}