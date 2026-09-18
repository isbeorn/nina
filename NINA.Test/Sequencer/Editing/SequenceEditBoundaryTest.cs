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
using NINA.Astrometry;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Editing;
using NUnit.Framework;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequenceEditBoundaryTest {
        private static LinkedTemplateContainer OpenTemplate(CoreEditorTestScope scope) {
            var template = new SequentialContainer();
            template.Add(new SequenceEditHistoryTest.PluginItem());
            var linked = new LinkedTemplateContainer();
            linked.MaterializeFromTemplate(new TemplatedSequenceContainer(
                (IProfileService)Application.Current.Resources["ProfileService"], "Test", template), true);
            scope.Root.Add(linked);
            linked.BeginEditTemplateCommand.Execute(null);
            linked.IsEditing.Should().BeTrue();
            return linked;
        }

        [Test]
        public void EditingTemplateContainer_DeleteBelongsToParentHistory() {
            using var scope = new CoreEditorTestScope();
            var linked = OpenTemplate(scope);
            var originalSession = scope.History.ActiveHistory;
            linked.DetachCommand.Execute(null);
            originalSession.IsEnabled.Should().BeFalse();
            scope.Root.Items.Should().NotContain(linked);
            scope.History.Position.Should().Be(1, "removing the template instance changes the parent sequence");
            scope.History.Undo().Should().BeTrue();
            scope.Root.Items.Should().ContainSingle().Which.Should().BeSameAs(linked);
            var contents = scope.History.ForContents(linked);
            contents.Should().NotBeSameAs(originalSession);
            scope.History.Redo().Should().BeTrue();
            contents.IsEnabled.Should().BeFalse("structural replay also disposes detached sessions");
            scope.History.Undo().Should().BeTrue();
            scope.Root.Items.Should().ContainSingle().Which.Should().BeSameAs(linked);
        }

        [Test]
        public void LinkedTargetDrop_UndoPreservesEditorNegativeZero() {
            using var scope = new CoreEditorTestScope();
            var linked = new LinkedTemplateContainer();
            var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
            linked.MaterializeFromTemplate(new TemplatedSequenceContainer(
                (IProfileService)Application.Current.Resources["ProfileService"], "Test", target), true);
            scope.Root.Add(linked);
            linked.TargetEditor.TargetName = "Before";
            linked.TargetEditor.InputCoordinates.Coordinates = new Coordinates(1, 0, Epoch.J2000, Coordinates.RAType.Hours);
            linked.TargetEditor.InputCoordinates.NegativeDec = true;
            target.Target.TargetName = "After";
            target.Target.InputCoordinates.Coordinates = new Coordinates(2, 0, Epoch.J2000, Coordinates.RAType.Hours);
            linked.DropTargetCommand.Execute(target);
            scope.History.Undo().Should().BeTrue();
            linked.TargetEditor.InputCoordinates.NegativeDec.Should().BeTrue();
            scope.History.Redo().Should().BeTrue();
            linked.TargetEditor.InputCoordinates.NegativeDec.Should().BeFalse();
            linked.TargetEditor.TargetName.Should().Be("After");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CustomEditorContext_IsResolvedForTheEditorOwner(bool insideTemplate) {
            using var scope = new CoreEditorTestScope();
            var linked = OpenTemplate(scope);
            var nested = scope.History.ActiveHistory;
            var outside = new SequenceEditHistoryTest.PluginItem();
            scope.Root.Add(outside);
            var inside = ((SequenceContainer)linked.Items.Single()).Items.Single();
            if (insideTemplate) scope.History.ForOwner(outside);
            var editor = new Button { DataContext = insideTemplate ? inside : outside };
            SequenceEditContext.SetHistory(editor, scope.History);
            SequenceEditContext.SetIsRecordingEnabled(editor, false);
            SequenceEditContext.GetHistory(editor).Should().BeSameAs(insideTemplate ? nested : scope.History,
                "custom editor recording must not depend on the previously active field");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WholeTargetAssignment_RestoresNegativeZero(bool undo) {
            using var scope = new CoreEditorTestScope();
            var target = (DeepSkyObjectContainer)scope.Create(typeof(DeepSkyObjectContainer));
            scope.Root.Add(target);
            target.Target.InputCoordinates.Coordinates = new Coordinates(1, 0, Epoch.J2000, Coordinates.RAType.Hours);
            target.Target.InputCoordinates.NegativeDec = undo;
            SequenceEditContext.Target(target, () => {
                target.Target.InputCoordinates.Coordinates = new Coordinates(2, 0, Epoch.J2000, Coordinates.RAType.Hours);
                target.Target.InputCoordinates.NegativeDec = !undo;
            });
            scope.History.Undo().Should().BeTrue();
            if (!undo) scope.History.Redo().Should().BeTrue();
            target.Target.InputCoordinates.NegativeDec.Should().BeTrue("whole target edits must preserve negative zero");
        }
    }
}