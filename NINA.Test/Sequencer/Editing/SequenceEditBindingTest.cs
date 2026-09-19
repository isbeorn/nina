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
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Expressions;
using NUnit.Framework;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequenceEditBindingTest {
        public class Settings {
            public double Value { get; set; } = 1;
            public DateTime? Date { get; set; } = new DateTime(2026, 1, 1);
        }
        public class PluginItem : SequenceEditHistoryTest.PluginItem {
            [JsonProperty]
            public Settings Settings { get; set; } = new();
        }

        private sealed class MultiValueConverter : IMultiValueConverter {
            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) => System.Convert.ToString(values[0], culture)!;
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
                double number = double.Parse(value.ToString()!, culture);
                return new object[] { number, number };
            }
        }

        [TestCase(BindingMode.TwoWay, BindingMode.OneWay, true)]
        [TestCase(BindingMode.Default, BindingMode.OneWay, true)]
        [TestCase(BindingMode.TwoWay, BindingMode.OneTime, true)]
        [TestCase(BindingMode.OneWay, BindingMode.OneWay, false)]
        [TestCase(BindingMode.OneTime, BindingMode.OneWay, false)]
        [TestCase(BindingMode.TwoWay, BindingMode.TwoWay, false)]
        public void PluginMultiBinding_RecordsOnlyOneWritableValue(BindingMode mode, BindingMode contextMode, bool recorded) {
            var root = new SequenceRootContainer();
            var item = new PluginItem { Setting = 10 };
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            var multi = new MultiBinding { Converter = new MultiValueConverter(), Mode = mode };
            multi.Bindings.Add(new Binding("Settings.Value"));
            multi.Bindings.Add(new Binding(nameof(item.Setting)) { Mode = contextMode });
            box.SetBinding(TextBox.TextProperty, multi);
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(box);
            try {
                box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, "42")) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
                box.SetCurrentValue(TextBox.TextProperty, "42");
                BindingOperations.GetMultiBindingExpression(box, TextBox.TextProperty).UpdateSource();
                behavior.Commit();
                bool writable = mode is BindingMode.TwoWay or BindingMode.Default;
                item.Settings.Value.Should().Be(writable ? 42 : 1);
                item.Setting.Should().Be(writable && contextMode == BindingMode.TwoWay ? 42 : 10);
                history.Position.Should().Be(recorded ? 1 : 0);
                if (recorded) {
                    history.Undo().Should().BeTrue();
                    item.Settings.Value.Should().Be(1);
                    box.Text.Should().Be("1");
                    history.Redo().Should().BeTrue();
                    item.Settings.Value.Should().Be(42);
                    box.Text.Should().Be("42");
                    item.Setting.Should().Be(10);
                }
                BindingOperations.GetMultiBinding(box, TextBox.TextProperty).Should().BeSameAs(multi);
            } finally { behavior.Detach(); }
        }

        private sealed class CustomSettingEdit : ISequenceEdit {
            private readonly Settings settings;
            public CustomSettingEdit(Settings settings) { this.settings = settings; }
            public string Description => "Custom setting";
            public bool CanUndo => settings.Value == 2;
            public bool CanRedo => settings.Value == 1;
            public void Undo() { settings.Value = 1; }
            public void Redo() { settings.Value = 2; }
        }

        [Test]
        public void OpaquePluginEditor_UsesOptionalAttachedContextWithoutAutomaticShallowCopies() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var editor = new TextBox { DataContext = item };
            editor.SetBinding(TextBox.TextProperty, new Binding(nameof(item.Settings)));
            SequencePropertyCapture.Create(item, editor.GetBindingExpression(TextBox.TextProperty)).Should().BeNull();
            SequenceEditContext.SetHistory(editor, history);
            SequenceEditContext.SetIsRecordingEnabled(editor, false);
            ISequenceEditHistory context = SequenceEditContext.GetHistory(editor);
            using (context.BeginTransaction("Custom setting")) {
                item.Settings.Value = 2;
                context.RecordApplied(new CustomSettingEdit(item.Settings));
            }
            history.Position.Should().Be(1);
            history.Undo().Should().BeTrue();
            item.Settings.Value.Should().Be(1);
            history.Redo().Should().BeTrue();
            item.Settings.Value.Should().Be(2);
        }

        [Test]
        public void NestedPluginValue_PasteAndFocusDeparture_KeepBindingsAndGroupOneEdit() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var panel = new StackPanel { DataContext = item };
            var box = new TextBox();
            panel.Children.Add(box);
            var binding = new Binding("Settings.Value") { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus };
            box.SetBinding(TextBox.TextProperty, binding);
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(panel);
            try {
                box.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste,
                    (_, e) => { box.Text = "42"; e.Handled = true; },
                    (_, e) => { e.CanExecute = true; e.Handled = true; }));
                ApplicationCommands.Paste.Execute(null, box);
                box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, box, panel) { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
                Drain();
                history.Position.Should().Be(1);
                item.Settings.Value.Should().Be(42);
                history.Undo().Should().BeTrue();
                item.Settings.Value.Should().Be(1);
                history.Redo().Should().BeTrue();
                item.Settings.Value.Should().Be(42);
                BindingOperations.GetBinding(box, TextBox.TextProperty).Should().BeSameAs(binding);
            } finally { behavior.Detach(); }
        }

        [Test]
        public void DropdownCheckboxAndDate_CommitUserGesturesAndReplayBothWays() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var panel = new StackPanel { DataContext = item };
            var combo = new ComboBox { ItemsSource = new[] { "before", "after" } };
            var check = new CheckBox();
            var date = new DatePicker();
            panel.Children.Add(combo);
            panel.Children.Add(check);
            panel.Children.Add(date);
            combo.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(item.Text)));
            check.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(item.Enabled)));
            date.SetBinding(DatePicker.SelectedDateProperty, new Binding("Settings.Date"));
            var window = new Window { Content = panel, Width = 300, Height = 200, Left = -32000, Top = -32000, ShowActivated = false, ShowInTaskbar = false };
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(panel);
            try {
                window.Show();
                Drain();
                MouseDown(combo);
                combo.IsDropDownOpen = true;
                combo.IsDropDownOpen.Should().BeTrue();
                MouseUp(combo);
                Drain();
                history.Position.Should().Be(0);
                combo.SelectedItem = "after";
                combo.IsDropDownOpen = false;
                Drain();
                history.Position.Should().Be(1);
                MouseDown(check);
                check.IsChecked = true;
                MouseUp(check);
                Drain();
                MouseDown(date);
                date.SelectedDate = new DateTime(2026, 12, 31);
                MouseUp(date);
                Drain();
                history.Position.Should().Be(3);
                history.MoveTo(0);
                item.Text.Should().Be("before");
                item.Enabled.Should().BeFalse();
                item.Settings.Date.Should().Be(new DateTime(2026, 1, 1));
                history.MoveTo(3);
                item.Text.Should().Be("after");
                item.Enabled.Should().BeTrue();
                item.Settings.Date.Should().Be(new DateTime(2026, 12, 31));
            } finally { behavior.Detach(); window.Close(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealExpressionControl_ClearAndInvalidInput_ReplayAcceptedDefinitions(bool contextIsScope) {
            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            foreach (string resource in new[] { "ProfileService", "SVGDictionary", "Brushes", "Converters" }) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"/NINA.WPF.Base;component/Resources/StaticResources/{resource}.xaml", UriKind.Relative) });
            }
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            if (contextIsScope) item.Formula.Context = root;
            var control = new ExprControl { DataContext = item };
            control.SetBinding(ExprControl.ExpProperty, new Binding(nameof(item.Formula)));
            control.Measure(new Size(500, 100));
            control.Arrange(new Rect(0, 0, 500, 100));
            control.UpdateLayout();
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(control);
            try {
                var box = FindTextBox(control);
                box.Should().NotBeNull();
                box!.GetBindingExpression(TextBox.TextProperty).ResolvedSource.Should().BeSameAs(item.Formula);
                Type(box!, "(");
                behavior.Commit();
                Type(box!, "");
                behavior.Commit();
                history.Position.Should().Be(2);
                history.Undo().Should().BeTrue();
                item.Formula.Definition.Should().Be("(");
                history.Undo().Should().BeTrue();
                item.Formula.Definition.Should().Be("1");
                history.MoveTo(2);
                item.Formula.Definition.Should().BeEmpty();
            } finally { behavior.Detach(); }
        }

        [Test]
        public void SymbolRenameDetachAndRestore_ResolveNewExpressionsWithoutGrowingConsumers() {
            var root = new SequenceRootContainer();
            var variable = new Variable("HistoryVariable", "1", root);
            root.Add(variable);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = variable };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(variable.Identifier)));
            var name = SequencePropertyCapture.Create(variable, box.GetBindingExpression(TextBox.TextProperty));
            variable.Identifier = "RenamedHistoryVariable";
            history.RecordApplied(name!.Complete());
            box.SetBinding(TextBox.TextProperty, new Binding("OriginalExpr.Definition"));
            var value = SequencePropertyCapture.Create(variable, box.GetBindingExpression(TextBox.TextProperty));
            variable.OriginalExpr.Definition = "2";
            history.RecordApplied(value!.Complete());
            variable.Expr.Definition = "99";
            variable.DetachCommand.Execute(null);
            for (int i = 0; i < 10; i++) {
                history.MoveTo(0);
                root.Items.Should().ContainSingle().Which.Should().BeSameAs(variable);
                variable.Identifier.Should().Be("HistoryVariable");
                variable.OriginalExpr.Definition.Should().Be("1");
                variable.Expr.Definition.Should().Be("99");
                variable.Consumers.Should().BeEmpty();
                history.MoveTo(3);
                root.Items.Should().BeEmpty();
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VariableExpressionBinding_RecordsInitialValueButNotRunningValue(bool initialValue) {
            var root = new SequenceRootContainer();
            var variable = new Variable("HistoryVariable", "1", root);
            root.Add(variable);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = variable };
            box.SetBinding(TextBox.TextProperty, new Binding(initialValue ? "OriginalExpr.Definition" : "Expr.Definition"));
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(box);
            try {
                Type(box, "2");
                box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                behavior.Commit();
                history.Position.Should().Be(initialValue ? 1 : 0);
                if (initialValue) {
                    history.Undo().Should().BeTrue();
                    variable.OriginalExpr.Definition.Should().Be("1");
                    history.Redo().Should().BeTrue();
                    variable.OriginalExpr.Definition.Should().Be("2");
                    variable.Expr.Definition.Should().Be("1");
                } else {
                    variable.Expr.Definition.Should().Be("2");
                    variable.OriginalExpr.Definition.Should().Be("1");
                }
            } finally { behavior.Detach(); }
        }

        private static TextBox? FindTextBox(DependencyObject element) {
            if (element is TextBox box) return box;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) {
                var found = FindTextBox(VisualTreeHelper.GetChild(element, i));
                if (found != null) return found;
            }
            return element is ContentControl { Content: DependencyObject content } ? FindTextBox(content) : null;
        }

        [Test]
        public void FocusedTextBox_NativeUndoThenRedo_LeavesSequenceHistoryUntilCommit() {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var panel = new StackPanel { DataContext = item };
            var box = new TextBox();
            panel.Children.Add(box);
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(item.Text)));
            var window = new Window { Content = panel, Width = 300, Height = 200, Left = -32000, Top = -32000, ShowActivated = false, ShowInTaskbar = false };
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(panel);
            try {
                window.Show();
                box.Focus();
                Drain();
                box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, "after")) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
                box.SelectAll();
                box.SelectedText = "after";
                box.CanUndo.Should().BeTrue();
                ApplicationCommands.Undo.Execute(null, box);
                box.Text.Should().Be("before");
                behavior.Commit();
                history.Position.Should().Be(0);
                ApplicationCommands.Redo.Execute(null, box);
                box.Text.Should().Be("after");
                history.Position.Should().Be(0);
                behavior.Commit();
                history.Position.Should().Be(1);
                history.Undo().Should().BeTrue();
                item.Text.Should().Be("before");
            } finally { behavior.Detach(); window.Close(); }
        }

        [TestCase("-0", 0, true)]
        [TestCase("-90", -90, true)]
        [TestCase("90", 90, false)]
        public void DeclinationMultiBinding_RestoresSignAndValue(string input, double degrees, bool negative) {
            var root = new SequenceRootContainer();
            var item = new PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var panel = new StackPanel { DataContext = item };
            var box = new TextBox();
            panel.Children.Add(box);
            var multi = new MultiBinding { Converter = new NINA.Core.Utility.Converters.DecDegreeConverter(), UpdateSourceTrigger = UpdateSourceTrigger.LostFocus };
            multi.Bindings.Add(new Binding("Coordinates.NegativeDec"));
            multi.Bindings.Add(new Binding("Coordinates.DecDegrees"));
            box.SetBinding(TextBox.TextProperty, multi);
            var behavior = new SequenceEditBehavior { History = history };
            behavior.Attach(panel);
            try {
                Type(box, input);
                behavior.Commit();
                history.Position.Should().Be(1);
                item.Coordinates.Coordinates.Dec.Should().Be(degrees);
                item.Coordinates.NegativeDec.Should().Be(negative);
                history.Undo().Should().BeTrue();
                item.Coordinates.Coordinates.Dec.Should().Be(0);
                item.Coordinates.NegativeDec.Should().BeFalse();
                history.Redo().Should().BeTrue();
                item.Coordinates.Coordinates.Dec.Should().Be(degrees);
                item.Coordinates.NegativeDec.Should().Be(negative);
            } finally { behavior.Detach(); }
        }

        [Test]
        public void CoordinateComponentEdit_RestoresTheOriginalExpressionDefinitions() {
            var root = new SequenceRootContainer();
            var item = new NINA.Sequencer.SequenceItem.Telescope.CoordinatesInstruction();
            root.Add(item);
            item.RaExpression.Definition = "1+1";
            item.DecExpression.Definition = "3+4";
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding("Coordinates.RAHours"));
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            item.Coordinates.RAHours = 5;
            history.RecordApplied(capture!.Complete());
            history.Undo().Should().BeTrue();
            item.RaExpression.Definition.Should().Be("1+1");
            item.DecExpression.Definition.Should().Be("3+4");
            history.Redo().Should().BeTrue();
            item.Coordinates.RAHours.Should().Be(5);
        }

        [Test]
        public void GeneratedNumericPropertyEdit_RestoresDefinitionInsteadOfItsEvaluatedValue() {
            var root = new SequenceRootContainer();
            var condition = new NINA.Sequencer.Conditions.LoopCondition();
            root.Add(condition);
            condition.IterationsExpression.Definition = "1+1";
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = condition };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(condition.Iterations)));
            var capture = SequencePropertyCapture.Create(condition, box.GetBindingExpression(TextBox.TextProperty));
            condition.Iterations = 5;
            history.RecordApplied(capture!.Complete());
            history.Undo().Should().BeTrue();
            condition.IterationsExpression.Definition.Should().Be("1+1");
            history.Redo().Should().BeTrue();
            condition.Iterations.Should().Be(5);
        }
        private static void Type(TextBox box, string text) {
            box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, text)) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
            box.Text = text;
        }
        private static void MouseDown(UIElement element) => element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });
        private static void MouseUp(UIElement element) => element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseUpEvent });
        private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
}