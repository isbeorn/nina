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
using Moq;
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Editing;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Utility;
using NUnit.Framework;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequenceEditStructureTest {
        [Test]
        public void CustomTriggerSource_MoveReplaceDelete_UndoRedoOriginalInstances() {
            var resources = new Mock<IApplicationResourceDictionary>();
            resources.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());
            var root = new SequenceRootContainer();
            var custom = new CustomTrigger(resources.Object);
            var source = new UnknownSequenceTrigger("Source");
            root.Add(source);
            root.Add(custom);
            using var history = new SequenceEditHistory(root);
            custom.DropIntoTriggerSourceCommand.Execute(new DropIntoParameters(source));
            custom.TriggerSource.Should().BeSameAs(source);
            history.Undo().Should().BeTrue();
            root.Triggers.Should().Equal(source, custom);
            custom.TriggerSource.Should().BeNull();
            history.Redo().Should().BeTrue();
            custom.TriggerSource.Should().BeSameAs(source);
            source.DetachCommand.Execute(null);
            history.Undo().Should().BeTrue();
            custom.TriggerSource.Should().BeSameAs(source);
            var replacement = new UnknownSequenceTrigger("Replacement");
            custom.DropIntoTriggerSourceCommand.Execute(new DropIntoParameters(replacement));
            ISequenceTrigger actual = custom.TriggerSource;
            history.Undo().Should().BeTrue();
            custom.TriggerSource.Should().BeSameAs(source);
            history.Redo().Should().BeTrue();
            custom.TriggerSource.Should().BeSameAs(actual);
        }

        [Test]
        public void PlacementConflict_DoesNotSkipEditOrOverwriteExternalStructure() {
            var root = new SequenceRootContainer();
            var item = new SequenceEditHistoryTest.PluginItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            item.DetachCommand.Execute(null);
            var external = new SequenceEditHistoryTest.PluginItem();
            root.Add(external);
            history.Undo().Should().BeFalse();
            history.Position.Should().Be(1);
            root.Items.Should().ContainSingle().Which.Should().BeSameAs(external);
        }

        [Test]
        public void MoveIntoTargetAndBack_RestoresExplicitCoordinateDefinitionsAndRotation() {
            using var profile = new NINA.Profile.Profile();
            var profiles = new Mock<NINA.Profile.Interfaces.IProfileService>();
            profiles.SetupGet(x => x.ActiveProfile).Returns(profile);
            var target = new DeepSkyObjectContainer(profiles.Object, Mock.Of<NINA.Astrometry.Interfaces.INighttimeCalculator>(),
                Mock.Of<NINA.WPF.Base.Interfaces.ViewModel.IFramingAssistantVM>(), Mock.Of<NINA.WPF.Base.Interfaces.Mediator.IApplicationMediator>(),
                Mock.Of<NINA.Equipment.Interfaces.IPlanetariumFactory>(), Mock.Of<NINA.Equipment.Interfaces.Mediator.ICameraMediator>(),
                Mock.Of<NINA.Equipment.Interfaces.Mediator.IFilterWheelMediator>(), Mock.Of<NINA.Sequencer.Logic.ISymbolBroker>());
            var root = new SequenceRootContainer();
            var first = new SequentialContainer();
            root.Add(first);
            root.Add(target);
            var item = new NINA.Sequencer.SequenceItem.Telescope.CoordinatesInstruction();
            first.Add(item);
            item.RaExpression.Definition = "1+1";
            item.DecExpression.Definition = "3+4";
            item.PositionAngleExpression.Definition = "20+10";
            using var history = new SequenceEditHistory(root);
            history.CaptureStructure("Move", () => target.Add(item));
            item.Inherited.Should().BeTrue();
            history.Undo().Should().BeTrue();
            item.Parent.Should().BeSameAs(first);
            item.Inherited.Should().BeFalse();
            item.RaExpression.Definition.Should().Be("1+1");
            item.DecExpression.Definition.Should().Be("3+4");
            item.PositionAngleExpression.Definition.Should().Be("20+10");
            history.Redo().Should().BeTrue();
            item.Parent.Should().BeSameAs(target);
            item.Inherited.Should().BeTrue();
        }

        [Test]
        public void MoveSymbolIntoOccupiedScope_RestoresOriginalIdentifierOnUndo() {
            var root = new SequenceRootContainer();
            var first = new SequentialContainer();
            var second = new SequentialContainer();
            root.Add(first);
            root.Add(second);
            var variable = new NINA.Sequencer.SequenceItem.Expressions.Variable("Collision", "1", null);
            first.Add(variable);
            second.Add(new NINA.Sequencer.SequenceItem.Expressions.Variable("Collision", "2", null));
            using var history = new SequenceEditHistory(root);
            history.CaptureStructure("Move", () => second.Add(variable));
            string renamed = variable.Identifier;
            renamed.Should().NotBe("Collision");
            history.Undo().Should().BeTrue();
            variable.Identifier.Should().Be("Collision");
            history.Redo().Should().BeTrue();
            variable.Identifier.Should().Be(renamed);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClearSequence_ClearsContentsAndHistoryOnlyWhenConfirmed(bool confirm) {
            using var scope = new CoreEditorTestScope();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("/NINA.WPF.Base;component/Resources/Styles/Path.xaml", UriKind.Relative)
            });
            var root = scope.Root;
            root.SequenceTitle = "My sequence";
            SequenceContainer[] areas = { new StartAreaContainer(), new TargetAreaContainer(), new EndAreaContainer() };
            foreach (var area in areas) {
                root.Add(area);
                area.Add(new SequenceEditHistoryTest.PluginItem());
                area.Add(new NINA.Sequencer.Conditions.LoopCondition());
                area.Add(new UnknownSequenceTrigger("Area trigger"));
            }
            root.Add(new NINA.Sequencer.Conditions.LoopCondition());
            root.Add(new UnknownSequenceTrigger("Root trigger"));
            var item = (SequenceEditHistoryTest.PluginItem)areas[1].Items.Single();
            var history = scope.History;
            item.DisableEnableCommand.Execute(null);
            item.DisableEnableCommand.Execute(null);
            history.Undo().Should().BeTrue();
            var entries = history.Entries.ToArray();
            root.SetChanged("Exposures");
            root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
            history.CanUndo.Should().BeTrue();
            history.CanRedo.Should().BeTrue();

            scope.Window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                var dialog = Application.Current.Windows.OfType<NINA.Core.MyMessageBox.MyMessageBoxView>().FirstOrDefault();
                if (dialog != null) dialog.DialogResult = confirm;
            }));
            root.DetachCommand.Execute(null);

            root.Items.Should().Equal(areas);
            if (confirm) {
                root.Conditions.Should().BeEmpty();
                root.Triggers.Should().BeEmpty();
                root.HasChanges.Values.Should().OnlyContain(changed => !changed);
                foreach (var area in areas) {
                    area.Items.Should().BeEmpty();
                    area.Conditions.Should().BeEmpty();
                    area.Triggers.Should().BeEmpty();
                }
                history.Entries.Should().ContainSingle();
                history.Position.Should().Be(0);
                history.CanUndo.Should().BeFalse();
                history.CanRedo.Should().BeFalse();
                history.Undo().Should().BeFalse();
                history.Redo().Should().BeFalse();

                var replacement = new SequenceEditHistoryTest.PluginItem();
                areas[1].Add(replacement);
                replacement.DisableEnableCommand.Execute(null);
                root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
                history.Undo().Should().BeTrue("the existing root and editor history remain usable");
                replacement.Status.Should().Be(NINA.Core.Enum.SequenceEntityStatus.CREATED);
                history.Redo().Should().BeTrue();
                replacement.Status.Should().Be(NINA.Core.Enum.SequenceEntityStatus.DISABLED);
            } else {
                root.SequenceTitle.Should().Be("My sequence");
                root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().BeTrue();
                root.DoesHaveChanges("Exposures").Should().BeTrue();
                root.Conditions.Should().ContainSingle();
                root.Triggers.Should().ContainSingle();
                foreach (var area in areas) {
                    area.Items.Should().ContainSingle();
                    area.Conditions.Should().ContainSingle();
                    area.Triggers.Should().ContainSingle();
                }
                areas[1].Items.Single().Should().BeSameAs(item);
                history.Entries.Should().Equal(entries);
                history.Position.Should().Be(1);
                history.CanUndo.Should().BeTrue();
                history.CanRedo.Should().BeTrue();
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ClearSequence_EmptySequenceStartsCleanOnlyWhenConfirmed(bool confirm, bool initiallyChanged) {
            using var scope = new CoreEditorTestScope();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("/NINA.WPF.Base;component/Resources/Styles/Path.xaml", UriKind.Relative)
            });
            var root = scope.Root;
            root.Add(new StartAreaContainer());
            root.Add(new TargetAreaContainer());
            root.Add(new EndAreaContainer());
            root.SequenceTitle = "Saved empty sequence";
            scope.History.MarkSaved();
            root.HasChanges[SequenceEntityINPC.defaultChangeSet] = initiallyChanged;
            root.HasChanges["Exposures"] = initiallyChanged;

            scope.Window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                var dialog = Application.Current.Windows.OfType<NINA.Core.MyMessageBox.MyMessageBoxView>().FirstOrDefault();
                if (dialog != null) dialog.DialogResult = confirm;
            }));
            root.DetachCommand.Execute(null);

            root.SequenceTitle.Should().Be(confirm
                ? NINA.Core.Locale.Loc.Instance["Lbl_SequenceContainer_SequenceRootContainer_Name"]
                : "Saved empty sequence");
            root.DoesHaveChanges(SequenceEntityINPC.defaultChangeSet).Should().Be(!confirm && initiallyChanged);
            root.DoesHaveChanges("Exposures").Should().Be(!confirm && initiallyChanged);
            scope.History.CanUndo.Should().BeFalse();
            scope.History.CanRedo.Should().BeFalse();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LinkedTargetOverride_RestoresNullAndWholeCoordinates(bool signOnly) {
            var root = new SequenceRootContainer();
            var linked = new LinkedTemplateContainer();
            root.Add(linked);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = linked };
            box.SetBinding(TextBox.TextProperty, new Binding("TargetEditor.InputCoordinates.RASeconds"));
            var capture = SequencePropertyCapture.Create(linked, box.GetBindingExpression(TextBox.TextProperty));
            if (signOnly) linked.TargetEditor.InputCoordinates.NegativeDec = true;
            else linked.TargetEditor.InputCoordinates.Coordinates = new NINA.Astrometry.Coordinates(23.99, -89.99, NINA.Astrometry.Epoch.J2000, NINA.Astrometry.Coordinates.RAType.Hours);
            history.RecordApplied(capture!.Complete());
            history.Undo().Should().BeTrue();
            linked.TargetOverride.Should().BeNull();
            history.Redo().Should().BeTrue();
            linked.TargetEditor.InputCoordinates.NegativeDec.Should().BeTrue();
            if (!signOnly) linked.TargetOverride.InputCoordinates.Coordinates.Dec.Should().Be(-89.99);
        }
    }
}