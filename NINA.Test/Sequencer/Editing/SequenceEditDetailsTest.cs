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
using NINA.Core.Locale;
using NINA.Sequencer.Container;
using NINA.Sequencer.Editing;
using NINA.Sequencer.Utility;
using NUnit.Framework;
using System.Windows.Controls;
using System.Windows.Data;
using Item = NINA.Test.Sequencer.Editing.SequenceEditHistoryTest.PluginItem;

namespace NINA.Test.Sequencer.Editing {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class SequenceEditDetailsTest {
        [Test]
        public void FieldEdit_ShowsItemPathPositionAndValues_WithoutFollowingLaterRenames() {
            var root = new SequenceRootContainer { SequenceTitle = "Night sequence" };
            var target = new SequentialContainer { Name = "M31" };
            root.Add(target);
            target.Add(new Item { Name = "Smart Exposure" });
            var item = new Item { Name = "Smart Exposure", Setting = 60 };
            target.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(item.Setting)));
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            item.Setting = 180;
            history.RecordApplied(capture!.Complete());

            string details = history.Entries.Last().Details;
            details.Should().Contain("Night sequence > M31 > Smart Exposure (#2)").And.Contain("60 -> 180");
            history.Undo().Should().BeTrue();
            history.Redo().Should().BeTrue();
            target.Name = "Renamed target";
            item.Name = "Renamed instruction";
            item.DetachCommand.Execute(null);
            history.Entries[1].Details.Should().Be(details);
        }

        [Test]
        public void ExpressionEdit_ShowsDefinitionAndFieldRatherThanAnEvaluatedResult() {
            var root = new SequenceRootContainer { SequenceTitle = "Night sequence" };
            var item = new Item { Name = "Exposure" };
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var box = new TextBox { DataContext = item };
            box.SetBinding(TextBox.TextProperty, new Binding("Formula.Definition"));
            var capture = SequencePropertyCapture.Create(item, box.GetBindingExpression(TextBox.TextProperty));
            item.Formula.Definition = "30*6";
            history.RecordApplied(capture!.Complete());
            history.Entries.Last().Description.Should().Contain("Formula").And.NotContain("Definition");
            history.Entries.Last().Details.Should().Contain("1 -> 30*6");
        }

        [Test]
        public void StructureDetails_NameTheActualItemAndLocations_IgnoreIncidentalIndexShifts() {
            var root = new SequenceRootContainer { SequenceTitle = "Night sequence" };
            var first = new SequentialContainer { Name = "M31" };
            var second = new SequentialContainer { Name = "M42" };
            root.Add(first); root.Add(second);
            var item = new Item { Name = "Smart Exposure" };
            var sibling = new Item { Name = "Dither" };
            first.Add(item); first.Add(sibling);
            using var history = new SequenceEditHistory(root);
            history.CaptureStructure("Move", () => { first.Remove(item); second.Add(item); });
            string details = history.Entries.Last().Details;
            details.Should().Contain("Smart Exposure").And.Contain("M31 (#1) -> Night sequence > M42 (#1)").And.NotContain("Dither");
            item.DetachCommand.Execute(null);
            history.Entries.Last().Details.Should().Contain("Removed Smart Exposure").And.Contain("M42 (#1)");
            history.Undo().Should().BeTrue();
            item.AddCloneToParentCommand.Execute(null);
            history.Entries.Last().Details.Should().Contain("Added").And.Contain("M42 (#2)");
        }

        [Test]
        public void BulkAndCompositeEdits_KeepUsefulDetailsBounded() {
            var root = new SequenceRootContainer { SequenceTitle = "Night sequence" };
            for (int i = 0; i < 8; i++) root.Add(new Item { Name = $"Instruction {i + 1}" });
            using var history = new SequenceEditHistory(root);
            history.CaptureStructure("Clear", () => {
                foreach (var item in root.GetItemsSnapshot()) root.Remove(item);
            });
            history.Entries.Last().Details.Should().Contain("Instruction 1").And.Contain("Instruction 5").And.Contain("+ 3 more changes").And.NotContain("Instruction 6");
            history.Undo().Should().BeTrue();
            using (history.BeginTransaction("Disable two items")) {
                ((Item)root.Items[0]).DisableEnableCommand.Execute(null);
                ((Item)root.Items[1]).DisableEnableCommand.Execute(null);
            }
            history.Entries.Last().Details.Should().Contain("Instruction 1").And.Contain("Instruction 2").And.Contain("Enabled -> Disabled");
        }

        [Test]
        public void SelectionCaption_IsFrozenBeforeMutableOptionChanges() {
            var root = new SequenceRootContainer();
            var item = new SelectionItem();
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var combo = new ComboBox { DataContext = item };
            combo.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(item.Option)));
            var capture = SequencePropertyCapture.Create(item, combo.GetBindingExpression(ComboBox.SelectedItemProperty));
            item.Option.Name = "Changed outside the editor";
            item.Option = new Option { Name = "Red" };
            history.RecordApplied(capture!.Complete());
            history.Entries.Last().Details.Should().Contain("Luminance -> Red").And.NotContain("Changed outside");
        }

        [Test]
        public void SelectionCaption_UsesTheEditorsDisplayMemberAndPreservesReplayIdentity() {
            var root = new SequenceRootContainer();
            var before = new CaptionedMode { Caption = new Option { Name = "Fast readout" } };
            var after = new CaptionedMode { Caption = new Option { Name = "Low noise" } };
            var item = new ReadoutItem { Mode = before };
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var combo = new ComboBox { DataContext = item, DisplayMemberPath = "Caption.Name" };
            combo.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(item.Mode)));
            var capture = SequencePropertyCapture.Create(item, combo.GetBindingExpression(ComboBox.SelectedItemProperty));
            before.Caption.Name = "Renamed later";
            item.Mode = after;
            history.RecordApplied(capture!.Complete());

            history.Entries.Last().Details.Should().Contain("Fast readout -> Low noise").And.NotContain("NINA.");
            history.Undo().Should().BeTrue();
            item.Mode.Should().BeSameAs(before);
            history.Redo().Should().BeTrue();
            item.Mode.Should().BeSameAs(after);
        }

        [Test]
        public void SelectionWithoutCaption_ShowsReadableNamesInsteadOfNamespaces() {
            var root = new SequenceRootContainer();
            var item = new ReadoutItem { Mode = new FastReadoutMode() };
            root.Add(item);
            using var history = new SequenceEditHistory(root);
            var combo = new ComboBox { DataContext = item };
            combo.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(item.Mode)));
            var capture = SequencePropertyCapture.Create(item, combo.GetBindingExpression(ComboBox.SelectedItemProperty));
            item.Mode = new LowNoiseReadoutMode();
            history.RecordApplied(capture!.Complete());

            history.Entries.Last().Details.Should().Contain("Fast Readout Mode -> Low Noise Readout Mode").And.NotContain("NINA.");
        }

        [Test]
        public void Captions_UseExistingLabelsAndPreserveLiteralUserText() {
            SequenceEditDetails.Value(InstructionErrorBehavior.SkipToSequenceEndInstructions).Should().Be(Loc.Instance["LblSkipToSequenceEndInstructions"]);
            SequenceEditDetails.Name(new SequentialContainer()).Should().Be(Loc.Instance["Lbl_SequenceContainer_SequentialContainer_Name"]);
            SequenceEditDetails.Value(new DisplayNamedMode()).Should().Be("Quiet readout");
            SequenceEditDetails.Value(new LabelledMode()).Should().Be("Custom readout");
            SequenceEditDetails.Value("NINA.Sequencer.UserExpression").Should().Be("NINA.Sequencer.UserExpression");
            SequenceEditDetails.Value(@"C:\Sequences\M31.v2.json").Should().Be(@"C:\Sequences\M31.v2.json");
        }

        public class Option { public string Name { get; set; } = "Luminance"; }
        public class SelectionItem : Item { public Option Option { get; set; } = new(); }
        public class ReadoutItem : Item { public object Mode { get; set; } = new FastReadoutMode(); }
        public class CaptionedMode { public Option Caption { get; set; } = new(); }
        public class FastReadoutMode { }
        public class LowNoiseReadoutMode { }
        public class DisplayNamedMode { public string DisplayName => "Quiet readout"; }
        public class LabelledMode { public override string ToString() => "Custom readout"; }
    }
}