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
using NINA.Core.Utility;
using NINA.Sequencer.Editing;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
        private FrameworkElement editor;
        private BindingExpressionBase binding;
        private BindingExpressionBase inputBinding;
        private SequenceEditHistory session;
        private SequencePropertyCapture capture;
        private bool completing;
        private bool interacted;
        private int generation;
        private DispatcherTimer wheelTimer;
        private static readonly HashSet<string> EditableProperties = new() {
            "Text", "Value", "IsChecked", "SelectedItem", "SelectedValue", "SelectedIndex", "SelectedDate", "SelectedTime"
        };

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
            if (editor != null && !IsWithin(e.OriginalSource as DependencyObject, editor)) Commit();
            Begin(e.OriginalSource as DependencyObject);
            if (editor is not TextBoxBase && !IsTextEditor(e.OriginalSource as DependencyObject)) MarkInteracted();
        }
        private void MouseUp(object sender, MouseButtonEventArgs e) {
            if (editor != null && editor is not TextBoxBase && editor is not ComboBox { IsEditable: true }) ScheduleCommit();
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
            if (editor != null && editor is not TextBoxBase
                && (!IsTextEditor(e.OriginalSource as DependencyObject) || IsComboSelectionKey(e.Key))) ScheduleCommit();
        }
        private bool IsComboSelectionKey(Key key) => editor is ComboBox && key is Key.Up or Key.Down or Key.PageUp or Key.PageDown;
        private static bool IsTextEditor(DependencyObject source) => Ancestors(source).Any(x => x is TextBoxBase);
        private void MouseWheel(object sender, MouseWheelEventArgs e) {
            Begin(e.OriginalSource as DependencyObject);
            MarkInteracted();
            wheelTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Background,
                (_, _) => Commit(), AssociatedObject.Dispatcher);
            wheelTimer.Stop();
            wheelTimer.Start();
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
        private void MarkInteracted() {
            if (capture == null) return;
            interacted = true;
            session.HasPendingEdit = true;
        }
        private void SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Editable combo boxes also select matches while typing. Their popup and key
            // handlers commit selections; typed text stays grouped until an explicit commit.
            if (editor is Selector && editor is not ComboBox { IsEditable: true }
                && IsWithin(e.OriginalSource as DependencyObject, editor)) ScheduleCommit();
        }
        private void ScheduleCommit() {
            int token = generation;
            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                if (token == generation && editor is not ComboBox { IsDropDownOpen: true }
                    && editor is not DatePicker { IsDropDownOpen: true }) Commit();
            }));
        }

        private void Begin(DependencyObject source) {
            if (completing || History is not SequenceEditHistory history || !history.IsRecording || source == null) return;
            if (!SequenceEditContext.GetIsRecordingEnabled(source)) return;
            if (editor != null && interacted && IsWithin(source, editor)) return;
            Commit();
            BindingExpressionBase pendingInput = null;
            ISequenceEntity visualOwner = Ancestors(source).OfType<FrameworkElement>().Select(x => x.DataContext).OfType<ISequenceEntity>().FirstOrDefault();
            foreach (FrameworkElement element in Ancestors(source).OfType<FrameworkElement>()) {
                foreach (DependencyProperty property in BoundProperties(element)) {
                    BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(element, property);
                    foreach (BindingExpression leaf in Leaves(expression)) {
                        pendingInput ??= expression;
                        try {
                            // Keep the visual owner for expressions whose context is an evaluation
                            // scope. Compound editors may instead bind to a child entity.
                            ISequenceEntity boundOwner = leaf.ResolvedSource as ISequenceEntity
                                ?? (leaf.ResolvedSource as Logic.Expression)?.Symbol
                                ?? (leaf.ResolvedSource as Logic.Expression)?.Context;
                            foreach (ISequenceEntity owner in new[] { visualOwner, boundOwner }) {
                                if (owner == null) continue;
                                SequenceEditHistory selected = history.ForOwner(owner);
                                if (selected == null) continue;
                                SequencePropertyCapture value = SequencePropertyCapture.Create(owner, leaf);
                                if (value == null) continue;
                                editor = element;
                                binding = expression;
                                inputBinding = pendingInput;
                                capture = value;
                                session = selected;
                                if (editor is ComboBox combo) combo.DropDownClosed += PopupClosed;
                                if (editor is DatePicker date) date.CalendarClosed += PopupClosed;
                                generation++;
                                return;
                            }
                        } catch (Exception ex) { Logger.Error("Unable to capture sequence edit", ex); }
                    }
                }
                if (ReferenceEquals(element, AssociatedObject)) break;
            }
        }

        internal void Commit() {
            if (capture == null || completing) return;
            completing = true;
            try {
                if (interacted && session.IsRecording) {
                    // Composite controls can defer the inner text binding as well as the
                    // outer model binding. Flush them in input-to-model order.
                    if (!ReferenceEquals(inputBinding, binding)) {
                        if (inputBinding is BindingExpression input) input.UpdateSource();
                        else if (inputBinding is MultiBindingExpression inputs) inputs.UpdateSource();
                    }
                    if (binding is BindingExpression single) single.UpdateSource();
                    else if (binding is MultiBindingExpression multi) multi.UpdateSource();
                    session.RecordApplied(capture.Complete());
                }
            } catch (Exception ex) { Logger.Error("Unable to record sequence edit", ex); }
            finally { Cancel(); completing = false; }
        }
        private void PopupClosed(object sender, EventArgs e) => ScheduleCommit();
        private void Cancel() {
            wheelTimer?.Stop();
            if (editor is ComboBox combo) combo.DropDownClosed -= PopupClosed;
            if (editor is DatePicker date) date.CalendarClosed -= PopupClosed;
            if (session != null) session.HasPendingEdit = false;
            editor = null; binding = null; inputBinding = null; capture = null; session = null; interacted = false; generation++;
        }
        private static readonly ConcurrentDictionary<Type, DependencyProperty[]> editorProperties = new();
        private static IEnumerable<DependencyProperty> BoundProperties(FrameworkElement element) {
            // Template and style bindings are not local values. Discover editor DPs once
            // per control type, including wrappers supplied by plugins.
            var properties = new HashSet<DependencyProperty>(editorProperties.GetOrAdd(element.GetType(), type =>
                TypeDescriptor.GetProperties(type).Cast<PropertyDescriptor>()
                    .Where(property => EditableProperties.Contains(property.Name))
                    .Select(DependencyPropertyDescriptor.FromProperty)
                    .Where(property => property != null).Select(property => property.DependencyProperty).ToArray()));
            LocalValueEnumerator values = element.GetLocalValueEnumerator();
            while (values.MoveNext()) if (EditableProperties.Contains(values.Current.Property.Name)) properties.Add(values.Current.Property);
            return properties;
        }
        private static IEnumerable<BindingExpression> Leaves(BindingExpressionBase expression) {
            if (expression is BindingExpression binding) {
                if (WritesSource(binding.ParentBinding.Mode, binding)) yield return binding;
            } else if (expression is MultiBindingExpression multi) {
                if (!WritesSource(multi.ParentMultiBinding.Mode, multi)) yield break;
                var children = multi.BindingExpressions.OfType<BindingExpression>().ToArray();
                if (children.Length != multi.BindingExpressions.Count) yield break;
                var editable = children.Where(child => WritesSource(child.ParentBinding.Mode == BindingMode.Default
                    ? multi.ParentMultiBinding.Mode : child.ParentBinding.Mode, multi)).ToArray();
                // Read-only inputs can provide display context, such as the current camera gain.
                // Only a single writable value can use ordinary property capture.
                if (editable.Length == 1) {
                    yield return editable[0];
                } else if (editable.Length > 1 && children.FirstOrDefault()?.ResolvedSource is NINA.Astrometry.InputCoordinates or NINA.Astrometry.InputTopocentricCoordinates
                    && children.All(child => ReferenceEquals(child.ResolvedSource, children[0].ResolvedSource))) {
                    // Declination sign and degrees are committed together by the existing converter.
                    // The coordinate adapter captures the complete value, including negative zero.
                    yield return children[0];
                }
            } else if (expression is PriorityBindingExpression priority && priority.ActiveBindingExpression != null) {
                foreach (BindingExpression leaf in Leaves(priority.ActiveBindingExpression)) yield return leaf;
            }
        }
        private static bool WritesSource(BindingMode mode, BindingExpressionBase expression) =>
            mode is BindingMode.TwoWay or BindingMode.OneWayToSource
            || (mode == BindingMode.Default && expression.TargetProperty.GetMetadata(expression.Target) is FrameworkPropertyMetadata { BindsTwoWayByDefault: true });

        private static bool IsWithin(DependencyObject source, DependencyObject ancestor) => Ancestors(source).Any(x => ReferenceEquals(x, ancestor));
        private static IEnumerable<DependencyObject> Ancestors(DependencyObject element) {
            while (element != null) {
                yield return element;
                DependencyObject parent = element is Visual ? VisualTreeHelper.GetParent(element) : null;
                element = parent ?? (element as FrameworkElement)?.Parent ?? LogicalTreeHelper.GetParent(element);
            }
        }
    }
}