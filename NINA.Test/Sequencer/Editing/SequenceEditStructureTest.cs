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
        public void ClearSequence_RecordsOnlyConfirmedChangeAndDoesNotRepeatDialog(bool confirm) {
            Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            Window previous = app.MainWindow;
            var window = new Window { Width = 300, Height = 200, Left = -32000, Top = -32000, ShowActivated = false, ShowInTaskbar = false };
            foreach (string name in new[] { "ProfileService", "SVGDictionary", "Brushes", "Converters" }) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"/NINA.WPF.Base;component/Resources/StaticResources/{name}.xaml", UriKind.Relative) });
            }
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/Styles/Button.xaml", UriKind.Relative) });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/NINA.WPF.Base;component/Resources/Styles/Path.xaml", UriKind.Relative) });
            var root = new SequenceRootContainer { SequenceTitle = "My sequence" };
            var areas = new[] { new SequentialContainer(), new SequentialContainer(), new SequentialContainer() };
            var item = new SequenceEditHistoryTest.PluginItem();
            foreach (var area in areas) root.Add(area);
            areas[1].Add(item);
            var trigger = new UnknownSequenceTrigger("Root trigger");
            root.Add(trigger);
            using var history = new SequenceEditHistory(root);
            try {
                app.MainWindow = window;
                window.Show();
                window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                    var dialog = app.Windows.OfType<NINA.Core.MyMessageBox.MyMessageBoxView>().FirstOrDefault();
                    if (dialog != null) dialog.DialogResult = confirm;
                }));
                root.DetachCommand.Execute(null);
                history.Position.Should().Be(confirm ? 1 : 0);
                if (confirm) {
                    areas[1].Items.Should().BeEmpty();
                    root.Triggers.Should().BeEmpty();
                    history.Undo().Should().BeTrue();
                    areas[1].Items.Should().ContainSingle().Which.Should().BeSameAs(item);
                    root.Triggers.Single().Should().BeSameAs(trigger);
                    root.SequenceTitle.Should().Be("My sequence");
                    history.Redo().Should().BeTrue();
                    areas[1].Items.Should().BeEmpty();
                } else {
                    areas[1].Items.Single().Should().BeSameAs(item);
                }
            } finally { window.Close(); app.MainWindow = previous; }
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