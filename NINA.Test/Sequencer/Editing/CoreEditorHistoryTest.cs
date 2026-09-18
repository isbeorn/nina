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
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Editing;
using NINA.Sequencer.Logic;
using Expression = NINA.Sequencer.Logic.Expression;
using NINA.Sequencer.SequenceItem.Expressions;
using NUnit.Framework;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class CoreEditorHistoryTest {
        public static IEnumerable<Type> CoreEntities => typeof(ISequenceEntity).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ISequenceEntity).IsAssignableFrom(t) && t.GetCustomAttributes<ExportAttribute>().Any())
            .OrderBy(t => t.FullName);

        [TestCaseSource(nameof(CoreEntities))]
        public void CoreTemplate_EditableBindingsUndoAndRedo(Type type) {
            using var scope = new CoreEditorTestScope();
            ISequenceEntity entity = scope.Create(type);
            scope.Show(entity);
            var failures = new List<string>();
            var fields = new List<string>();
            int checkedInputs = 0;
            foreach (FrameworkElement editor in Descendants<FrameworkElement>(scope.Host).ToArray()) {
                DependencyProperty? property = editor switch {
                    TextBox box when !box.IsReadOnly && !HasComboAncestor(box) && !Descendants<TextBox>(box).Skip(1).Any(t => !t.IsReadOnly) => TextBox.TextProperty,
                    ComboBox { IsEditable: true } => ComboBox.TextProperty,
                    ComboBox combo => BindingOperations.IsDataBound(combo, Selector.SelectedValueProperty) ? Selector.SelectedValueProperty : Selector.SelectedItemProperty,
                    CheckBox => ToggleButton.IsCheckedProperty,
                    _ => null
                };
                if (property == null) continue;
                BindingExpressionBase? binding = BindingOperations.GetBindingExpressionBase(editor, property);
                BindingExpression? leaf = binding switch {
                    BindingExpression single => single,
                    MultiBindingExpression multi => multi.BindingExpressions.OfType<BindingExpression>().FirstOrDefault(b => !b.ResolvedSourcePropertyName.StartsWith("Negative")),
                    _ => null
                };
                if (leaf == null || leaf.ParentBinding.Mode is BindingMode.OneWay or BindingMode.OneTime) continue;
                if (!SequenceEditContext.GetIsRecordingEnabled(editor)) continue; // Current variable editor.
                // Follow a composite control's forwarding Text binding for assertions on the
                // accepted model value, while editing the actual inner text box.
                while (leaf.ResolvedSource is FrameworkElement wrapper && leaf.ResolvedSourcePropertyName == "Text") {
                    var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromName("Text", wrapper.GetType(), wrapper.GetType());
                    BindingExpression? forwarded = descriptor == null ? null : wrapper.GetBindingExpression(descriptor.DependencyProperty);
                    if (forwarded == null) break;
                    leaf = forwarded;
                }
                string name = $"{leaf.ResolvedSource?.GetType().Name}.{leaf.ResolvedSourcePropertyName}";
                if (leaf.ResolvedSource is Expression exp) {
                    ISequenceEntity expressionOwner = exp.Symbol ?? exp.Context ?? entity;
                    PropertyInfo? expressionProperty = expressionOwner.GetType().GetProperties().FirstOrDefault(p => p.PropertyType == typeof(Expression) && ReferenceEquals(p.GetValue(expressionOwner), exp));
                    if (expressionProperty != null) name = $"{expressionOwner.GetType().Name}.{expressionProperty.Name}";
                }
                try {
                    object source = leaf.ResolvedSource!;
                    PropertyInfo modelProperty = source.GetType().GetProperty(leaf.ResolvedSourcePropertyName)!;
                    object? Read() => modelProperty.IsDefined(typeof(NINA.Sequencer.Generators.IsExpressionAttribute))
                        ? ((Expression)source.GetType().GetProperty(modelProperty.Name + "Expression")!.GetValue(source)!).Definition
                        : modelProperty.GetValue(source);
                    object? before = Read();
                    int position = scope.History.Position;
                    if (editor is TextBox text) {
                        string input = modelProperty.PropertyType == typeof(string) && modelProperty.Name != "Definition" ? "HistoryAudit" : text.Text == "12" ? "13" : "12";
                        TypeText(text, input);
                        text.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                        BindingOperations.GetMultiBindingExpression(text, TextBox.TextProperty)?.UpdateSource();
                    } else if (editor is ComboBox combo) {
                        MouseDown(combo);
                        if (combo.IsEditable) combo.SetCurrentValue(ComboBox.TextProperty, combo.Text == "Green" ? "Red" : "Green");
                        else {
                            combo.Items.Count.Should().BeGreaterThan(1, name);
                            combo.SetCurrentValue(Selector.SelectedIndexProperty, combo.Items.Count - 1 == combo.SelectedIndex ? 0 : combo.Items.Count - 1);
                        }
                        combo.GetBindingExpression(property)?.UpdateSource();
                        BindingOperations.GetMultiBindingExpression(combo, property)?.UpdateSource();
                    } else if (editor is CheckBox check) {
                        MouseDown(check);
                        check.SetCurrentValue(ToggleButton.IsCheckedProperty, check.IsChecked != true);
                        check.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateSource();
                    }
                    scope.Behavior.Commit();
                    object? after = Read();
                    after.Should().NotBe(before, $"{name} must actually change through its binding");
                    scope.History.Position.Should().Be(position + 1, name);
                    scope.History.Undo().Should().BeTrue(name);
                    Read().Should().Be(before, $"undo {name}");
                    scope.History.Redo().Should().BeTrue(name);
                    Read().Should().Be(after, $"redo {name}");
                    checkedInputs++;
                    fields.Add(name);
                } catch (Exception ex) {
                    failures.Add($"{name}: {ex.Message}");
                }
            }
            TestContext.Out.WriteLine($"AUDIT {type.Name} | {checkedInputs} | {string.Join(", ", fields)}");
            failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
        }

        [TestCase(typeof(NINA.Sequencer.SequenceItem.FlatDevice.SetBrightness))]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Switch.SetSwitchValue))]
        public void ExpressionStepper_ButtonsRecordBothDirections(Type type) {
            using var scope = new CoreEditorTestScope();
            scope.Show(scope.Create(type));
            var stepper = Descendants<ExprStepperControl>(scope.Host).Single();
            stepper.Exp.Definition = "10";
            var buttons = Descendants<Button>(stepper).ToArray();
            foreach (Button button in new[] { buttons[1], buttons[0] }) {
                string before = stepper.Exp.Definition;
                int position = scope.History.Position;
                MouseDown(button);
                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                scope.Behavior.Commit();
                string after = stepper.Exp.Definition;
                after.Should().NotBe(before);
                scope.History.Position.Should().Be(position + 1);
                scope.History.Undo().Should().BeTrue();
                stepper.Exp.Definition.Should().Be(before);
                scope.History.Redo().Should().BeTrue();
                stepper.Exp.Definition.Should().Be(after);
            }
            foreach (var boundary in new[] { (Value: stepper.Min, Button: buttons[0]), (Value: stepper.Max, Button: buttons[1]) }) {
                stepper.Exp.Definition = boundary.Value.ToString(System.Globalization.CultureInfo.CurrentCulture);
                int position = scope.History.Position;
                boundary.Button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                scope.Behavior.Commit();
                scope.History.Position.Should().Be(position, "a rejected adjustment at the limit is not an edit");
            }
        }

        [TestCase(typeof(NINA.Sequencer.Conditions.AltitudeCondition), "RASeconds", "59.9")]
        [TestCase(typeof(NINA.Sequencer.Conditions.AltitudeCondition), "DecDegrees", "-90")]
        [TestCase(typeof(NINA.Sequencer.Conditions.AltitudeCondition), "DecDegrees", "90")]
        [TestCase(typeof(NINA.Sequencer.Conditions.AboveHorizonCondition), "DecDegrees", "-0")]
        [TestCase(typeof(NINA.Sequencer.Conditions.AboveHorizonCondition), "DecSeconds", "59.9")]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Telescope.SlewScopeToAltAz), "AltDegrees", "-90")]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Telescope.SlewScopeToAltAz), "AltDegrees", "90")]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Telescope.SlewScopeToAltAz), "AltDegrees", "-0")]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Telescope.SlewScopeToAltAz), "AltSeconds", "59.9")]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Telescope.SlewScopeToAltAz), "AzDegrees", "359")]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Telescope.SlewScopeToAltAz), "AzDegrees", "0")]
        public void CoordinateComponents_RestoreExpressionsAndWholeCoordinates(Type type, string field, string input) {
            using var scope = new CoreEditorTestScope();
            ISequenceEntity entity = scope.Create(type);
            scope.Show(entity);
            bool horizontal = entity is NINA.Sequencer.SequenceItem.Telescope.SlewScopeToAltAz;
            var first = (Expression)type.GetProperty(horizontal ? "AltExpression" : "RaExpression")!.GetValue(entity)!;
            var second = (Expression)type.GetProperty(horizontal ? "AzExpression" : "DecExpression")!.GetValue(entity)!;
            first.Definition = "2+3";
            second.Definition = "10+10";
            Drain();
            var box = Descendants<TextBox>(scope.Host).Single(t => t.GetBindingExpression(TextBox.TextProperty)?.ResolvedSourcePropertyName == field
                || BindingOperations.GetMultiBindingExpression(t, TextBox.TextProperty)?.BindingExpressions.OfType<BindingExpression>().Any(b => b.ResolvedSourcePropertyName == field) == true);
            var leaf = box.GetBindingExpression(TextBox.TextProperty) ?? BindingOperations.GetMultiBindingExpression(box, TextBox.TextProperty).BindingExpressions.OfType<BindingExpression>().Last();
            object coordinates = leaf.ResolvedSource!;
            string State() => coordinates is NINA.Astrometry.InputCoordinates equatorial
                ? $"{equatorial.Coordinates.RA:R}|{equatorial.Coordinates.Dec:R}|{equatorial.NegativeDec}|{equatorial.Coordinates.Epoch}"
                : $"{((NINA.Astrometry.InputTopocentricCoordinates)coordinates).Coordinates.Altitude.Degree:R}|{((NINA.Astrometry.InputTopocentricCoordinates)coordinates).Coordinates.Azimuth.Degree:R}|{((NINA.Astrometry.InputTopocentricCoordinates)coordinates).NegativeAlt}";
            string before = State();
            TypeText(box, input);
            scope.Behavior.Commit();
            string after = State();
            after.Should().NotBe(before);
            scope.History.Position.Should().Be(1);
            for (int repeat = 0; repeat < 3; repeat++) {
                scope.History.Undo().Should().BeTrue();
                State().Should().Be(before);
                first.Definition.Should().Be("2+3");
                second.Definition.Should().Be("10+10");
                scope.History.Redo().Should().BeTrue();
                State().Should().Be(after);
            }
        }

        [TestCase(typeof(NINA.Sequencer.Conditions.AltitudeCondition))]
        [TestCase(typeof(NINA.Sequencer.Conditions.AboveHorizonCondition))]
        public void CoordinateCondition_MoveIntoTargetAndUndo_RestoresExplicitDefinitions(Type type) {
            using var scope = new CoreEditorTestScope();
            var condition = (NINA.Sequencer.Conditions.ISequenceCondition)scope.Create(type);
            scope.Root.Add(condition);
            var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
            scope.Root.Add(target);
            var ra = (Expression)type.GetProperty("RaExpression")!.GetValue(condition)!;
            var dec = (Expression)type.GetProperty("DecExpression")!.GetValue(condition)!;
            var rotation = (Expression)type.GetProperty("PositionAngleExpression")!.GetValue(condition)!;
            ra.Definition = "1+1";
            dec.Definition = "3+4";
            rotation.Definition = "20+10";
            target.DropIntoConditionsCommand.Execute(new DropIntoParameters(condition, target, NINA.Core.Enum.DropTargetEnum.Center));
            condition.Parent.Should().BeSameAs(target);
            scope.History.Position.Should().Be(1);
            for (int repeat = 0; repeat < 3; repeat++) {
                scope.History.Undo().Should().BeTrue();
                condition.Parent.Should().BeSameAs(scope.Root);
                ra.Definition.Should().Be("1+1");
                dec.Definition.Should().Be("3+4");
                rotation.Definition.Should().Be("20+10");
                scope.History.Redo().Should().BeTrue();
                condition.Parent.Should().BeSameAs(target);
            }
        }

        internal static void TypeText(TextBox text, string value) {
            text.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice,
                new TextComposition(InputManager.Current, text, value)) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
            text.SetCurrentValue(TextBox.TextProperty, value);
            text.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
        }

        [TestCase(typeof(NINA.Sequencer.SequenceItem.Utility.WaitForTime))]
        [TestCase(typeof(NINA.Sequencer.Conditions.TimeCondition))]
        [TestCase(typeof(ResetVariableToDate))]
        public void TimeProviderSelection_UndoRestoresManualTime(Type type) {
            using var scope = new CoreEditorTestScope();
            ISequenceEntity entity = scope.Create(type);
            scope.Show(entity);
            string[] parts = { "Hours", "Minutes", "Seconds" };
            int[] before = { 11, 22, 33 };
            for (int i = 0; i < parts.Length; i++) type.GetProperty(parts[i])!.SetValue(entity, before[i]);
            var combo = Descendants<ComboBox>(scope.Host).Single(c => c.GetBindingExpression(Selector.SelectedItemProperty)?.ResolvedSourcePropertyName == "SelectedProvider");
            MouseDown(combo);
            combo.SetCurrentValue(Selector.SelectedIndexProperty, 1);
            scope.Behavior.Commit();
            scope.History.Position.Should().Be(1);
            scope.History.Undo().Should().BeTrue();
            parts.Select(p => (int)type.GetProperty(p)!.GetValue(entity)!).Should().Equal(before);
            scope.History.Redo().Should().BeTrue();
            parts.Select(p => (int)type.GetProperty(p)!.GetValue(entity)!).Should().Equal(20, 30, 40);
        }

        [Test]
        public void LinkedTemplateTarget_AllOverrideInputsReplayThroughActualTemplate() {
            using var scope = new CoreEditorTestScope();
            var linked = new LinkedTemplateContainer { IsExpanded = true };
            var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
            linked.MaterializeFromTemplate(new TemplatedSequenceContainer((NINA.Profile.Interfaces.IProfileService)Application.Current.Resources["ProfileService"], "Test", target), true);
            scope.Show(linked);
            string State() => linked.TargetOverride == null ? "None" : $"{linked.TargetOverride.TargetName}|{linked.TargetOverride.InputCoordinates.Coordinates.RA:R}|{linked.TargetOverride.InputCoordinates.Coordinates.Dec:R}|{linked.TargetOverride.PositionAngle:R}|{linked.TargetEditor.InputCoordinates.NegativeDec}";
            int count = 0;
            foreach (TextBox box in Descendants<TextBox>(scope.Host).Where(t => !t.IsReadOnly).ToArray()) {
                BindingExpression? binding = box.GetBindingExpression(TextBox.TextProperty)
                    ?? BindingOperations.GetMultiBindingExpression(box, TextBox.TextProperty)?.BindingExpressions.OfType<BindingExpression>().FirstOrDefault();
                if (binding == null || (!ReferenceEquals(binding.ResolvedSource, linked.TargetEditor) && !ReferenceEquals(binding.ResolvedSource, linked.TargetEditor.InputCoordinates))) continue;
                string before = State();
                TypeText(box, binding.ResolvedSourcePropertyName == "TargetName" ? "M31" : "12");
                scope.Behavior.Commit();
                string after = State();
                after.Should().NotBe(before);
                scope.History.Position.Should().Be(++count);
                scope.History.Undo().Should().BeTrue();
                State().Should().Be(before);
                scope.History.Redo().Should().BeTrue();
                State().Should().Be(after);
            }
            count.Should().Be(8);
        }

        [TestCase(typeof(NINA.Sequencer.SequenceItem.Utility.Annotation))]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Utility.MessageBox))]
        [TestCase(typeof(NINA.Sequencer.SequenceItem.Utility.ExternalScript))]
        public void CompositeTextEditor_ToolbarUndoCommitsPendingInnerText(Type type) {
            using var scope = new CoreEditorTestScope();
            scope.Show(scope.Create(type));
            var editor = Descendants<ExprStringControl>(scope.Host).Single();
            var box = Descendants<TextBox>(editor).Single();
            string before = editor.Text;
            box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, "after")) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
            box.SetCurrentValue(TextBox.TextProperty, "after");
            scope.History.Undo().Should().BeTrue();
            editor.Text.Should().Be(before);
            scope.History.Redo().Should().BeTrue();
            editor.Text.Should().Be("after");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LinkedTemplateTriggerEditors_UseTemplateSessionOnlyWhenEditing(bool editing) {
            using var scope = new CoreEditorTestScope();
            var template = new SequentialContainer();
            template.Add((NINA.Sequencer.Trigger.ISequenceTrigger)scope.Create(typeof(NINA.Sequencer.Trigger.MeridianFlip.ProgrammableMeridianFlipTrigger)));
            var linked = new LinkedTemplateContainer();
            linked.MaterializeFromTemplate(new TemplatedSequenceContainer((NINA.Profile.Interfaces.IProfileService)Application.Current.Resources["ProfileService"], "Test", template), true);
            scope.Root.Add(linked);
            if (editing) linked.BeginEditTemplateCommand.Execute(null);
            var session = scope.History.ActiveHistory;
            var materialized = (SequenceContainer)linked.Items.Single();
            var trigger = (NINA.Sequencer.Trigger.MeridianFlip.ProgrammableMeridianFlipTrigger)materialized.Triggers.Single();
            var item = new NINA.Sequencer.SequenceItem.Utility.WaitForTimeSpan();
            trigger.BeforeFlipActions.Add(item);
            scope.Host.DataContext = item;
            scope.Host.Content = item;
            Drain();
            var box = Descendants<TextBox>(scope.Host).Single(t => t.GetBindingExpression(TextBox.TextProperty)?.ResolvedSource is Expression);
            TypeText(box, "12");
            scope.Behavior.Commit();
            scope.History.Position.Should().Be(0);
            session.Position.Should().Be(editing ? 1 : 0);
            if (editing) {
                session.Should().NotBeSameAs(scope.History);
                session.Undo().Should().BeTrue();
                session.Redo().Should().BeTrue();
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ProgrammableFlipActionSets_FieldAndStructureUndoRedo(bool beforeFlip) {
            using var scope = new CoreEditorTestScope();
            var trigger = (NINA.Sequencer.Trigger.MeridianFlip.ProgrammableMeridianFlipTrigger)scope.Create(typeof(NINA.Sequencer.Trigger.MeridianFlip.ProgrammableMeridianFlipTrigger));
            scope.Root.Add(trigger);
            SequentialContainer actions = beforeFlip ? trigger.BeforeFlipActions : trigger.AfterFlipActions;
            var item = new NINA.Sequencer.SequenceItem.Utility.WaitForTimeSpan();
            actions.Add(item);
            scope.Host.DataContext = item;
            scope.Host.Content = item;
            scope.Host.ContentTemplate = (DataTemplate)Application.Current.FindResource(new DataTemplateKey(item.GetType()));
            Drain();
            var box = Descendants<TextBox>(scope.Host).Single(t => t.GetBindingExpression(TextBox.TextProperty)?.ResolvedSource is Expression);
            string original = item.TimeExpression.Definition;
            TypeText(box, "12");
            box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            scope.Behavior.Commit();
            scope.History.Position.Should().Be(1);
            scope.History.Undo().Should().BeTrue();
            item.TimeExpression.Definition.Should().Be(original);
            scope.History.Redo().Should().BeTrue();
            item.TimeExpression.Definition.Should().Be("12");
            item.DetachCommand.Execute(null);
            actions.Items.Should().NotContain(item);
            scope.History.Position.Should().Be(2);
            scope.History.Undo().Should().BeTrue();
            actions.Items.Should().Contain(item);
            scope.History.Redo().Should().BeTrue();
            actions.Items.Should().NotContain(item);
        }

        internal static void MouseDown(FrameworkElement element) => element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });

        private static bool HasComboAncestor(DependencyObject element) {
            for (DependencyObject? current = System.Windows.Media.VisualTreeHelper.GetParent(element); current != null; current = System.Windows.Media.VisualTreeHelper.GetParent(current)) {
                if (current is ComboBox) return true;
            }
            return false;
        }
    }
}