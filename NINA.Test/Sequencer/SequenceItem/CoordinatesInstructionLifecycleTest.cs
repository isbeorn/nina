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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Serialization;
using System.IO;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Editing;
using NINA.ViewModel.FramingAssistant;
using NINA.WPF.Base.SkySurvey;
using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger.SafetyMonitor;
using NINA.Sequencer.Trigger.Utility;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Test.Sequencer.SequenceItem {
    public partial class CoordinatesInstructionInheritanceTest {
        [TestCase(SlewScopeToRaDecInstruction, true)]
        [TestCase(SlewScopeToRaDecInstruction, false)]
        [TestCase(nameof(AltitudeCondition), true)]
        [TestCase(nameof(AltitudeCondition), false)]
        [TestCase(nameof(AboveHorizonCondition), true)]
        [TestCase(nameof(AboveHorizonCondition), false)]
        public void CoordinateEditorSnapshotReplay_InvalidExpressionKeepsSavedAxis(string entityName, bool rightAscension) {
            var root = new SequenceRootContainer();
            CoordinatesInstruction? instruction = null;
            AltitudeCondition? altitude = null;
            AboveHorizonCondition? horizon = null;
            ISequenceEntity entity;
            if (entityName == nameof(AltitudeCondition)) {
                altitude = new AltitudeCondition(profileServiceMock.Object);
                root.Add(altitude);
                entity = altitude;
            } else if (entityName == nameof(AboveHorizonCondition)) {
                horizon = new AboveHorizonCondition(profileServiceMock.Object);
                root.Add(horizon);
                entity = horizon;
            } else {
                instruction = CreateInstruction(entityName);
                root.Add(instruction);
                entity = instruction;
            }

            var input = instruction?.Coordinates ?? altitude?.Data.Coordinates ?? horizon!.Data.Coordinates;
            var ra = instruction?.RaExpression ?? altitude?.RaExpression ?? horizon!.RaExpression;
            var dec = instruction?.DecExpression ?? altitude?.DecExpression ?? horizon!.DecExpression;
            ra.Definition = "5";
            dec.Definition = "-0.5";
            var expression = rightAscension ? ra : dec;
            expression.Definition = "invalid(";
            expression.IsValid = true;
            var provider = (ISequenceCustomPropertyEditProvider)entity;
            string property = rightAscension ? nameof(InputCoordinates.RAHours) : nameof(InputCoordinates.DecDegrees);
            provider.TryCapturePropertyState(input, property, out var before).Should().BeTrue();
            before.Should().NotBeNull();
            if (rightAscension) {
                input.RAHours = 8;
            } else {
                input.DecDegrees = -10;
            }
            provider.TryCapturePropertyState(input, property, out var after).Should().BeTrue();
            after.Should().NotBeNull();
            double editedDec = input.Coordinates.Dec;

            for (int repeat = 0; repeat < 2; repeat++) {
                before!.Restore();
                input.Coordinates.RA.Should().Be(5);
                input.Coordinates.Dec.Should().Be(-0.5);
                expression.Definition.Should().Be("invalid(");
                after!.Restore();
                input.Coordinates.RA.Should().Be(rightAscension ? 8 : 5);
                input.Coordinates.Dec.Should().Be(editedDec);
            }
        }

        [TestCase(CenterInstruction, true)]
        [TestCase(CenterInstruction, false)]
        [TestCase(CenterAndRotateInstruction, true)]
        [TestCase(CenterAndRotateInstruction, false)]
        [TestCase(SlewScopeToRaDecInstruction, true)]
        [TestCase(SlewScopeToRaDecInstruction, false)]
        [TestCase(WaitForAltitudeInstruction, true)]
        [TestCase(WaitForAltitudeInstruction, false)]
        [TestCase(WaitUntilAboveHorizonInstruction, true)]
        [TestCase(WaitUntilAboveHorizonInstruction, false)]
        public void LegalCoordinateEndpoints_PreserveUntouchedFormula(string instructionName, bool rightAscension) {
            var instruction = CreateInstruction(instructionName);
            new SequenceRootContainer().Add(instruction);
            instruction.RaExpression.Definition = "1+1";
            instruction.DecExpression.Definition = "-1/2";
            _ = instruction.Ra;
            _ = instruction.Dec;
            foreach (double endpoint in rightAscension ? new double[] { 0, 24 } : new double[] { -90, 90 }) {
                if (rightAscension) {
                    instruction.RaExpression.Definition = endpoint.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    _ = instruction.Ra;
                    instruction.Coordinates.Coordinates.RA.Should().Be(endpoint);
                    instruction.Coordinates.Coordinates.Dec.Should().Be(-0.5);
                    instruction.DecExpression.Definition.Should().Be("-1/2");
                } else {
                    instruction.DecExpression.Definition = endpoint.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    _ = instruction.Dec;
                    instruction.Coordinates.Coordinates.Dec.Should().Be(endpoint);
                    instruction.Coordinates.Coordinates.RA.Should().Be(2);
                    instruction.RaExpression.Definition.Should().Be("1+1");
                }
            }
        }

        [TestCase(CenterInstruction, true, "NaN")]
        [TestCase(CenterInstruction, true, "Sqrt(-1)")]
        [TestCase(CenterInstruction, false, "NaN")]
        [TestCase(CenterInstruction, false, "Sqrt(-1)")]
        [TestCase(CenterAndRotateInstruction, true, "NaN")]
        [TestCase(CenterAndRotateInstruction, true, "Sqrt(-1)")]
        [TestCase(CenterAndRotateInstruction, false, "NaN")]
        [TestCase(CenterAndRotateInstruction, false, "Sqrt(-1)")]
        [TestCase(SlewScopeToRaDecInstruction, true, "NaN")]
        [TestCase(SlewScopeToRaDecInstruction, true, "Sqrt(-1)")]
        [TestCase(SlewScopeToRaDecInstruction, false, "NaN")]
        [TestCase(SlewScopeToRaDecInstruction, false, "Sqrt(-1)")]
        [TestCase(WaitForAltitudeInstruction, true, "NaN")]
        [TestCase(WaitForAltitudeInstruction, true, "Sqrt(-1)")]
        [TestCase(WaitForAltitudeInstruction, false, "NaN")]
        [TestCase(WaitForAltitudeInstruction, false, "Sqrt(-1)")]
        [TestCase(WaitUntilAboveHorizonInstruction, true, "NaN")]
        [TestCase(WaitUntilAboveHorizonInstruction, true, "Sqrt(-1)")]
        [TestCase(WaitUntilAboveHorizonInstruction, false, "NaN")]
        [TestCase(WaitUntilAboveHorizonInstruction, false, "Sqrt(-1)")]
        [TestCase(nameof(AltitudeCondition), true, "NaN")]
        [TestCase(nameof(AltitudeCondition), true, "Sqrt(-1)")]
        [TestCase(nameof(AltitudeCondition), false, "NaN")]
        [TestCase(nameof(AltitudeCondition), false, "Sqrt(-1)")]
        [TestCase(nameof(AboveHorizonCondition), true, "NaN")]
        [TestCase(nameof(AboveHorizonCondition), true, "Sqrt(-1)")]
        [TestCase(nameof(AboveHorizonCondition), false, "NaN")]
        [TestCase(nameof(AboveHorizonCondition), false, "Sqrt(-1)")]
        public void NonfiniteExpressions_PreserveCoordinatesAndRecoverToValidZero(string entityName, bool rightAscension, string nonfiniteDefinition) {
            var root = new SequenceRootContainer();
            CoordinatesInstruction? instruction = null;
            AltitudeCondition? altitude = null;
            AboveHorizonCondition? horizon = null;
            if (entityName == nameof(AltitudeCondition)) {
                altitude = new AltitudeCondition(profileServiceMock.Object);
                root.Add(altitude);
            } else if (entityName == nameof(AboveHorizonCondition)) {
                horizon = new AboveHorizonCondition(profileServiceMock.Object);
                root.Add(horizon);
            } else {
                instruction = CreateInstruction(entityName);
                root.Add(instruction);
            }

            var input = instruction?.Coordinates ?? altitude?.Data.Coordinates ?? horizon!.Data.Coordinates;
            var ra = instruction?.RaExpression ?? altitude?.RaExpression ?? horizon!.RaExpression;
            var dec = instruction?.DecExpression ?? altitude?.DecExpression ?? horizon!.DecExpression;
            ra.Definition = "5";
            dec.Definition = "-0.5";
            void ReadAxis() {
                if (rightAscension) {
                    _ = instruction?.Ra ?? altitude?.Ra ?? horizon!.Ra;
                } else {
                    _ = instruction?.Dec ?? altitude?.Dec ?? horizon!.Dec;
                }
            }
            var expression = rightAscension ? ra : dec;
            foreach (var definition in new[] { nonfiniteDefinition, "Infinity", "invalid(", "100" }) {
                expression.Definition = definition;
                ReadAxis();

                input.Coordinates.RA.Should().Be(5, definition + " must preserve the last valid position");
                input.Coordinates.Dec.Should().Be(-0.5, definition + " must preserve the last valid position");
            }

            expression.Definition = "0";
            ReadAxis();

            input.Coordinates.RA.Should().Be(rightAscension ? 0 : 5);
            input.Coordinates.Dec.Should().Be(rightAscension ? -0.5 : 0);
        }

        [TestCase(nameof(CoordinatesInstruction), false)]
        [TestCase(nameof(CoordinatesInstruction), true)]
        [TestCase(CenterInstruction, false)]
        [TestCase(CenterInstruction, true)]
        [TestCase(SlewScopeToRaDecInstruction, false)]
        [TestCase(SlewScopeToRaDecInstruction, true)]
        [TestCase(WaitForAltitudeInstruction, false)]
        [TestCase(WaitForAltitudeInstruction, true)]
        [TestCase(WaitUntilAboveHorizonInstruction, false)]
        [TestCase(WaitUntilAboveHorizonInstruction, true)]
        public void CoordinateJsonHydration_PreservesSelectedSignAtZero(string instructionName, bool negative) {
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new System.Collections.Generic.List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => new SequenceRootContainer());
            factory.Setup(x => x.GetItem<CoordinatesInstruction>()).Returns(() => (CoordinatesInstruction)new CoordinatesInstruction().Clone());
            factory.Setup(x => x.GetItem<Center>()).Returns(() => (Center)CreateInstruction(CenterInstruction).Clone());
            factory.Setup(x => x.GetItem<SlewScopeToRaDec>()).Returns(() => (SlewScopeToRaDec)CreateInstruction(SlewScopeToRaDecInstruction).Clone());
            factory.Setup(x => x.GetItem<WaitUntilAboveHorizon>()).Returns(() => (WaitUntilAboveHorizon)new WaitUntilAboveHorizon(profileServiceMock.Object).Clone());
            factory.Setup(x => x.GetItem<WaitForAltitude>()).Returns(() => (WaitForAltitude)new WaitForAltitude(profileServiceMock.Object).Clone());
            var converter = new SequenceJsonConverter(factory.Object);
            var instruction = instructionName == nameof(CoordinatesInstruction) ? new CoordinatesInstruction() : CreateInstruction(instructionName);
            var root = new SequenceRootContainer();
            root.Add(instruction);
            instruction.Coordinates.NegativeDec = negative;

            var loaded = (CoordinatesInstruction)converter.Deserialize(converter.Serialize(root)).Items[0];

            loaded.Coordinates.Coordinates.RA.Should().Be(0);
            loaded.Coordinates.Coordinates.Dec.Should().Be(0);
            loaded.Coordinates.NegativeDec.Should().Be(negative);
            if (loaded.Data != null) {
                loaded.Data.Coordinates.Should().BeSameAs(loaded.Coordinates);
            }
        }

        [TestCase(CenterInstruction)]
        [TestCase(CenterAndRotateInstruction)]
        [TestCase(SlewScopeToRaDecInstruction)]
        [TestCase(WaitForAltitudeInstruction)]
        [TestCase(WaitUntilAboveHorizonInstruction)]
        public void AtomicExpressionWrite_PreservesEpochClockAndOppositeFormula(string instructionName) {
            var instruction = CreateInstruction(instructionName);
            new SequenceRootContainer().Add(instruction);
            var clock = Mock.Of<ICustomDateTime>();
            instruction.Coordinates.Coordinates = new Coordinates(Angle.ByHours(5), Angle.ByDegree(-0.5), Epoch.JNOW, clock);
            instruction.DecExpression.Definition = "-1/2";
            instruction.RaExpression.Definition = "2+4";

            instruction.Coordinates.Coordinates.RA.Should().Be(6);
            instruction.Coordinates.Coordinates.Dec.Should().Be(-0.5);
            instruction.Coordinates.Coordinates.Epoch.Should().Be(Epoch.JNOW);
            instruction.Coordinates.Coordinates.DateTime.Should().BeSameAs(clock);
            instruction.DecExpression.Definition.Should().Be("-1/2");
            instruction.Coordinates.RAMinutes = 30;
            instruction.DecExpression.Definition.Should().Be("-1/2");
        }

        [TestCase(false, "", "", 21.557505555555555, -0.82325)]
        [TestCase(true, "", "", 21.557505555555555, -0.82325)]
        [TestCase(false, "0", "0", 0, 0)]
        [TestCase(true, "0", "0", 0, 0)]
        [TestCase(false, "1+1", "-1/2", 2, -0.5)]
        [TestCase(true, "1+1", "-1/2", 2, -0.5)]
        public void StandaloneWaitJson_DefinitionsWinAndBothLegacyAxesAreSnapshotted(bool altitudeWait, string raDefinition, string decDefinition, double expectedRa, double expectedDec) {
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new System.Collections.Generic.List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => new SequenceRootContainer());
            factory.Setup(x => x.GetItem<WaitUntilAboveHorizon>()).Returns(() => (WaitUntilAboveHorizon)new WaitUntilAboveHorizon(profileServiceMock.Object).Clone());
            factory.Setup(x => x.GetItem<WaitForAltitude>()).Returns(() => (WaitForAltitude)new WaitForAltitude(profileServiceMock.Object).Clone());
            var converter = new SequenceJsonConverter(factory.Object);
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Sequencer", "Serialization", "LegacySequences", "InheritedWaitCoordinates.json");
            var root = JObject.Parse(File.ReadAllText(path));
            var wait = (JObject)root["Items"]![0]!["Items"]![0]!["Items"]![0]!;
            wait["Inherited"] = false;
            wait["Parent"] = new JObject { ["$ref"] = "1" };
            wait["RaExpression"]!["Definition"] = raDefinition;
            wait["DecExpression"]!["Definition"] = decDefinition;
            wait["RaDefinition"] = raDefinition;
            wait["DecDefinition"] = decDefinition;
            if (altitudeWait) {
                wait["$type"] = typeof(WaitForAltitude).AssemblyQualifiedName;
            }
            // The pre-expression wait format stores its coordinate input only in Data.
            var legacy = wait["Coordinates"]!;
            legacy.Parent!.Remove();
            wait["Data"]!["Coordinates"] = legacy;
            root["Items"] = new JArray(wait);

            var result = converter.Deserialize(root.ToString());
            var loaded = (CoordinatesInstruction)result.Items[0];

            loaded.Coordinates.Coordinates.RA.Should().BeApproximately(expectedRa, Tolerance);
            loaded.Coordinates.Coordinates.Dec.Should().BeApproximately(expectedDec, Tolerance);
            loaded.Data.Coordinates.Should().BeSameAs(loaded.Coordinates);
            var repeated = (CoordinatesInstruction)converter.Deserialize(converter.Serialize(result)).Items[0];
            repeated.Coordinates.Coordinates.RA.Should().BeApproximately(Math.Round(expectedRa, 7), Tolerance);
            repeated.Coordinates.Coordinates.Dec.Should().BeApproximately(Math.Round(expectedDec, 7), Tolerance);
            var secondReload = (CoordinatesInstruction)converter.Deserialize(converter.Serialize(repeated.Parent)).Items[0];
            secondReload.Coordinates.Coordinates.RA.Should().Be(repeated.Coordinates.Coordinates.RA);
            secondReload.Coordinates.Coordinates.Dec.Should().Be(repeated.Coordinates.Coordinates.Dec);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WaitClone_RetainsDataLegacyPriorityAndNullCoordinateFallback(bool altitudeWait) {
            CoordinatesInstruction original = altitudeWait
                ? new WaitForAltitude(profileServiceMock.Object) : new WaitUntilAboveHorizon(profileServiceMock.Object);
            original.Data.Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(12), Angle.ByDegree(-0.25), Epoch.J2000));
            var clone = (CoordinatesInstruction)original.Clone();

            clone.Coordinates.Should().BeSameAs(clone.Data.Coordinates);
            clone.Coordinates.Should().NotBeSameAs(original.Data.Coordinates);
            clone.Coordinates.Coordinates.RA.Should().Be(12);
            clone.Coordinates.Coordinates.Dec.Should().Be(-0.25);
            original.Data.Coordinates = null;

            clone = (CoordinatesInstruction)original.Clone();

            clone.Coordinates.Should().NotBeNull().And.BeSameAs(clone.Data.Coordinates);
            clone.Coordinates.Coordinates.RA.Should().Be(0);
            clone.Coordinates.Coordinates.Dec.Should().Be(0);
        }

        [Test]
        public void BaseInstruction_DataRemainsIndependentWithoutWaitOptIn() {
            var instruction = new CoordinatesInstruction {
                Data = new WaitLoopData(profileServiceMock.Object, false, "Plugin coordinates") {
                    Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(12), Angle.ByDegree(-0.25), Epoch.J2000))
                },
                Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(5), Angle.ByDegree(0.5), Epoch.J2000))
            };

            instruction.OnDeserialized(default);

            instruction.Coordinates.Should().NotBeSameAs(instruction.Data.Coordinates);
            instruction.Coordinates.Coordinates.RA.Should().Be(5);
            instruction.Coordinates.Coordinates.Dec.Should().Be(0.5);
            instruction.Data.Coordinates.Coordinates.RA.Should().Be(12);
            instruction.Data.Coordinates.Coordinates.Dec.Should().Be(-0.25);
        }

        [TestCase(CenterInstruction)]
        [TestCase(CenterAndRotateInstruction)]
        [TestCase(SlewScopeToRaDecInstruction)]
        [TestCase(WaitForAltitudeInstruction)]
        [TestCase(WaitUntilAboveHorizonInstruction)]
        public void StandaloneExpressionEdit_PreservesSignedDeclinationAndOtherFormula(string instructionName) {
            var instruction = CreateInstruction(instructionName);
            new SequenceRootContainer().Add(instruction);
            instruction.RaExpression.Definition = "5+5";
            instruction.DecExpression.Definition = "-0.5";
            _ = instruction.Ra;
            _ = instruction.Dec;
            instruction.Coordinates.Coordinates.Dec.Should().Be(-0.5);
            instruction.Coordinates.NegativeDec.Should().BeTrue();

            instruction.Coordinates.RAHours = 12;

            instruction.DecExpression.Definition.Should().Be("-0.5");
            instruction.DecExpression.Definition = "0.5";
            instruction.Coordinates.Coordinates.Dec.Should().Be(0.5);
            instruction.Coordinates.NegativeDec.Should().BeFalse();
            instruction.DecExpression.Definition = "0";
            instruction.Coordinates.Coordinates.Dec.Should().Be(0);
            instruction.DecExpression.Definition = "-0.5";
            instruction.DecExpression.Definition = "0";
            instruction.Coordinates.Coordinates.Dec.Should().Be(0);
            instruction.Coordinates.NegativeDec.Should().BeTrue();
            instruction.DecExpression.Definition = "invalid(";
            instruction.Coordinates.Coordinates.Dec.Should().Be(0);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConditionDetach_ReentrantValidationRetainsLastInheritedPosition(bool aboveHorizon) {
            var target = CreateTargetContainer();
            new SequenceRootContainer().Add(target);
            var source = new Coordinates(Angle.ByHours(21.5575056), Angle.ByDegree(-0.82325), Epoch.J2000);
            SetTarget(target, source, 0);
            var altitude = aboveHorizon ? null : new AltitudeCondition(profileServiceMock.Object);
            var horizon = aboveHorizon ? new AboveHorizonCondition(profileServiceMock.Object) : null;
            var condition = (NINA.Sequencer.Conditions.SequenceCondition?)altitude ?? horizon!;
            var data = altitude?.Data ?? horizon!.Data;
            var ra = altitude?.RaExpression ?? horizon!.RaExpression;
            var dec = altitude?.DecExpression ?? horizon!.DecExpression;
            ra.Definition = "1+1";
            dec.Definition = "3+4";
            target.Add(condition);
            _ = altitude?.Ra ?? horizon!.Ra;
            _ = altitude?.Dec ?? horizon!.Dec;
            bool observed = false;
            condition.PropertyChanged += (_, e) => {
                if (e.PropertyName != nameof(AltitudeCondition.HasDsoParent)) {
                    return;
                }

                observed = true;
                ((NINA.Sequencer.Validations.IValidatable)condition).Validate();
            };

            condition.AttachNewParent(null);

            observed.Should().BeTrue();
            (altitude?.HasDsoParent ?? horizon!.HasDsoParent).Should().BeFalse();
            data.Coordinates.Coordinates.RA.Should().BeApproximately(source.RA, Tolerance);
            data.Coordinates.Coordinates.Dec.Should().BeApproximately(source.Dec, Tolerance);
            source.RA.Should().BeApproximately(21.5575056, Tolerance);
            source.Dec.Should().BeApproximately(-0.82325, Tolerance);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConditionAttachment_ReentrantReadCannotMutateParent(bool aboveHorizon) {
            var target = CreateTargetContainer();
            new SequenceRootContainer().Add(target);
            var source = new Coordinates(Angle.ByHours(21.5575056), Angle.ByDegree(-0.82325), Epoch.J2000);
            SetTarget(target, source, 0);
            var altitude = aboveHorizon ? null : new AltitudeCondition(profileServiceMock.Object);
            var horizon = aboveHorizon ? new AboveHorizonCondition(profileServiceMock.Object) : null;
            var data = altitude?.Data ?? horizon!.Data;
            var ra = altitude?.RaExpression ?? horizon!.RaExpression;
            var dec = altitude?.DecExpression ?? horizon!.DecExpression;
            ra.Definition = "1+1";
            dec.Definition = "3+4";
            bool observed = false;
            data.Coordinates.PropertyChanged += (_, e) => {
                if (observed || e.PropertyName != nameof(InputCoordinates.Coordinates)) {
                    return;
                }

                observed = true;
                _ = altitude?.Ra ?? horizon!.Ra;
                _ = altitude?.Dec ?? horizon!.Dec;
            };

            target.Add((ISequenceCondition?)altitude ?? horizon!);

            observed.Should().BeTrue();
            source.RA.Should().BeApproximately(21.5575056, Tolerance);
            source.Dec.Should().BeApproximately(-0.82325, Tolerance);
            data.Coordinates.Coordinates.Should().NotBeSameAs(source);
            data.Coordinates.Coordinates.RA.Should().BeApproximately(source.RA, Tolerance);
            data.Coordinates.Coordinates.Dec.Should().BeApproximately(source.Dec, Tolerance);
            ra.Definition.Should().Be("1+1");
            dec.Definition.Should().Be("3+4");
        }

        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, false)]
        [TestCase(false, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(true, true, true)]
        public void ReportJson_LoadAndRepeatedRoundTripsPreserveRawInheritedCoordinates(bool fromFile, bool eager, bool altitudeWait) {
            using var horizonReader = new StringReader("0 5\n90 12\n180 8\n270 15");
            profile.AstrometrySettings.Horizon = CustomHorizon.FromReader_Standard(horizonReader);
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new System.Collections.Generic.List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => new SequenceRootContainer());
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => (SequentialContainer)new SequentialContainer().Clone());
            factory.Setup(x => x.GetContainer<DeepSkyObjectContainer>()).Returns(() => (DeepSkyObjectContainer)CreateTargetContainer().Clone());
            factory.Setup(x => x.GetItem<WaitUntilAboveHorizon>()).Returns(() => (WaitUntilAboveHorizon)new WaitUntilAboveHorizon(profileServiceMock.Object).Clone());
            factory.Setup(x => x.GetItem<WaitForAltitude>()).Returns(() => (WaitForAltitude)new WaitForAltitude(profileServiceMock.Object).Clone());
            var converter = new SequenceJsonConverter(factory.Object);
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Sequencer", "Serialization", "LegacySequences", "InheritedWaitCoordinates.json");
            var previous = JsonConvert.DefaultSettings;
            var content = File.ReadAllText(path);
            var temporaryPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid() + ".json");
            try {
                if (altitudeWait) {
                    content = content.Replace(nameof(WaitUntilAboveHorizon), nameof(WaitForAltitude));
                }
                File.WriteAllText(temporaryPath, content);
                JsonConvert.DefaultSettings = eager ? () => new JsonSerializerSettings() : null;
                var result = fromFile ? converter.DeserializeFromFile(temporaryPath) : converter.Deserialize(content);
                for (int iteration = 0; iteration < 3; iteration++) {
                    var target = (DeepSkyObjectContainer)result.Items[0];
                    var wait = (CoordinatesInstruction)((SequentialContainer)target.Items[0]).Items[0];
                    var source = target.Target.InputCoordinates.Coordinates;
                    wait.Coordinates.Coordinates.RA.Should().BeApproximately(21.5575056, Tolerance, "loading must repair coordinates before any scalar read or validation");
                    wait.Coordinates.Coordinates.Dec.Should().BeApproximately(-0.82325, Tolerance);
                    wait.RaExpression.Definition.Should().BeEmpty();
                    wait.DecExpression.Definition.Should().BeEmpty();
                    wait.Data.Coordinates.Should().BeSameAs(wait.Coordinates);
                    wait.Coordinates.Coordinates.Should().NotBeSameAs(source);
                    _ = wait.Ra;
                    _ = wait.Dec;
                    ((NINA.Sequencer.Validations.IValidatable)wait).Validate();
                    wait.Coordinates.Coordinates.RA.Should().BeApproximately(source.RA, Tolerance);
                    wait.Coordinates.Coordinates.Dec.Should().BeApproximately(source.Dec, Tolerance);
                    source.RA.Should().BeApproximately(21.5575056, Tolerance);
                    source.Dec.Should().BeApproximately(-0.82325, Tolerance);
                    var utc = new DateTime(2026, 1, 1, 22, 0, 0, DateTimeKind.Utc);
                    var expectedPosition = source.Clone().Transform(Angle.ByDegree(47), Angle.ByDegree(11), profile.AstrometrySettings.Elevation, utc);
                    wait.Data.UpdateCurrentTargetPosition(utc).Should().BeTrue();
                    wait.Data.CurrentAltitude.Should().BeApproximately(expectedPosition.Altitude.Degree, Tolerance);
                    var expectedThreshold = altitudeWait ? wait.Data.Offset : profile.AstrometrySettings.Horizon.GetAltitude(expectedPosition.Azimuth.Degree) + wait.Data.Offset;
                    wait.Data.TargetAltitude.Should().BeApproximately(expectedThreshold, Tolerance);
                    result = converter.Deserialize(converter.Serialize(result));
                }
            } finally {
                JsonConvert.DefaultSettings = previous;
                File.Delete(temporaryPath);
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void FramingUpdateExistingTargetCommand_RoundTripPreservesNestedWaitCoordinates() {
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(new System.Collections.Generic.List<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequenceRootContainer>()).Returns(() => new SequenceRootContainer());
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => (SequentialContainer)new SequentialContainer().Clone());
            factory.Setup(x => x.GetContainer<DeepSkyObjectContainer>()).Returns(() => (DeepSkyObjectContainer)CreateTargetContainer().Clone());
            factory.Setup(x => x.GetItem<WaitUntilAboveHorizon>()).Returns(() => (WaitUntilAboveHorizon)new WaitUntilAboveHorizon(profileServiceMock.Object).Clone());
            var converter = new SequenceJsonConverter(factory.Object);
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Sequencer", "Serialization", "LegacySequences", "InheritedWaitCoordinates.json");
            var root = converter.Deserialize(File.ReadAllText(path));
            var target = (DeepSkyObjectContainer)root.Items[0];
            var framing = (FramingAssistantVM)RuntimeHelpers.GetUninitializedObject(typeof(FramingAssistantVM));
            var sequenceMediator = new Mock<ISequenceMediator>();
            sequenceMediator.SetupGet(x => x.Initialized).Returns(true);
            sequenceMediator.Setup(x => x.GetAllTargetsInSimpleSequence()).Returns(new System.Collections.Generic.List<IDeepSkyObjectContainer>());
            sequenceMediator.Setup(x => x.GetAllTargetsInAdvancedSequence()).Returns(new System.Collections.Generic.List<IDeepSkyObjectContainer> { target });
            foreach (var field in new (string Name, object Value)[] {
                ("sequenceMediator", sequenceMediator.Object),
                ("applicationMediator", new Mock<IApplicationMediator>().Object),
                ("skyMapAnnotator", Mock.Of<ISkyMapAnnotator>()),
                ("_rectangleCalculated", true)
            }) {
                typeof(FramingAssistantVM).GetField(field.Name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(framing, field.Value);
            }
            typeof(FramingAssistantVM).GetMethod("InitializeCommands", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(framing, null);
            var updated = new Coordinates(Angle.ByHours(5.25), Angle.ByDegree(-0.375), Epoch.J2000);
            framing.CameraRectangles.Add(new FramingRectangle(0, 0, 0, 200, 200) { Coordinates = updated, DSOPositionAngle = 27 });
            framing.GetExistingSequencerTargetsCommand.Execute(null);
            framing.UpdateExistingTargetInSequencerCommand.CanExecute(target).Should().BeTrue();

            framing.UpdateExistingTargetInSequencerCommand.Execute(target);
            var loaded = converter.Deserialize(converter.Serialize(root));

            var loadedTarget = (DeepSkyObjectContainer)loaded.Items[0];
            var loadedWait = (WaitUntilAboveHorizon)((SequentialContainer)loadedTarget.Items[0]).Items[0];
            loadedWait.Coordinates.Coordinates.RA.Should().BeApproximately(5.25, Tolerance);
            loadedWait.Coordinates.Coordinates.Dec.Should().BeApproximately(-0.375, Tolerance);
            loadedWait.Data.Coordinates.Should().BeSameAs(loadedWait.Coordinates);
            loadedTarget.Target.PositionAngle.Should().Be(27);
            loadedTarget.Target.InputCoordinates.Coordinates.RA.Should().BeApproximately(5.25, Tolerance);
            loadedTarget.Target.InputCoordinates.Coordinates.Dec.Should().BeApproximately(-0.375, Tolerance);
        }

        [TestCase(CenterInstruction)]
        [TestCase(CenterAndRotateInstruction)]
        [TestCase(SlewScopeToRaDecInstruction)]
        [TestCase(WaitForAltitudeInstruction)]
        [TestCase(WaitUntilAboveHorizonInstruction)]
        public void FailedExpressions_DoNotReplaceLastValidCoordinates(string instructionName) {
            var instruction = CreateInstruction(instructionName);
            new SequenceRootContainer().Add(instruction);
            instruction.RaExpression.Definition = "5+5";
            instruction.DecExpression.Definition = "-1/2";
            _ = instruction.Ra;
            _ = instruction.Dec;
            instruction.RaExpression.Definition = "100";
            instruction.DecExpression.Definition = "invalid(";
            _ = instruction.Ra;
            _ = instruction.Dec;

            instruction.Coordinates.Coordinates.RA.Should().Be(10);
            instruction.Coordinates.Coordinates.Dec.Should().Be(-0.5);
            instruction.RaExpression.Error.Should().NotBeNullOrEmpty();
            instruction.DecExpression.Error.Should().NotBeNullOrEmpty();
        }

        [TestCase(false, false, false)]
        [TestCase(false, true, true)]
        [TestCase(true, false, false)]
        public void WaitReachability_UsesSignedFractionalDeclination(bool horizon, bool below, bool reachable) {
            profile.AstrometrySettings.Latitude = 89;
            CoordinatesInstruction instruction = horizon
                ? new WaitUntilAboveHorizon(profileServiceMock.Object) : new WaitForAltitude(profileServiceMock.Object);
            new SequenceRootContainer().Add(instruction);
            instruction.Coordinates.Coordinates = new Coordinates(Angle.ByHours(5), Angle.ByDegree(-0.5), Epoch.J2000);
            if (instruction is WaitForAltitude altitude) {
                altitude.AboveOrBelow = below ? "<" : ">";
            }
            instruction.Offset = below ? -1.25 : 0.75;

            ((NINA.Sequencer.Validations.IValidatable)instruction).Validate().Should().Be(reachable);
        }

        [TestCase(CenterInstruction)]
        [TestCase(CenterAndRotateInstruction)]
        [TestCase(SlewScopeToRaDecInstruction)]
        [TestCase(WaitForAltitudeInstruction)]
        [TestCase(WaitUntilAboveHorizonInstruction)]
        public void InheritedInvalidLocalDefinitions_AreDormantUntilDetach(string instructionName) {
            var target = CreateTargetContainer();
            new SequenceRootContainer().Add(target);
            SetTarget(target, new Coordinates(Angle.ByHours(21), Angle.ByDegree(-0.5), Epoch.J2000), 15);
            var instruction = CreateInstruction(instructionName);
            instruction.RaExpression.Definition = "invalid(";
            instruction.DecExpression.Definition = "invalid(";
            target.Add(instruction);

            instruction.Validate().Should().BeTrue();
            ((NINA.Sequencer.Validations.IValidatable)instruction).Validate().Should().BeTrue();
            _ = instruction.Ra;
            _ = instruction.Dec;
            instruction.Coordinates.Coordinates.RA.Should().Be(21);
            instruction.Coordinates.Coordinates.Dec.Should().Be(-0.5);
            instruction.RaExpression.Definition.Should().Be("invalid(");
            instruction.DecExpression.Definition.Should().Be("invalid(");

            instruction.AttachNewParent(null);

            instruction.Inherited.Should().BeFalse();
            instruction.Coordinates.Coordinates.RA.Should().Be(0);
            instruction.Coordinates.Coordinates.Dec.Should().Be(0);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProtectedCoordinateWrite_ObserverExceptionRestoresProtection(bool alreadyProtected) {
            var instruction = new CoordinatesInstruction();
            new SequenceRootContainer().Add(instruction);
            instruction.DecExpression.Definition = "1/2";
            var protection = typeof(CoordinatesInstruction).GetField("Protect", BindingFlags.Instance | BindingFlags.NonPublic)!;
            protection.SetValue(instruction, alreadyProtected);
            PropertyChangedEventHandler observer = (_, e) => {
                if (e.PropertyName == nameof(InputCoordinates.Coordinates)) {
                    throw new InvalidOperationException("observer failure");
                }
            };
            instruction.Coordinates.PropertyChanged += observer;
            var context = new ContextCoordinates(new Coordinates(Angle.ByHours(3), Angle.ByDegree(-0.5), Epoch.J2000), 10, null);
            var apply = typeof(CoordinatesInstruction).GetMethod("ApplyInheritedCoordinates", BindingFlags.Instance | BindingFlags.NonPublic)!;

            Action action = () => apply.Invoke(instruction, new object[] { context });

            action.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();
            protection.GetValue(instruction).Should().Be(alreadyProtected);
            instruction.Coordinates.PropertyChanged -= observer;
            protection.SetValue(instruction, false);
            instruction.Coordinates.RAHours = 8;
            instruction.RaExpression.Definition.Should().Be("8");
            instruction.DecExpression.Definition.Should().Be("1/2");
        }

        [TestCase(CenterInstruction)]
        [TestCase(CenterAndRotateInstruction)]
        [TestCase(SlewScopeToRaDecInstruction)]
        [TestCase(WaitForAltitudeInstruction)]
        [TestCase(WaitUntilAboveHorizonInstruction)]
        public void Attachment_ReentrantScalarReadCannotOverwriteInheritedCoordinates(string instructionName) {
            var target = CreateTargetContainer();
            var root = new SequenceRootContainer();
            root.Add(target);
            var source = new Coordinates(Angle.ByHours(21.5575056), Angle.ByDegree(-0.82325), Epoch.J2000);
            SetTarget(target, source, 15);
            var instruction = CreateInstruction(instructionName);
            instruction.RaExpression.Definition = "1+1";
            instruction.DecExpression.Definition = "3+4";
            bool observed = false;
            instruction.Coordinates.PropertyChanged += (_, e) => {
                if (observed || e.PropertyName != nameof(InputCoordinates.Coordinates)) {
                    return;
                }

                observed = true;
                _ = instruction.Ra;
                _ = instruction.Dec;
            };

            target.Add(instruction);

            observed.Should().BeTrue();
            instruction.Inherited.Should().BeTrue();
            instruction.Coordinates.Coordinates.RA.Should().BeApproximately(21.5575056, Tolerance);
            instruction.Coordinates.Coordinates.Dec.Should().BeApproximately(-0.82325, Tolerance);
            source.RA.Should().BeApproximately(21.5575056, Tolerance);
            source.Dec.Should().BeApproximately(-0.82325, Tolerance);
            instruction.RaExpression.Definition.Should().Be("1+1");
            instruction.DecExpression.Definition.Should().Be("3+4");
        }

    }
}
