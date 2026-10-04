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
using Microsoft.Xaml.Behaviors;
using NINA.Astrometry;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Editing;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static NINA.Test.Sequencer.Editing.CoreEditorTestScope;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class TargetDropHistoryTest {
        [Test]
        public void RemovedDso_IsCollectedAfterHistoryReleasesIt(
            [Values(false, true)] bool showView,
            [Values("disabled", "clear", "evict", "dispose")] string historyRelease) {
            using var scope = new CoreEditorTestScope();
            if (historyRelease == "disabled") scope.History.IsEnabled = false;
            var removed = CreateAndRemoveTarget(scope, showView, retainCommand: false);

            CollectAfterDispatcherCleanup();
            removed.Target.IsAlive.Should().Be(historyRelease != "disabled", "only undo history should retain the removed target");
            if (historyRelease == "clear") scope.History.Clear();
            else if (historyRelease == "dispose") scope.History.Dispose();
            else if (historyRelease == "evict") {
                for (int i = 0; i < 100; i++) {
                    var edit = SequencePropertyCapture.Capture("Title", () => scope.Root.SequenceTitle, title => scope.Root.SequenceTitle = title);
                    scope.Root.SequenceTitle = $"Title {i}";
                    scope.History.RecordApplied(edit.Complete());
                }
            }

            CollectAfterDispatcherCleanup();
            removed.Target.IsAlive.Should().BeFalse();
            removed.Child.IsAlive.Should().BeFalse();
            GC.KeepAlive(scope);
        }

        [Test]
        public void RetainedDropCommand_DoesNotRetainRemovedDso() {
            using var scope = new CoreEditorTestScope();
            scope.History.IsEnabled = false;
            var removed = CreateAndRemoveTarget(scope, showView: false, retainCommand: true);

            CollectAfterDispatcherCleanup();

            removed.Target.IsAlive.Should().BeFalse("a retained weak command must not own the removed container");
            removed.Child.IsAlive.Should().BeFalse();
            GC.KeepAlive(removed.Command);
            GC.KeepAlive(scope);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference Target, WeakReference Child, ICommand? Command) CreateAndRemoveTarget(
            CoreEditorTestScope scope, bool showView, bool retainCommand) {
            var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
            var child = new WaitForTimeSpan();
            target.Add(child);
            scope.Root.Add(target);
            if (showView) {
                scope.Host.Content = new TreeView { ItemsSource = scope.Root.Items };
                Drain();
                Descendants<NINA.View.Sequencer.HierarchicalSequenceContainerView>(scope.Host)
                    .Should().Contain(view => ReferenceEquals(view.DataContext, target));
            }
            ICommand? command = retainCommand ? target.DropTargetCommand : null;
            target.DetachCommand.Execute(null);
            Drain();
            scope.Root.Items.Should().BeEmpty();
            Descendants<NINA.View.Sequencer.HierarchicalSequenceContainerView>(scope.Host).Should().BeEmpty();
            return (new WeakReference(target), new WeakReference(child), command);
        }

        private static void CollectAfterDispatcherCleanup() {
            Drain();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Drain();
            GC.Collect();
        }

        [TestCase(false, 1, 20, 10, 3, -40, 50)]
        [TestCase(true, 1, 20, 10, 3, -40, 50)]
        [TestCase(false, 0, 0, 0, 3, -40, 0)]
        [TestCase(true, 0, 0, 0, 3, -40, 0)]
        [TestCase(false, 1, 20, 0, 0, 0, 0)]
        [TestCase(true, 1, 20, 0, 0, 0, 0)]
        public void CompiledDsoTargetDrop_AfterCollection_UpdatesTargetAndSupportsUndoRedo(
            bool clone, double beforeRa, double beforeDec, double beforeRotation, double afterRa, double afterDec, double afterRotation) {
            using var scope = new CoreEditorTestScope();
            var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
            target.Name = "Before";
            target.Target.TargetName = "Before";
            target.Target.InputCoordinates.Coordinates = new Coordinates(beforeRa, beforeDec, Epoch.J2000, Coordinates.RAType.Hours);
            target.Target.PositionAngle = beforeRotation;
            if (clone) target = (DeepSkyObjectContainer)target.Clone();
            var slew = (SlewScopeToRaDec)scope.Create(typeof(SlewScopeToRaDec));
            target.Add(slew);
            var source = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
            source.Target.TargetName = "After";
            source.Target.InputCoordinates.Coordinates = new Coordinates(afterRa, afterDec, Epoch.J2000, Coordinates.RAType.Hours);
            source.Target.PositionAngle = afterRotation;
            var saved = new TargetSequenceContainer((IProfileService)Application.Current.Resources["ProfileService"], source);
            scope.Show(target);
            var drop = Descendants<FrameworkElement>(scope.Host)
                .SelectMany(element => Interaction.GetBehaviors(element).OfType<DropIntoBehavior>())
                .Single(behavior => behavior.OnDropCommand == nameof(DeepSkyObjectContainer.DropTargetCommand));

            // A user can drop long after construction and the initial nighttime calculation.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            drop.ExecuteDropInto(new DropIntoParameters(saved));

            AssertTarget("After", afterRa, afterDec, afterRotation);
            target.Target.InputCoordinates.Should().NotBeSameAs(source.Target.InputCoordinates);
            scope.History.Position.Should().Be(1);
            for (int repeat = 0; repeat < 2; repeat++) {
                scope.History.Undo().Should().BeTrue();
                AssertTarget("Before", beforeRa, beforeDec, beforeRotation);
                scope.History.Redo().Should().BeTrue();
                AssertTarget("After", afterRa, afterDec, afterRotation);
            }
            source.Target.TargetName.Should().Be("After");
            source.Target.InputCoordinates.Coordinates.RA.Should().Be(afterRa);
            source.Target.InputCoordinates.Coordinates.Dec.Should().Be(afterDec);

            void AssertTarget(string name, double ra, double dec, double rotation) {
                Drain();
                target.Name.Should().Be(name);
                target.Target.TargetName.Should().Be(name);
                target.Target.InputCoordinates.Coordinates.RA.Should().Be(ra);
                target.Target.InputCoordinates.Coordinates.Dec.Should().Be(dec);
                target.Target.PositionAngle.Should().Be(rotation);
                target.Target.DeepSkyObject.Coordinates.RA.Should().Be(ra);
                target.Target.DeepSkyObject.Coordinates.Dec.Should().Be(dec);
                slew.Inherited.Should().BeTrue();
                slew.Coordinates.Coordinates.RA.Should().Be(ra);
                slew.Coordinates.Coordinates.Dec.Should().Be(dec);
                Descendants<TextBlock>(scope.Host).Should().Contain(text => text.Text == target.Target.InputCoordinates.Coordinates.RAString);
                Descendants<TextBlock>(scope.Host).Should().Contain(text => text.Text == target.Target.InputCoordinates.Coordinates.DecString);
                Descendants<TextBox>(scope.Host).Single(text => ReferenceEquals(text.DataContext, target.Target.InputCoordinates)
                    && BindingOperations.GetBinding(text, TextBox.TextProperty)?.Path.Path == nameof(InputCoordinates.RAHours))
                    .Text.Should().Be(target.Target.InputCoordinates.RAHours.ToString(CultureInfo.CurrentCulture));
            }
        }
    }
}