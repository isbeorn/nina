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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Utility;
using NINA.Test.Sequencer.Editing;
using NINA.View.Sequencer;
using NUnit.Framework;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;
using Path = System.Windows.Shapes.Path;

namespace NINA.Test.Sequencer.View {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequenceBlockViewTest {
        [TestCase(typeof(TakeExposure), true)]
        [TestCase(typeof(SmartExposure), true)]
        [TestCase(typeof(AutofocusAfterTimeTrigger), false)]
        [TestCase(typeof(AltitudeCondition), false)]
        public void MenuLifecycle_ConstructsCommandControlsOnlyWhileOpen(Type type, bool hasAttempts) {
            using var scope = new CoreEditorTestScope();
            var item = scope.Create(type);
            scope.Show(item);

            Named<WrapPanel>(scope.Host, "ButtonCommands").Should().BeNull();
            Named<ComboBox>(scope.Host, "PART_ErrorBehavior").Should().BeNull();
            Named<Button>(scope.Host, "ResetProgressButton").Should().BeNull();
            for (int repeat = 0; repeat < 2; repeat++) {
                ExecuteButton(scope.Host, "ShowMenuButton");
                var menu = Named<WrapPanel>(scope.Host, "ButtonCommands")!;
                menu.Should().NotBeNull();
                menu.DataContext.Should().BeSameAs(item);
                Descendants<TextBox>(menu).Count().Should().Be(hasAttempts ? 1 : 0);
                ExecuteButton(scope.Host, "ShowMenuButton");
                Named<WrapPanel>(scope.Host, "ButtonCommands").Should().BeNull();
                PresentationSource.FromVisual(menu).Should().BeNull();
            }
        }

        [Test]
        public void Menu_ReopensWithCurrentValuesAndWorkingCommands() {
            using var scope = new CoreEditorTestScope();
            var item = (TakeExposure)scope.Create(typeof(TakeExposure));
            item.Attempts = 2;
            item.ErrorBehavior = InstructionErrorBehavior.AbortOnError;
            scope.Show(item);

            ExecuteButton(scope.Host, "ShowMenuButton");
            var firstMenu = Named<WrapPanel>(scope.Host, "ButtonCommands")!;
            firstMenu.Should().NotBeNull();
            Descendants<TextBox>(firstMenu).Single().Text.Should().Be("2");
            Named<ComboBox>(firstMenu, "PART_ErrorBehavior")!.SelectedItem.Should().Be(item.ErrorBehavior);
            foreach (string name in new[] { "EnableDisableButton", "ResetProgressButton", "AddCloneToParentButton", "MoveUpButton", "MoveDownButton" }) {
                var button = Named<Button>(firstMenu, name)!;
                button.Command.Should().NotBeNull();
                button.ToolTip.Should().NotBeNull();
                button.DataContext.Should().BeSameAs(item);
            }

            ExecuteButton(scope.Host, "ShowMenuButton");
            Named<WrapPanel>(scope.Host, "ButtonCommands").Should().BeNull();
            item.Attempts = 4;
            item.ErrorBehavior = InstructionErrorBehavior.SkipToSequenceEndInstructions;
            ExecuteButton(scope.Host, "ShowMenuButton");
            var reopenedMenu = Named<WrapPanel>(scope.Host, "ButtonCommands")!;
            reopenedMenu.Should().NotBeSameAs(firstMenu);
            Descendants<TextBox>(reopenedMenu).Single().Text.Should().Be("4");
            Named<ComboBox>(reopenedMenu, "PART_ErrorBehavior")!.SelectedItem.Should().Be(item.ErrorBehavior);

            item.Status = SequenceEntityStatus.FINISHED;
            ExecuteButton(scope.Host, "ResetProgressButton");
            item.Status.Should().Be(SequenceEntityStatus.CREATED);
            item.ShowMenu.Should().BeFalse();
            Named<WrapPanel>(scope.Host, "ButtonCommands").Should().BeNull();
        }

        [Test]
        public void MenuToggle_PreservesDisableEnableAndHistory() {
            using var scope = new CoreEditorTestScope();
            var item = (TakeExposure)scope.Create(typeof(TakeExposure));
            scope.Show(item);
            ExecuteButton(scope.Host, "ShowMenuButton");

            ExecuteButton(scope.Host, "EnableDisableButton");
            item.Status.Should().Be(SequenceEntityStatus.DISABLED);
            item.ShowMenu.Should().BeFalse();
            scope.History.Position.Should().Be(1);
            scope.History.Undo().Should().BeTrue();
            item.Status.Should().Be(SequenceEntityStatus.CREATED);
            scope.History.Redo().Should().BeTrue();
            item.Status.Should().Be(SequenceEntityStatus.DISABLED);

            ExecuteButton(scope.Host, "ShowDisableDisableButtonInsteadOfMenu");
            item.Status.Should().Be(SequenceEntityStatus.CREATED);
            scope.History.Position.Should().Be(2);
            scope.History.Undo().Should().BeTrue();
            item.Status.Should().Be(SequenceEntityStatus.DISABLED);
            scope.History.Redo().Should().BeTrue();
            item.Status.Should().Be(SequenceEntityStatus.CREATED);
        }

