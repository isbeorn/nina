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
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SequenceItemBase = NINA.Sequencer.SequenceItem.SequenceItem;

namespace NINA.Test.Sequencer.Container {

    [TestFixture]
    public class LinkedTemplateContainerTest {
        private NINA.Profile.Profile profile;
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void SetUp() {
            profile = new NINA.Profile.Profile();
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
        }

        [TearDown]
        public void TearDown() {
            profile?.Dispose();
        }

        [Test]
        public void TryResolveTemplate_MaterializesTemplateContentAndRefreshesLatestVersion() {
            TemplateReference reference = CreateReference("Night/Luminance.template.json", "Luminance");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };

            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "Luminance", "Original item") }, true, null);

            sut.TryResolveTemplate().Should().BeTrue();

            ISequenceContainer materialized = sut.Items.Should().ContainSingle().Subject.Should().BeAssignableTo<ISequenceContainer>().Subject;
            materialized.Name.Should().Be("Luminance");
            materialized.Items.Should().ContainSingle(i => i.Name == "Original item");
            sut.LinkState.Should().Be(TemplateLinkState.Resolved);

            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "Luminance", "Updated item") }, true, null);
            sut.TryResolveTemplate().Should().BeTrue();

            ISequenceContainer refreshed = sut.Items.Should().ContainSingle().Subject.Should().BeAssignableTo<ISequenceContainer>().Subject;
            refreshed.Items.Should().ContainSingle(i => i.Name == "Updated item");
        }

        [Test]
        public void LinkedTemplateContent_IsPreviewOnlyUntilEditModeIsEnabled() {
            TemplateReference reference = CreateReference("Readonly.template.json", "Readonly");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "Readonly", "Existing item") }, true, null);
            sut.TryResolveTemplate();
            SequenceContainer materialized = sut.Items.OfType<SequenceContainer>().Single();

            materialized.Items.Should().ContainSingle(i => i.Name == "Existing item");
            sut.IsEditing.Should().BeFalse();
            sut.BeginEditTemplateCommand.CanExecute(null).Should().BeTrue();
            sut.HeaderText.Should().Be(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_HeaderReadonly"]);

            sut.BeginEditTemplateCommand.Execute(null);
            sut.IsEditing.Should().BeTrue();
            sut.HeaderText.Should().Be(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_HeaderEditing"]);
            materialized.DropIntoCommand.Execute(new DropIntoParameters(new NamedInstruction("Allowed"), null, DropTargetEnum.Center));

            materialized.Items.Should().HaveCount(2);
            materialized.Items.Should().Contain(i => i.Name == "Allowed");
        }

        [Test]
        public void MoveDown_DoesNotPlaceSiblingInsideLinkedTemplateWrapper() {
            TemplateReference reference = CreateReference("ReadonlyMoveDown.template.json", "ReadonlyMoveDown");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer linkedTemplate = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "ReadonlyMoveDown", "Template item") }, true, null);
            linkedTemplate.TryResolveTemplate();
            SequenceItemBase outside = new NamedInstruction("Outside");
            SequentialContainer parent = new SequentialContainer();
            parent.Add(outside);
            parent.Add(linkedTemplate);

            parent.MoveDown(outside);

            linkedTemplate.CanAcceptSequenceItemPlacement.Should().BeFalse();
            parent.Items.Should().ContainInOrder(new ISequenceItem[] { linkedTemplate, outside });
            linkedTemplate.Items.Should().ContainSingle();
            linkedTemplate.Items.Should().NotContain(outside);
            outside.Parent.Should().BeSameAs(parent);
        }

        [Test]
        public void MoveUp_DoesNotPlaceSiblingInsideLinkedTemplateWrapper() {
            TemplateReference reference = CreateReference("ReadonlyMoveUp.template.json", "ReadonlyMoveUp");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer linkedTemplate = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "ReadonlyMoveUp", "Template item") }, true, null);
            linkedTemplate.TryResolveTemplate();
            SequenceItemBase outside = new NamedInstruction("Outside");
            SequentialContainer parent = new SequentialContainer();
            parent.Add(linkedTemplate);
            parent.Add(outside);

            parent.MoveUp(outside);

            linkedTemplate.CanAcceptSequenceItemPlacement.Should().BeFalse();
            parent.Items.Should().ContainInOrder(new ISequenceItem[] { outside, linkedTemplate });
            linkedTemplate.Items.Should().ContainSingle();
            linkedTemplate.Items.Should().NotContain(outside);
            outside.Parent.Should().BeSameAs(parent);
        }

        [Test]
        public async Task Execute_WaitsForInitialTemplateLoadAndRunsResolvedContent() {
            TemplateReference reference = CreateReference("Startup.template.json", "Startup");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            ExecutionProbe executionProbe = new ExecutionProbe();
            Task executeTask = sut.Execute(Mock.Of<IProgress<ApplicationStatus>>(), CancellationToken.None);

            await Task.Delay(100);
            executeTask.IsCompleted.Should().BeFalse();

            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "Startup", new ProbeInstruction(executionProbe)) }, true, null);
            await executeTask.WaitAsync(TimeSpan.FromSeconds(5));

            executionProbe.Executions.Should().Be(1);
            sut.LinkState.Should().Be(TemplateLinkState.Resolved);
        }

        [Test]
        public void IsExpanded_MaterializesTemplateContentOnDemand() {
            TemplateReference reference = CreateReference("Expand.template.json", "Expand");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "Expand", "Expanded item") }, true, null);
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };

            sut.Items.Should().BeEmpty();

            sut.IsExpanded = true;

            sut.Items.Should().ContainSingle();
            sut.Items.OfType<ISequenceContainer>().Single().Items.Should().ContainSingle(i => i.Name == "Expanded item");
            sut.LinkState.Should().Be(TemplateLinkState.Resolved);
        }

        [Test]
        public void SequenceJsonConverter_RoundTripsLinkedTemplateReferenceWithoutPreviewContent() {
            TemplateReference reference = CreateReference("RoundTrip.template.json", "RoundTrip");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "RoundTrip", "Preview item") }, true, null);
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            sut.TryResolveTemplate();
            SequencerFactory factory = new SequencerFactory(
                profileServiceMock.Object,
                new List<ISequenceItem> { new NamedInstruction("Prototype") },
                new List<NINA.Sequencer.Conditions.ISequenceCondition>(),
                new List<NINA.Sequencer.Trigger.ISequenceTrigger>(),
                new List<ISequenceContainer> { new LinkedTemplateContainer(resolver), new SequentialContainer() },
                new List<IDateTimeProvider>(),
                new List<ISequenceEntityUpgrader>());
            SequenceJsonConverter converter = new SequenceJsonConverter(factory);

            string json = converter.Serialize(sut);
            json.Should().NotContain("Preview item");
            ISequenceContainer roundTripped = converter.Deserialize(json);

            LinkedTemplateContainer linkedTemplateContainer = roundTripped.Should().BeOfType<LinkedTemplateContainer>().Subject;
            linkedTemplateContainer.TemplateReference.SourceKind.Should().Be(TemplateReferenceSourceKind.User);
            linkedTemplateContainer.TemplateReference.RelativePath.Should().Be("RoundTrip.template.json");
            linkedTemplateContainer.Items.Should().BeEmpty();

            linkedTemplateContainer.TryResolveTemplate().Should().BeTrue();
            linkedTemplateContainer.Items.Should().ContainSingle();
            linkedTemplateContainer.Items.OfType<ISequenceContainer>().Single().Name.Should().Be("RoundTrip");
        }

        [Test]
        public void SequenceJsonConverter_IgnoresLegacyLinkedTemplatePreviewContent() {
            TemplateReference reference = CreateReference("LegacyRoundTrip.template.json", "LegacyRoundTrip");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "LegacyRoundTrip", "Current item") }, true, null);
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            sut.TryResolveTemplate();
            SequencerFactory factory = new SequencerFactory(
                profileServiceMock.Object,
                new List<ISequenceItem> { new NamedInstruction("Prototype") },
                new List<NINA.Sequencer.Conditions.ISequenceCondition>(),
                new List<NINA.Sequencer.Trigger.ISequenceTrigger>(),
                new List<ISequenceContainer> { new LinkedTemplateContainer(resolver), new SequentialContainer() },
                new List<IDateTimeProvider>(),
                new List<ISequenceEntityUpgrader>());
            SequenceJsonConverter converter = new SequenceJsonConverter(factory);
            string legacyJson = JsonConvert.SerializeObject(sut, Formatting.Indented, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            });
            legacyJson.Should().Contain(nameof(NamedInstruction));

            ISequenceContainer roundTripped = converter.Deserialize(legacyJson);

            LinkedTemplateContainer linkedTemplateContainer = roundTripped.Should().BeOfType<LinkedTemplateContainer>().Subject;
            linkedTemplateContainer.Items.Should().BeEmpty();
            linkedTemplateContainer.LinkState.Should().Be(TemplateLinkState.Pending);

            linkedTemplateContainer.TryResolveTemplate().Should().BeTrue();
            linkedTemplateContainer.Items.OfType<ISequenceContainer>().Single().Items.Should().ContainSingle(i => i.Name == "Current item");
        }

        [Test]
        public void DropTargetCommand_AppliesTargetOverrideToMaterializedDsoTemplateAfterRefresh() {
            TemplateReference reference = CreateReference("Galaxy.template.json", "Galaxy");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            TargetableContainer templateContainer = CreateTargetableTemplate("Galaxy workflow", "Original item");
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, templateContainer) }, true, null);
            sut.TryResolveTemplate();

            TargetableContainer materialized = sut.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetableContainer>().Subject;
            sut.SupportsTargetOverride.Should().BeTrue();
            sut.HasTargetOverride.Should().BeFalse();
            materialized.Target.TargetName.Should().BeEmpty();

            TargetableContainer targetSource = CreateTargetableTemplate("M31", "Target item");
            targetSource.Target.TargetName = "M31";
            targetSource.Target.InputCoordinates = new InputCoordinates(new Coordinates(0.7123, 41.269, Epoch.J2000, Coordinates.RAType.Hours));
            targetSource.Target.PositionAngle = 123.4d;

            sut.DropTargetCommand.Execute(new DropIntoParameters(new TargetSequenceContainer(profileServiceMock.Object, targetSource), sut));

            sut.HasTargetOverride.Should().BeTrue();
            sut.TargetStatusText.Should().Contain("M31");
            materialized.Target.TargetName.Should().Be("M31");
            materialized.Target.InputCoordinates.Coordinates.RA.Should().BeApproximately(0.7123, 0.0000001);
            materialized.Target.InputCoordinates.Coordinates.Dec.Should().BeApproximately(41.269, 0.0000001);
            materialized.Target.PositionAngle.Should().BeApproximately(123.4d, 0.0000001);

            TargetableContainer updatedTemplateContainer = CreateTargetableTemplate("Galaxy workflow", "Updated item");
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, updatedTemplateContainer) }, true, null);
            sut.TryResolveTemplate();

            TargetableContainer refreshed = sut.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetableContainer>().Subject;
            refreshed.Items.Should().ContainSingle(i => i.Name == "Updated item");
            refreshed.Target.TargetName.Should().Be("M31");
            refreshed.Target.InputCoordinates.Coordinates.RA.Should().BeApproximately(0.7123, 0.0000001);
            refreshed.Target.InputCoordinates.Coordinates.Dec.Should().BeApproximately(41.269, 0.0000001);
            refreshed.Target.PositionAngle.Should().BeApproximately(123.4d, 0.0000001);
        }

        [Test]
        public void TargetEditor_UpdatesTargetOverrideAndMaterializedDsoTemplate() {
            TemplateReference reference = CreateReference("EditableTarget.template.json", "EditableTarget");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            TargetableContainer templateContainer = CreateTargetableTemplate("Galaxy workflow", "Original item");
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, templateContainer) }, true, null);
            sut.TryResolveTemplate();

            sut.TargetEditor.TargetName = "M51";
            sut.TargetEditor.InputCoordinates = new InputCoordinates(new Coordinates(13.4979, 47.1953, Epoch.J2000, Coordinates.RAType.Hours));
            sut.TargetEditor.PositionAngle = 88.2d;

            TargetableContainer materialized = sut.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetableContainer>().Subject;
            sut.HasTargetOverride.Should().BeTrue();
            sut.TargetOverride.TargetName.Should().Be("M51");
            sut.TargetOverride.InputCoordinates.Coordinates.RA.Should().BeApproximately(13.4979, 0.0000001);
            sut.TargetOverride.InputCoordinates.Coordinates.Dec.Should().BeApproximately(47.1953, 0.0000001);
            sut.TargetOverride.PositionAngle.Should().BeApproximately(88.2d, 0.0000001);
            materialized.Target.TargetName.Should().Be("M51");
            materialized.Target.InputCoordinates.Coordinates.RA.Should().BeApproximately(13.4979, 0.0000001);
            materialized.Target.InputCoordinates.Coordinates.Dec.Should().BeApproximately(47.1953, 0.0000001);
            materialized.Target.PositionAngle.Should().BeApproximately(88.2d, 0.0000001);
        }

        [Test]
        public void TargetEditorCoordinatePartChanges_UpdateMaterializedDsoTemplateInPlace() {
            TemplateReference reference = CreateReference("EditableTargetParts.template.json", "EditableTargetParts");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            TargetableContainer templateContainer = CreateTargetableTemplate("Galaxy workflow", "Original item");
            CoordinatesInstruction inheritedCoordinatesInstruction = new CoordinatesInstruction();
            templateContainer.Add(inheritedCoordinatesInstruction);
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, templateContainer) }, true, null);
            sut.TryResolveTemplate();
            TargetableContainer materialized = sut.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetableContainer>().Subject;
            CoordinatesInstruction materializedCoordinatesInstruction = materialized.Items.OfType<CoordinatesInstruction>().Should().ContainSingle().Subject;
            InputCoordinates originalMaterializedCoordinates = materialized.Target.InputCoordinates;
            List<string> coordinatePropertyChanges = new List<string>();
            materialized.Target.InputCoordinates.PropertyChanged += (sender, args) => {
                if (args.PropertyName != null) {
                    coordinatePropertyChanges.Add(args.PropertyName);
                }
            };

            sut.TargetEditor.TargetName = "M81";
            sut.TargetEditor.InputCoordinates.RAHours = 9;
            sut.TargetEditor.InputCoordinates.RAMinutes = 55;
            sut.TargetEditor.InputCoordinates.RASeconds = 33.2d;
            sut.TargetEditor.InputCoordinates.DecDegrees = 69;
            sut.TargetEditor.InputCoordinates.DecMinutes = 3;
            sut.TargetEditor.InputCoordinates.DecSeconds = 55.1d;
            sut.TargetEditor.PositionAngle = 27.4d;

            materialized.Target.TargetName.Should().Be("M81");
            materialized.Target.InputCoordinates.Should().BeSameAs(originalMaterializedCoordinates);
            materialized.Target.InputCoordinates.RAHours.Should().Be(9);
            materialized.Target.InputCoordinates.RAMinutes.Should().Be(55);
            materialized.Target.InputCoordinates.RASeconds.Should().BeApproximately(33.2d, 0.0000001);
            materialized.Target.InputCoordinates.DecDegrees.Should().Be(69);
            materialized.Target.InputCoordinates.DecMinutes.Should().Be(3);
            materialized.Target.InputCoordinates.DecSeconds.Should().BeApproximately(55.1d, 0.0000001);
            materialized.Target.PositionAngle.Should().BeApproximately(27.4d, 0.0000001);
            coordinatePropertyChanges.Should().Contain(nameof(InputCoordinates.RAHours));
            coordinatePropertyChanges.Should().Contain(nameof(InputCoordinates.DecDegrees));
            materializedCoordinatesInstruction.Inherited.Should().BeTrue();
            materializedCoordinatesInstruction.Coordinates.Coordinates.RA.Should().BeApproximately(materialized.Target.InputCoordinates.Coordinates.RA, 0.0000001);
            materializedCoordinatesInstruction.Coordinates.Coordinates.Dec.Should().BeApproximately(materialized.Target.InputCoordinates.Coordinates.Dec, 0.0000001);
            materializedCoordinatesInstruction.PositionAngle.Should().BeApproximately(materialized.Target.PositionAngle, 0.0000001);
        }

        [Test]
        public void TargetEditorCoordinatePartChangesWithoutName_CountAsTargetOverrideAndUpdateSubtree() {
            TemplateReference reference = CreateReference("CoordinateOnlyTarget.template.json", "CoordinateOnlyTarget");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            TargetableContainer templateContainer = CreateTargetableTemplate("Galaxy workflow", "Original item");
            templateContainer.Add(new CoordinatesInstruction());
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, templateContainer) }, true, null);
            sut.TryResolveTemplate();
            TargetableContainer materialized = sut.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetableContainer>().Subject;
            CoordinatesInstruction materializedCoordinatesInstruction = materialized.Items.OfType<CoordinatesInstruction>().Should().ContainSingle().Subject;

            sut.TargetEditor.InputCoordinates.RAHours = 9;
            sut.TargetEditor.InputCoordinates.RAMinutes = 55;
            sut.TargetEditor.InputCoordinates.RASeconds = 33.2d;
            sut.TargetEditor.InputCoordinates.DecDegrees = 69;
            sut.TargetEditor.InputCoordinates.DecMinutes = 3;
            sut.TargetEditor.InputCoordinates.DecSeconds = 55.1d;

            sut.HasTargetOverride.Should().BeTrue();
            materialized.Name.Should().Be("Galaxy workflow");
            materialized.Target.TargetName.Should().BeEmpty();
            materialized.Target.InputCoordinates.RAHours.Should().Be(9);
            materialized.Target.InputCoordinates.RAMinutes.Should().Be(55);
            materialized.Target.InputCoordinates.RASeconds.Should().BeApproximately(33.2d, 0.0000001);
            materialized.Target.InputCoordinates.DecDegrees.Should().Be(69);
            materialized.Target.InputCoordinates.DecMinutes.Should().Be(3);
            materialized.Target.InputCoordinates.DecSeconds.Should().BeApproximately(55.1d, 0.0000001);
            materializedCoordinatesInstruction.Inherited.Should().BeTrue();
            materializedCoordinatesInstruction.Coordinates.Coordinates.RA.Should().BeApproximately(materialized.Target.InputCoordinates.Coordinates.RA, 0.0000001);
            materializedCoordinatesInstruction.Coordinates.Coordinates.Dec.Should().BeApproximately(materialized.Target.InputCoordinates.Coordinates.Dec, 0.0000001);
        }

        [Test]
        public void MaterializedDsoTargetChanges_UpdateTargetOverrideAndTargetEditor() {
            TemplateReference reference = CreateReference("ExternalTargetUpdate.template.json", "ExternalTargetUpdate");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            TargetableContainer templateContainer = CreateTargetableTemplate("Galaxy workflow", "Original item");
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, templateContainer) }, true, null);
            sut.TryResolveTemplate();
            TargetableContainer materialized = sut.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetableContainer>().Subject;
            List<string> changedProperties = new List<string>();
            sut.PropertyChanged += (sender, args) => {
                if (args.PropertyName != null) {
                    changedProperties.Add(args.PropertyName);
                }
            };

            materialized.Target.TargetName = "M101";
            materialized.Target.PositionAngle = 12.3d;
            materialized.Target.InputCoordinates = new InputCoordinates(new Coordinates(14.0535, 54.3489, Epoch.J2000, Coordinates.RAType.Hours));

            sut.HasTargetOverride.Should().BeTrue();
            sut.TargetOverride.TargetName.Should().Be("M101");
            sut.TargetOverride.InputCoordinates.Coordinates.RA.Should().BeApproximately(14.0535, 0.0000001);
            sut.TargetOverride.InputCoordinates.Coordinates.Dec.Should().BeApproximately(54.3489, 0.0000001);
            sut.TargetOverride.PositionAngle.Should().BeApproximately(12.3d, 0.0000001);
            sut.TargetEditor.TargetName.Should().Be("M101");
            sut.TargetEditor.InputCoordinates.Coordinates.RA.Should().BeApproximately(14.0535, 0.0000001);
            sut.TargetEditor.InputCoordinates.Coordinates.Dec.Should().BeApproximately(54.3489, 0.0000001);
            sut.TargetEditor.PositionAngle.Should().BeApproximately(12.3d, 0.0000001);
            sut.TargetStatusText.Should().Contain("M101");
            materialized.Name.Should().Be("M101");
            changedProperties.Should().Contain(nameof(LinkedTemplateContainer.TargetOverride));
            changedProperties.Should().Contain(nameof(LinkedTemplateContainer.TargetEditor));
            changedProperties.Should().Contain(nameof(LinkedTemplateContainer.TargetStatusText));
        }

        [Test]
        public void RefreshLinkState_DetectsTargetSupportWithoutMaterializingContent() {
            TemplateReference reference = CreateReference("LazyTarget.template.json", "LazyTarget");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            TargetableContainer templateContainer = CreateTargetableTemplate("Galaxy workflow", "Original item");
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, templateContainer) }, true, null);
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };

            sut.RefreshLinkState().Should().BeTrue();

            sut.Items.Should().BeEmpty();
            sut.SupportsTargetOverride.Should().BeTrue();
            sut.HasTargetOverride.Should().BeFalse();

            TargetableContainer targetSource = CreateTargetableTemplate("M31", "Target item");
            targetSource.Target.TargetName = "M31";
            targetSource.Target.InputCoordinates = new InputCoordinates(new Coordinates(0.7123, 41.269, Epoch.J2000, Coordinates.RAType.Hours));
            targetSource.Target.PositionAngle = 123.4d;

            sut.DropTargetCommand.Execute(new DropIntoParameters(new TargetSequenceContainer(profileServiceMock.Object, targetSource), sut));

            sut.HasTargetOverride.Should().BeTrue();
            sut.TargetStatusText.Should().Contain("M31");
            sut.Items.Should().BeEmpty();

            sut.IsExpanded = true;

            TargetableContainer materialized = sut.Items.Should().ContainSingle().Subject.Should().BeOfType<TargetableContainer>().Subject;
            materialized.Target.TargetName.Should().Be("M31");
            materialized.Target.InputCoordinates.Coordinates.RA.Should().BeApproximately(0.7123, 0.0000001);
            materialized.Target.InputCoordinates.Coordinates.Dec.Should().BeApproximately(41.269, 0.0000001);
            materialized.Target.PositionAngle.Should().BeApproximately(123.4d, 0.0000001);
        }

        [Test]
        public void SequenceJsonConverter_RoundTripsTargetOverride() {
            TemplateReference reference = CreateReference("TargetRoundTrip.template.json", "TargetRoundTrip");
            TemplateLinkResolver resolver = new TemplateLinkResolver();
            resolver.UpdateTemplates(new[] { CreateTemplate(reference, "TargetRoundTrip", "Template item") }, true, null);
            LinkedTemplateContainer sut = new LinkedTemplateContainer(resolver) {
                TemplateReference = reference.Clone()
            };
            sut.TryResolveTemplate();
            TargetableContainer targetSource = CreateTargetableTemplate("M33", "Target item");
            targetSource.Target.TargetName = "M33";
            targetSource.Target.InputCoordinates = new InputCoordinates(new Coordinates(1.5641, 30.6602, Epoch.J2000, Coordinates.RAType.Hours));
            targetSource.Target.PositionAngle = 45.6d;
            sut.TargetOverride = LinkedTemplateTargetOverride.FromInputTarget(targetSource.Target);
            SequencerFactory factory = new SequencerFactory(
                profileServiceMock.Object,
                new List<ISequenceItem> { new NamedInstruction("Prototype") },
                new List<NINA.Sequencer.Conditions.ISequenceCondition>(),
                new List<NINA.Sequencer.Trigger.ISequenceTrigger>(),
                new List<ISequenceContainer> { new LinkedTemplateContainer(resolver), new SequentialContainer() },
                new List<IDateTimeProvider>(),
                new List<ISequenceEntityUpgrader>());
            SequenceJsonConverter converter = new SequenceJsonConverter(factory);

            ISequenceContainer roundTripped = converter.Deserialize(converter.Serialize(sut));

            LinkedTemplateContainer linkedTemplateContainer = roundTripped.Should().BeOfType<LinkedTemplateContainer>().Subject;
            linkedTemplateContainer.TargetOverride.TargetName.Should().Be("M33");
            linkedTemplateContainer.TargetOverride.InputCoordinates.Coordinates.RA.Should().BeApproximately(1.5641, 0.0000001);
            linkedTemplateContainer.TargetOverride.InputCoordinates.Coordinates.Dec.Should().BeApproximately(30.6602, 0.0000001);
            linkedTemplateContainer.TargetOverride.PositionAngle.Should().BeApproximately(45.6d, 0.0000001);
            linkedTemplateContainer.TargetEditor.TargetName.Should().Be("M33");
            linkedTemplateContainer.TargetEditor.InputCoordinates.Coordinates.RA.Should().BeApproximately(1.5641, 0.0000001);
            linkedTemplateContainer.TargetEditor.InputCoordinates.Coordinates.Dec.Should().BeApproximately(30.6602, 0.0000001);
            linkedTemplateContainer.TargetEditor.PositionAngle.Should().BeApproximately(45.6d, 0.0000001);
        }

        private TemplatedSequenceContainer CreateTemplate(TemplateReference reference, string templateName, string itemName) {
            return CreateTemplate(reference, templateName, new NamedInstruction(itemName));
        }

        private TemplatedSequenceContainer CreateTemplate(TemplateReference reference, string templateName, ISequenceItem item) {
            SequentialContainer container = new SequentialContainer {
                Name = templateName
            };
            container.Add(item);
            return new TemplatedSequenceContainer(profileServiceMock.Object, "LblTemplate_UserTemplates", container, reference.Clone(), null);
        }

        private TemplatedSequenceContainer CreateTemplate(TemplateReference reference, ISequenceContainer container) {
            return new TemplatedSequenceContainer(profileServiceMock.Object, "LblTemplate_UserTemplates", container, reference.Clone(), null);
        }

        private static TargetableContainer CreateTargetableTemplate(string templateName, string itemName) {
            TargetableContainer container = new TargetableContainer {
                Name = templateName
            };
            container.Add(new NamedInstruction(itemName));
            return container;
        }

        private static TemplateReference CreateReference(string path, string displayName) {
            return new TemplateReference {
                SourceKind = TemplateReferenceSourceKind.User,
                RelativePath = path,
                DisplayName = displayName
            };
        }

        private sealed class ExecutionProbe {
            public int Executions { get; set; }
        }

        private sealed class ProbeInstruction : SequenceItemBase {
            private readonly ExecutionProbe executionProbe;

            public ProbeInstruction(ExecutionProbe executionProbe) {
                this.executionProbe = executionProbe;
                Name = "Probe";
            }

            public override object Clone() {
                return new ProbeInstruction(executionProbe);
            }

            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
                executionProbe.Executions++;
                return Task.CompletedTask;
            }
        }

        private sealed class TargetableContainer : SequentialContainer, IDeepSkyObjectContainer {
            private InputTarget target = new InputTarget(Angle.Zero, Angle.Zero, null);

            public TargetableContainer() {
                Target = new InputTarget(Angle.Zero, Angle.Zero, null);
            }

            public InputTarget Target {
                get => target;
                set {
                    if (ReferenceEquals(target, value)) {
                        return;
                    }

                    if (target != null) {
                        target.CoordinatesChanged -= Target_OnCoordinatesChanged;
                    }

                    target = value;
                    if (target != null) {
                        target.CoordinatesChanged += Target_OnCoordinatesChanged;
                    }
                }
            }

            public NighttimeData NighttimeData => null;

            private void Target_OnCoordinatesChanged(object? sender, EventArgs e) {
                AfterParentChanged();
            }

            public override object Clone() {
                TargetableContainer clone = new TargetableContainer {
                    Icon = Icon,
                    Name = Name,
                    Category = Category,
                    Description = Description,
                    Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                    Triggers = new ObservableCollection<ISequenceTrigger>(Triggers.Select(t => t.Clone() as ISequenceTrigger)),
                    Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(c => c.Clone() as ISequenceCondition)),
                    Target = CloneTarget(Target)
                };

                foreach (ISequenceItem item in clone.Items) {
                    item.AttachNewParent(clone);
                }

                foreach (ISequenceCondition condition in clone.Conditions) {
                    condition.AttachNewParent(clone);
                }

                foreach (ISequenceTrigger trigger in clone.Triggers) {
                    trigger.AttachNewParent(clone);
                }

                return clone;
            }

            private static InputTarget CloneTarget(InputTarget source) {
                InputTarget clone = new InputTarget(Angle.Zero, Angle.Zero, null) {
                    TargetName = source.TargetName,
                    InputCoordinates = source.InputCoordinates?.Clone() ?? new InputCoordinates(),
                    PositionAngle = source.PositionAngle
                };

                return clone;
            }
        }

        private sealed class NamedInstruction : SequenceItemBase {
            public NamedInstruction(string name) {
                Name = name;
            }

            public override object Clone() {
                return new NamedInstruction(Name);
            }

            public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
                return Task.CompletedTask;
            }
        }
    }
}