        [Test]
        public void FocusedAttemptsEdit_RightClickClose_PreservesValueAndHistory() {
            using var scope = new CoreEditorTestScope();
            var item = (TakeExposure)scope.Create(typeof(TakeExposure));
            scope.Show(item);
            ExecuteButton(scope.Host, "ShowMenuButton");
            var menu = Named<WrapPanel>(scope.Host, "ButtonCommands")!;
            var attempts = Descendants<TextBox>(menu).Single();
            Keyboard.Focus(attempts).Should().BeSameAs(attempts);
            attempts.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice,
                new TextComposition(InputManager.Current, attempts, "3")) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
            attempts.SetCurrentValue(TextBox.TextProperty, "3");
            item.Attempts.Should().Be(1, "the focused editor has not yet updated its LostFocus binding");

            var header = (Border)Named<Grid>(scope.Host, "StackPanel")!.Parent;
            header.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right) { RoutedEvent = Mouse.PreviewMouseDownEvent });
            header.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right) { RoutedEvent = UIElement.MouseRightButtonDownEvent });
            Drain();

            item.ShowMenu.Should().BeFalse();
            Named<WrapPanel>(scope.Host, "ButtonCommands").Should().BeNull();
            item.Attempts.Should().Be(3);
            scope.History.Position.Should().Be(1);
            scope.History.Undo().Should().BeTrue();
            item.Attempts.Should().Be(1);
            scope.History.Redo().Should().BeTrue();
            item.Attempts.Should().Be(3);
            ExecuteButton(scope.Host, "ShowMenuButton");
            Descendants<TextBox>(Named<WrapPanel>(scope.Host, "ButtonCommands")!).Single().Text.Should().Be("3");
        }

        [Test]
        public void MenuErrorBehaviorEdit_CloseAndReopen_PreservesValueAndHistory() {
            using var scope = new CoreEditorTestScope();
            var item = (TakeExposure)scope.Create(typeof(TakeExposure));
            scope.Show(item);
            ExecuteButton(scope.Host, "ShowMenuButton");
            var combo = Named<ComboBox>(scope.Host, "PART_ErrorBehavior")!;
            CoreEditorHistoryTest.MouseDown(combo);
            combo.SetCurrentValue(ComboBox.SelectedItemProperty, InstructionErrorBehavior.AbortOnError);
            Drain();
            ExecuteButton(scope.Host, "ShowMenuButton");

            item.ErrorBehavior.Should().Be(InstructionErrorBehavior.AbortOnError);
            scope.History.Position.Should().Be(1);
            scope.History.Undo().Should().BeTrue();
            item.ErrorBehavior.Should().Be(InstructionErrorBehavior.ContinueOnError);
            scope.History.Redo().Should().BeTrue();
            item.ErrorBehavior.Should().Be(InstructionErrorBehavior.AbortOnError);
            ExecuteButton(scope.Host, "ShowMenuButton");
            Named<ComboBox>(scope.Host, "PART_ErrorBehavior")!.SelectedItem.Should().Be(item.ErrorBehavior);
        }

        [Test]
        public void ClosedMenu_ErrorIndicatorTracksEveryBehaviorInBothDirections() {
            using var scope = new CoreEditorTestScope();
            var item = (TakeExposure)scope.Create(typeof(TakeExposure));
            scope.Show(item);
            var view = Descendants<SequenceBlockView>(scope.Host).Single();
            var indicator = Descendants<ContentControl>(view).Single(control => control.Height == 25
                && control.HorizontalAlignment == HorizontalAlignment.Center && control.ToolTip is TextBlock);
            var behaviors = Enum.GetValues<InstructionErrorBehavior>();

            foreach (var behavior in behaviors.Concat(behaviors.Reverse())) {
                item.ErrorBehavior = behavior;
                Drain();
                string? icon = behavior switch {
                    InstructionErrorBehavior.SkipInstructionSetOnError => "SkipSVG",
                    InstructionErrorBehavior.AbortOnError => "StopSVG",
                    InstructionErrorBehavior.SkipToSequenceEndInstructions => "FlagFinishSVG",
                    _ => null
                };
                indicator.Visibility.Should().Be(icon == null ? Visibility.Collapsed : Visibility.Visible);
                if (icon == null) indicator.Content.Should().BeNull();
                else {
                    var paths = Descendants<Path>((DependencyObject)indicator.Content).ToArray();
                    paths.Should().HaveCount(2);
                    paths[1].Data.ToString().Should().Be(((Geometry)view.FindResource(icon)).ToString());
                }
                item.ShowMenu.Should().BeFalse();
                Named<WrapPanel>(view, "ButtonCommands").Should().BeNull();
            }
        }

        private static T? Named<T>(DependencyObject root, string name) where T : FrameworkElement =>
            Descendants<T>(root).SingleOrDefault(element => element.Name == name);

        private static void ExecuteButton(DependencyObject root, string name) {
            var button = Named<Button>(root, name)!;
            button.Should().NotBeNull();
            button.IsVisible.Should().BeTrue();
            button.Command.Should().NotBeNull();
            button.Command.CanExecute(button.CommandParameter).Should().BeTrue();
            button.Command.Execute(button.CommandParameter);
            Drain();
        }
    }
}
