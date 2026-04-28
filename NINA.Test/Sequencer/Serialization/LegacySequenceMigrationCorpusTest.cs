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
using Newtonsoft.Json.Linq;
using NINA.Astrometry.Interfaces;
using NINA.Core.Interfaces;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Media;

namespace NINA.Test.Sequencer.Serialization {

    [TestFixture]
    [NonParallelizable]
    public class LegacySequenceMigrationCorpusTest {
        private const string Master32Commit = "2393eae581145ed5b8114bf07c48ca2580540fd5";
        private static readonly string[] CorpusPathParts = { "Sequencer", "Serialization", "LegacySequences", "v3.2" };

        private static readonly ISet<string> IgnoredJsonProperties = new HashSet<string>(StringComparer.Ordinal) {
            "$id",
            "$type",
            "$values",
            "Conditions",
            "DropIntoCommand",
            "DropIntoConditionsCommand",
            "DropIntoTriggersCommand",
            "HasChanged",
            "HasDsoParent",
            "Icon",
            "Issues",
            "Items",
            "Parent",
            "ResetProgressCommand",
            "ShowMenu",
            "Strategy",
            "Status",
            "SymbolBroker",
            "Triggers",
            "TriggerRunner"
        };

        /// <summary>
        /// Guards the golden input itself: this corpus must remain a complete 3.2 master snapshot,
        /// otherwise the migration test can pass while silently dropping an old sequencer export.
        /// </summary>
        [Test]
        public void LegacyMasterCorpus_CoversEveryMasterSequencerExportWithDefaultAndPopulatedVariants() {
            foreach (LegacyCorpusFile corpus in LoadCorpusFiles()) {
                JObject legacyJson = ReadJsonObject(corpus.SequencePath);
                LegacyManifest manifest = ReadManifest(corpus.ManifestPath);
                ISet<string> serializedEntityTypes = TraverseJsonEntities(legacyJson)
                    .Select(x => ExtractTypeName(x.Json))
                    .Where(x => x != null)
                    .Select(x => x!)
                    .ToHashSet(StringComparer.Ordinal);

                manifest.SourceBranch.Should().Be("master");
                manifest.SourceCommit.Should().Be(Master32Commit);
                manifest.Items.Should().HaveCount(65);
                manifest.Conditions.Should().HaveCount(10);
                manifest.Triggers.Should().HaveCount(12);
                manifest.Containers.Should().HaveCount(14);
                manifest.Entities.Should().HaveCount(200);

                manifest.Items
                    .Concat(manifest.Conditions)
                    .Concat(manifest.Triggers)
                    .Concat(manifest.Containers)
                    .Except(serializedEntityTypes, StringComparer.Ordinal)
                    .Should()
                    .BeEmpty("the legacy sequence JSON must contain every exported 3.2 sequencer entity from the manifest");

                manifest.Entities
                    .GroupBy(x => $"{x.Kind}:{x.Type}", StringComparer.Ordinal)
                    .Select(x => new { Entity = x.Key, Variants = x.Select(y => y.Variant).ToHashSet(StringComparer.Ordinal) })
                    .Where(x => !x.Variants.SetEquals(new[] { "Default", "Populated" }))
                    .Should()
                    .BeEmpty("each non-root master entity in the corpus should have both a default and a populated-value permutation");
            }
        }

        /// <summary>
        /// Exercises the compatibility contract that existing 3.2 user sequence files migrate through
        /// the current symbol-aware serializers without culture-dependent parsing, unknown fallbacks,
        /// scalar value drift, or expression-definition drift after a current-version save round-trip.
        /// </summary>
        [Test]
        public void LegacyMasterCorpus_MigratesUnderNonInvariantCultureWithoutUnknownEntitiesOrValueDrift() {
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

                using CurrentSequencerCatalog catalog = CurrentSequencerCatalog.Create();
                SequenceJsonConverter converter = new SequenceJsonConverter(catalog.Factory);

                foreach (LegacyCorpusFile corpus in LoadCorpusFiles()) {
                    string legacyJsonText = File.ReadAllText(corpus.SequencePath);
                    JObject legacyJson = ReadJsonObject(corpus.SequencePath);
                    IReadOnlyList<JsonEntityNode> legacyNodes = TraverseJsonEntities(legacyJson).ToList();

                    ISequenceContainer migrated = converter.Deserialize(legacyJsonText, corpus.SequencePath);
                    IReadOnlyList<EntityNode> migratedNodes = TraverseEntities(migrated).ToList();

                    AssertNoUnknownEntities(migratedNodes, "every built-in 3.2 entity should resolve to a current sequencer type");

                    FindValueMismatches(legacyNodes, migratedNodes)
                        .Should()
                        .BeEmpty("old scalar JSON values must survive migration unchanged");

                    FindExpressionMismatches(legacyNodes, migratedNodes)
                        .Should()
                        .BeEmpty("old scalar JSON values must also hydrate expression definitions and evaluated expression values");

                    AssertPopulatedSwitchFilterWasMigrated(migratedNodes);

                    string currentJsonText = converter.Serialize(migrated);
                    ISequenceContainer roundTripped = converter.Deserialize(currentJsonText, corpus.SequencePath);
                    IReadOnlyList<EntityNode> roundTrippedNodes = TraverseEntities(roundTripped).ToList();

                    AssertNoUnknownEntities(roundTrippedNodes, "current-version serialization should not introduce unknown entities");

                    CreateEntityTypeDigest(roundTrippedNodes)
                        .Should()
                        .Equal(CreateEntityTypeDigest(migratedNodes), "current-version serialization should preserve the migrated sequence structure");

                    FindExpressionMismatches(legacyNodes, roundTrippedNodes)
                        .Should()
                        .BeEmpty("expression-backed migrated values must survive current-version serialization");

                    AssertPopulatedSwitchFilterWasMigrated(roundTrippedNodes);
                }
            } finally {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }

        /// <summary>
        /// Finds the copied legacy corpus manifests in the test output and pairs each manifest with its sequence JSON file.
        /// </summary>
        private static IReadOnlyList<LegacyCorpusFile> LoadCorpusFiles() {
            string corpusDirectory = Path.Combine(new[] { TestContext.CurrentContext.TestDirectory }.Concat(CorpusPathParts).ToArray());
            Directory.Exists(corpusDirectory).Should().BeTrue("legacy sequence corpus files must be copied to the test output");

            IReadOnlyList<LegacyCorpusFile> corpusFiles = Directory
                .EnumerateFiles(corpusDirectory, "*.manifest.json")
                .OrderBy(x => x, StringComparer.Ordinal)
                .Select(x => new LegacyCorpusFile(
                    x,
                    Path.Combine(Path.GetDirectoryName(x)!, ReadManifest(x).SequenceFile)))
                .ToList();

            corpusFiles.Should().NotBeEmpty("at least one legacy sequence migration corpus is expected");
            return corpusFiles;
        }

        /// <summary>
        /// Reads the generated corpus manifest so the test can assert the source commit and entity coverage.
        /// </summary>
        private static LegacyManifest ReadManifest(string path) {
            return JsonConvert.DeserializeObject<LegacyManifest>(File.ReadAllText(path))!;
        }

        /// <summary>
        /// Loads legacy JSON without automatic date parsing so persisted scalar values are compared in their original shape.
        /// </summary>
        private static JObject ReadJsonObject(string path) {
            using StringReader stringReader = new StringReader(File.ReadAllText(path));
            using JsonTextReader jsonReader = new JsonTextReader(stringReader) {
                DateParseHandling = DateParseHandling.None
            };
            return JObject.Load(jsonReader);
        }

        /// <summary>
        /// Starts a path-preserving walk of a legacy sequence JSON entity tree from the root container.
        /// </summary>
        private static IEnumerable<JsonEntityNode> TraverseJsonEntities(JObject entityJson) {
            return TraverseJsonEntities(entityJson, "$");
        }

        /// <summary>
        /// Walks legacy JSON entities in the same structural order as the current object graph traversal.
        /// </summary>
        private static IEnumerable<JsonEntityNode> TraverseJsonEntities(JObject entityJson, string path) {
            if (entityJson.ContainsKey("$ref")) {
                yield break;
            }

            yield return new JsonEntityNode(path, entityJson);

            foreach (JsonEntityNode condition in TraverseJsonCollection(entityJson["Conditions"], $"{path}.Conditions")) {
                yield return condition;
            }

            foreach (JsonEntityNode trigger in TraverseJsonCollection(entityJson["Triggers"], $"{path}.Triggers")) {
                yield return trigger;
            }

            if (entityJson["TriggerRunner"] is JObject triggerRunner) {
                foreach (JsonEntityNode triggerRunnerNode in TraverseJsonEntities(triggerRunner, $"{path}.TriggerRunner")) {
                    yield return triggerRunnerNode;
                }
            }

            foreach (JsonEntityNode item in TraverseJsonCollection(entityJson["Items"], $"{path}.Items")) {
                yield return item;
            }
        }

        /// <summary>
        /// Expands a Json.NET-preserved collection node so child entities receive stable comparison paths.
        /// </summary>
        private static IEnumerable<JsonEntityNode> TraverseJsonCollection(JToken? token, string path) {
            if (token is not JObject collection || collection["$values"] is not JArray values) {
                yield break;
            }

            for (int index = 0; index < values.Count; index++) {
                if (values[index] is JObject child) {
                    foreach (JsonEntityNode node in TraverseJsonEntities(child, $"{path}[{index}]")) {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// Starts a path-preserving walk of the migrated current sequencer object graph.
        /// </summary>
        private static IEnumerable<EntityNode> TraverseEntities(ISequenceEntity entity) {
            return TraverseEntities(entity, "$");
        }

        /// <summary>
        /// Walks the current sequencer object graph in the same structural order as the legacy JSON traversal.
        /// </summary>
        private static IEnumerable<EntityNode> TraverseEntities(ISequenceEntity entity, string path) {
            yield return new EntityNode(path, entity);

            if (entity is IConditionable conditionable) {
                for (int index = 0; index < conditionable.Conditions.Count; index++) {
                    foreach (EntityNode condition in TraverseEntities(conditionable.Conditions[index], $"{path}.Conditions[{index}]")) {
                        yield return condition;
                    }
                }
            }

            if (entity is ITriggerable triggerable) {
                for (int index = 0; index < triggerable.Triggers.Count; index++) {
                    foreach (EntityNode trigger in TraverseEntities(triggerable.Triggers[index], $"{path}.Triggers[{index}]")) {
                        yield return trigger;
                    }
                }
            }

            PropertyInfo? triggerRunnerProperty = entity.GetType().GetProperty("TriggerRunner", BindingFlags.Instance | BindingFlags.Public);
            if (triggerRunnerProperty?.GetValue(entity) is ISequenceEntity triggerRunner) {
                foreach (EntityNode triggerRunnerNode in TraverseEntities(triggerRunner, $"{path}.TriggerRunner")) {
                    yield return triggerRunnerNode;
                }
            }

            if (entity is ISequenceContainer container) {
                for (int index = 0; index < container.Items.Count; index++) {
                    foreach (EntityNode item in TraverseEntities(container.Items[index], $"{path}.Items[{index}]")) {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the unqualified assembly type name from legacy JSON so manifest coverage ignores assembly details.
        /// </summary>
        private static string? ExtractTypeName(JObject entityJson) {
            string? assemblyQualifiedName = entityJson["$type"]?.Value<string>();
            return assemblyQualifiedName?.Split(',')[0];
        }

        /// <summary>
        /// Compares every comparable persisted legacy scalar against the migrated entity property at the same path.
        /// </summary>
        private static IReadOnlyList<string> FindValueMismatches(
                IReadOnlyList<JsonEntityNode> legacyNodes,
                IReadOnlyList<EntityNode> migratedNodes
        ) {
            IDictionary<string, EntityNode> migratedByPath = migratedNodes.ToDictionary(x => x.Path, StringComparer.Ordinal);
            List<string> mismatches = new List<string>();
            int comparedValues = 0;

            foreach (JsonEntityNode legacyNode in legacyNodes) {
                if (!migratedByPath.TryGetValue(legacyNode.Path, out EntityNode? migratedNode)) {
                    mismatches.Add($"{legacyNode.Path}: migrated entity is missing");
                    continue;
                }

                foreach (ComparableJsonValue expectedValue in GetComparableValues(legacyNode.Json)) {
                    comparedValues++;
                    if (!TryGetCurrentComparableValue(migratedNode.Entity, expectedValue, out string actualValue)) {
                        mismatches.Add($"{legacyNode.Path}.{expectedValue.DisplayPath}: migrated entity no longer exposes this persisted value");
                        continue;
                    }

                    if (!StringComparer.Ordinal.Equals(expectedValue.NormalizedValue, actualValue)) {
                        mismatches.Add($"{legacyNode.Path}.{expectedValue.DisplayPath}: expected {expectedValue.NormalizedValue}, got {actualValue}");
                    }
                }
            }

            comparedValues.Should().BeGreaterThan(400, "the corpus should compare a broad set of migrated sequence values");
            return mismatches;
        }

        /// <summary>
        /// Verifies legacy scalar and nested values also hydrate current expression objects, not only scalar getters.
        /// </summary>
        private static IReadOnlyList<string> FindExpressionMismatches(
                IReadOnlyList<JsonEntityNode> legacyNodes,
                IReadOnlyList<EntityNode> currentNodes
        ) {
            IDictionary<string, EntityNode> currentByPath = currentNodes.ToDictionary(x => x.Path, StringComparer.Ordinal);
            List<string> mismatches = new List<string>();
            int checkedExpressions = 0;
            int checkedDefinitions = 0;

            foreach (JsonEntityNode legacyNode in legacyNodes) {
                if (!currentByPath.TryGetValue(legacyNode.Path, out EntityNode? currentNode)) {
                    continue;
                }

                foreach (ExpressionExpectation expectation in CreateExpressionExpectations(legacyNode.Json, currentNode.Entity)) {
                    checkedExpressions++;
                    string location = $"{legacyNode.Path}.{expectation.ExpressionPropertyName} <= {expectation.SourcePath}";

                    if (!TryGetExpression(currentNode.Entity, expectation.ExpressionPropertyName, out Expression expression)) {
                        mismatches.Add($"{location}: current entity does not expose the expected expression");
                        continue;
                    }

                    if (expectation.NumericValue.HasValue) {
                        expression.Evaluate(true);
                        if (HasHardExpressionError(expression)) {
                            mismatches.Add($"{location}: expression has error '{expression.Error}'");
                        }

                        double expectedValue = expectation.NumericValue.Value;
                        if (!NumbersApproximatelyEqual(expression.Value, expectedValue)) {
                            mismatches.Add($"{location}: expected evaluated value {FormatInvariant(expectedValue)}, got {FormatInvariant(expression.Value)}");
                        }

                        bool definitionRequired = RequiresExpressionDefinition(expression, expectedValue);
                        if (definitionRequired) {
                            checkedDefinitions++;
                        }

                        if (definitionRequired || !string.IsNullOrWhiteSpace(expression.Definition)) {
                            FindNumericDefinitionMismatches(location, expression.Definition, expectedValue, definitionRequired, mismatches);
                        }
                    } else if (expectation.StringValue != null) {
                        checkedDefinitions++;
                        if (string.IsNullOrWhiteSpace(expression.Definition)) {
                            mismatches.Add($"{location}: expected expression definition '{expectation.StringValue}', got an empty definition");
                        } else if (!StringComparer.Ordinal.Equals(expression.Definition, expectation.StringValue)) {
                            mismatches.Add($"{location}: expected expression definition '{expectation.StringValue}', got '{expression.Definition}'");
                        }
                    }
                }
            }

            checkedExpressions.Should().BeGreaterThan(50, "the corpus should cover expression-backed migrated values across many sequencer entities");
            checkedDefinitions.Should().BeGreaterThan(10, "the populated corpus variants should force non-default expression backfills");
            return mismatches;
        }

        /// <summary>
        /// Builds expression expectations from legacy JSON, including special nested fields that map to generated expressions.
        /// </summary>
        private static IReadOnlyList<ExpressionExpectation> CreateExpressionExpectations(JObject legacyJson, ISequenceEntity entity) {
            ISet<string> expressionPropertyNames = GetExpressionPropertyNames(entity.GetType());
            if (expressionPropertyNames.Count == 0) {
                return Array.Empty<ExpressionExpectation>();
            }

            Dictionary<string, ExpressionExpectation> expectations = new Dictionary<string, ExpressionExpectation>(StringComparer.Ordinal);

            foreach (string expressionPropertyName in expressionPropertyNames) {
                string valuePropertyName = expressionPropertyName[..^"Expression".Length];
                if (TryReadNumberPath(legacyJson, valuePropertyName, out double numericValue)) {
                    AddNumericExpressionExpectation(expectations, expressionPropertyName, valuePropertyName, numericValue);
                } else if (TryReadStringPath(legacyJson, valuePropertyName, out string stringValue)) {
                    AddStringExpressionExpectation(expectations, expressionPropertyName, valuePropertyName, stringValue);
                }
            }

            if (expressionPropertyNames.Contains("ROIPctExpression") &&
                    TryReadNumberPath(legacyJson, "ROI", out double roiValue)) {
                AddNumericExpressionExpectation(expectations, "ROIPctExpression", "ROI", roiValue * 100d);
            }

            if (expressionPropertyNames.Contains("OffsetExpression") &&
                    TryReadNumberPath(legacyJson, "Data.Offset", out double dataOffset)) {
                AddNumericExpressionExpectation(expectations, "OffsetExpression", "Data.Offset", dataOffset);
            }

            if (expressionPropertyNames.Contains("IterationsExpression") &&
                    !TryReadNumberPath(legacyJson, "Iterations", out _) &&
                    TryReadFirstLoopConditionIterations(legacyJson, out double loopIterations)) {
                AddNumericExpressionExpectation(expectations, "IterationsExpression", "Conditions[0].Iterations", loopIterations);
            }

            if (expressionPropertyNames.Contains("XfilterExpression") &&
                    TryReadStringPath(legacyJson, "Filter._name", out string filterName)) {
                AddStringExpressionExpectation(expectations, "XfilterExpression", "Filter._name", SymbolBroker.SanitizeIdentifier(filterName));
            }

            AddEquatorialCoordinateExpectations(expressionPropertyNames, legacyJson, expectations, "InputCoordinates");
            AddEquatorialCoordinateExpectations(expressionPropertyNames, legacyJson, expectations, "Coordinates");
            AddEquatorialCoordinateExpectations(expressionPropertyNames, legacyJson, expectations, "Data.Coordinates");
            AddTopocentricCoordinateExpectations(expressionPropertyNames, legacyJson, expectations, "Coordinates");

            return expectations.Values.ToList();
        }

        /// <summary>
        /// Discovers generated expression properties on a current entity without depending on the source generator internals.
        /// </summary>
        private static ISet<string> GetExpressionPropertyNames(Type entityType) {
            return entityType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.PropertyType == typeof(Expression) &&
                            x.Name.EndsWith("Expression", StringComparison.Ordinal) &&
                            x.GetIndexParameters().Length == 0)
                .Select(x => x.Name)
                .ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>
        /// Adds RA/Dec expression expectations for legacy coordinate objects that used component fields.
        /// </summary>
        private static void AddEquatorialCoordinateExpectations(
                ISet<string> expressionPropertyNames,
                JObject legacyJson,
                IDictionary<string, ExpressionExpectation> expectations,
                string sourcePath
        ) {
            if (!TryReadEquatorialCoordinates(legacyJson, sourcePath, out double rightAscension, out double declination)) {
                return;
            }

            if (expressionPropertyNames.Contains("RaExpression")) {
                AddNumericExpressionExpectation(expectations, "RaExpression", $"{sourcePath}.RA", rightAscension);
            }

            if (expressionPropertyNames.Contains("DecExpression")) {
                AddNumericExpressionExpectation(expectations, "DecExpression", $"{sourcePath}.Dec", declination);
            }
        }

        /// <summary>
        /// Adds Alt/Az expression expectations for legacy topocentric coordinate objects that used component fields.
        /// </summary>
        private static void AddTopocentricCoordinateExpectations(
                ISet<string> expressionPropertyNames,
                JObject legacyJson,
                IDictionary<string, ExpressionExpectation> expectations,
                string sourcePath
        ) {
            if (!TryReadTopocentricCoordinates(legacyJson, sourcePath, out double azimuth, out double altitude)) {
                return;
            }

            if (expressionPropertyNames.Contains("AzExpression")) {
                AddNumericExpressionExpectation(expectations, "AzExpression", $"{sourcePath}.Az", azimuth);
            }

            if (expressionPropertyNames.Contains("AltExpression")) {
                AddNumericExpressionExpectation(expectations, "AltExpression", $"{sourcePath}.Alt", altitude);
            }
        }

        /// <summary>
        /// Stores a numeric expression expectation keyed by expression property so special mappings can override generic ones.
        /// </summary>
        private static void AddNumericExpressionExpectation(
                IDictionary<string, ExpressionExpectation> expectations,
                string expressionPropertyName,
                string sourcePath,
                double value
        ) {
            expectations[expressionPropertyName] = new ExpressionExpectation(expressionPropertyName, sourcePath, value, null);
        }

        /// <summary>
        /// Stores a string expression expectation for migrated symbol-style expressions such as filter names.
        /// </summary>
        private static void AddStringExpressionExpectation(
                IDictionary<string, ExpressionExpectation> expectations,
                string expressionPropertyName,
                string sourcePath,
                string value
        ) {
            expectations[expressionPropertyName] = new ExpressionExpectation(expressionPropertyName, sourcePath, null, value);
        }

        /// <summary>
        /// Reads a generated expression property from the current entity when the migrated type exposes one.
        /// </summary>
        private static bool TryGetExpression(ISequenceEntity entity, string expressionPropertyName, out Expression expression) {
            PropertyInfo? property = entity.GetType().GetProperty(expressionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetValue(entity) is Expression resolvedExpression) {
                expression = resolvedExpression;
                return true;
            }

            expression = null!;
            return false;
        }

        /// <summary>
        /// Converts legacy RA/Dec component fields into the decimal values stored by current RA/Dec expressions.
        /// </summary>
        private static bool TryReadEquatorialCoordinates(JObject root, string path, out double rightAscension, out double declination) {
            rightAscension = 0;
            declination = 0;

            if (!TryReadObjectPath(root, path, out JObject coordinates) ||
                    !TryReadNumberPath(coordinates, "RAHours", out double hours) ||
                    !TryReadNumberPath(coordinates, "RAMinutes", out double minutes) ||
                    !TryReadNumberPath(coordinates, "RASeconds", out double seconds) ||
                    !TryReadNumberPath(coordinates, "DecDegrees", out double degrees) ||
                    !TryReadNumberPath(coordinates, "DecMinutes", out double decMinutes) ||
                    !TryReadNumberPath(coordinates, "DecSeconds", out double decSeconds)) {
                return false;
            }

            bool negativeDeclination = degrees < 0 || coordinates["NegativeDec"]?.Value<bool>() == true;
            rightAscension = hours + (minutes / 60d) + (seconds / 3600d);
            declination = ApplySign(Math.Abs(degrees), decMinutes, decSeconds, negativeDeclination);
            return true;
        }

        /// <summary>
        /// Converts legacy Alt/Az component fields into the decimal values stored by current Alt/Az expressions.
        /// </summary>
        private static bool TryReadTopocentricCoordinates(JObject root, string path, out double azimuth, out double altitude) {
            azimuth = 0;
            altitude = 0;

            if (!TryReadObjectPath(root, path, out JObject coordinates) ||
                    !TryReadNumberPath(coordinates, "AzDegrees", out double azimuthDegrees) ||
                    !TryReadNumberPath(coordinates, "AzMinutes", out double azimuthMinutes) ||
                    !TryReadNumberPath(coordinates, "AzSeconds", out double azimuthSeconds) ||
                    !TryReadNumberPath(coordinates, "AltDegrees", out double altitudeDegrees) ||
                    !TryReadNumberPath(coordinates, "AltMinutes", out double altitudeMinutes) ||
                    !TryReadNumberPath(coordinates, "AltSeconds", out double altitudeSeconds)) {
                return false;
            }

            azimuth = ApplySign(Math.Abs(azimuthDegrees), azimuthMinutes, azimuthSeconds, azimuthDegrees < 0);
            altitude = ApplySign(Math.Abs(altitudeDegrees), altitudeMinutes, altitudeSeconds, altitudeDegrees < 0);
            return true;
        }

        /// <summary>
        /// Reads a numeric token by dotted JSON path using invariant conversion for culture-stable expectations.
        /// </summary>
        private static bool TryReadNumberPath(JObject root, string path, out double value) {
            value = 0;
            if (!TryReadTokenPath(root, path, out JToken? token) ||
                    token is not JValue jValue ||
                    (jValue.Type != JTokenType.Integer && jValue.Type != JTokenType.Float)) {
                return false;
            }

            value = Convert.ToDouble(jValue.Value, CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// Reads the legacy nested loop count used by immutable imaging containers before they gained their own IterationsExpression.
        /// </summary>
        private static bool TryReadFirstLoopConditionIterations(JObject root, out double value) {
            value = 0;
            if (root["Conditions"] is not JObject conditions ||
                    conditions["$values"] is not JArray values ||
                    values.Count == 0 ||
                    values[0] is not JObject loopConditionJson) {
                return false;
            }

            return TryReadNumberPath(loopConditionJson, "Iterations", out value);
        }

        /// <summary>
        /// Reads a string token by dotted JSON path for expression definitions that are symbol identifiers.
        /// </summary>
        private static bool TryReadStringPath(JObject root, string path, out string value) {
            value = string.Empty;
            if (!TryReadTokenPath(root, path, out JToken? token) ||
                    token is not JValue jValue ||
                    jValue.Type != JTokenType.String) {
                return false;
            }

            value = jValue.Value<string>() ?? string.Empty;
            return true;
        }

        /// <summary>
        /// Reads an object token by dotted JSON path when special migration checks need nested legacy data.
        /// </summary>
        private static bool TryReadObjectPath(JObject root, string path, out JObject value) {
            if (!TryReadTokenPath(root, path, out JToken? token) || token is not JObject jObject) {
                value = null!;
                return false;
            }

            value = jObject;
            return true;
        }

        /// <summary>
        /// Resolves a simple dotted path inside a single legacy entity JSON object.
        /// </summary>
        private static bool TryReadTokenPath(JObject root, string path, out JToken? value) {
            value = root;
            foreach (string part in path.Split('.')) {
                if (value is not JObject currentObject || !currentObject.TryGetValue(part, out value)) {
                    value = null;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds failures for missing or culture-dependent numeric expression definitions after migration.
        /// </summary>
        private static void FindNumericDefinitionMismatches(
                string location,
                string definition,
                double expectedValue,
                bool definitionRequired,
                IList<string> mismatches
        ) {
            if (string.IsNullOrWhiteSpace(definition)) {
                if (definitionRequired) {
                    mismatches.Add($"{location}: expected a non-empty expression definition for {FormatInvariant(expectedValue)}");
                }
                return;
            }

            if (definition.Contains(',')) {
                mismatches.Add($"{location}: expression definition '{definition}' is not invariant-culture formatted");
                return;
            }

            if (!double.TryParse(definition, NumberStyles.Float, CultureInfo.InvariantCulture, out double definitionValue)) {
                mismatches.Add($"{location}: expression definition '{definition}' is not an invariant numeric literal");
                return;
            }

            if (!NumbersApproximatelyEqual(definitionValue, expectedValue)) {
                mismatches.Add($"{location}: expected expression definition {FormatInvariant(expectedValue)}, got '{definition}'");
            }
        }

        /// <summary>
        /// Decides whether an expression should carry an explicit definition instead of relying on default or auto values.
        /// </summary>
        private static bool RequiresExpressionDefinition(Expression expression, double expectedValue) {
            if (!double.IsNaN(expression.Default) && NumbersApproximatelyEqual(expectedValue, expression.Default)) {
                return false;
            }

            if (!double.IsNaN(expression.AutoValue) && NumbersApproximatelyEqual(expectedValue, expression.AutoValue)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Distinguishes blocking expression errors from warnings so migration does not accept invalid current expressions.
        /// </summary>
        private static bool HasHardExpressionError(Expression expression) {
            return !string.IsNullOrWhiteSpace(expression.Error) && !Expression.JustWarnings(expression.Error);
        }

        /// <summary>
        /// Applies a legacy coordinate sign after combining degree, minute, and second components.
        /// </summary>
        private static double ApplySign(double degrees, double minutes, double seconds, bool isNegative) {
            double value = degrees + (minutes / 60d) + (seconds / 3600d);
            return isNegative ? -value : value;
        }

        /// <summary>
        /// Compares decimalized legacy coordinate and expression values with tolerance for component conversion rounding.
        /// </summary>
        private static bool NumbersApproximatelyEqual(double actual, double expected) {
            return Math.Abs(actual - expected) <= 0.000001d;
        }

        /// <summary>
        /// Formats diagnostic numeric values with invariant culture so failures are readable under non-invariant tests.
        /// </summary>
        private static string FormatInvariant(double value) {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Enumerates persisted legacy scalar values while ignoring runtime-only serialization infrastructure.
        /// </summary>
        private static IEnumerable<ComparableJsonValue> GetComparableValues(JObject entityJson) {
            foreach (JProperty property in entityJson.Properties()) {
                if (IgnoredJsonProperties.Contains(property.Name)) {
                    continue;
                }

                foreach (ComparableJsonValue value in GetComparableValues(property.Name, property.Name, property.Value, depth: 0)) {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Recursively flattens shallow legacy value objects so nested settings are compared against current properties.
        /// </summary>
        private static IEnumerable<ComparableJsonValue> GetComparableValues(string propertyName, string displayPath, JToken token, int depth) {
            if (token is JValue jValue && TryNormalizeJValue(jValue, out string normalized)) {
                yield return new ComparableJsonValue(propertyName, displayPath, normalized);
                yield break;
            }

            if (depth >= 4 || token is not JObject jObject || jObject.ContainsKey("$ref") || jObject["$values"] != null) {
                yield break;
            }

            foreach (JProperty childProperty in jObject.Properties()) {
                if (IgnoredJsonProperties.Contains(childProperty.Name)) {
                    continue;
                }

                foreach (ComparableJsonValue value in GetComparableValues(
                             propertyName,
                             $"{displayPath}.{childProperty.Name}",
                             childProperty.Value,
                             depth + 1)) {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Reads the migrated current property corresponding to a legacy JSON value, including renamed private fields.
        /// </summary>
        private static bool TryGetCurrentComparableValue(ISequenceEntity entity, ComparableJsonValue expectedValue, out string normalizedValue) {
            object? current = entity;
            string[] propertyPath = expectedValue.DisplayPath.Split('.');

            foreach (string propertyName in propertyPath) {
                if (current == null) {
                    normalizedValue = NormalizeNull();
                    return true;
                }

                PropertyInfo? property = current.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                    ?? current.GetType().GetProperty(GetPublicPropertyNameForLegacyField(propertyName), BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.GetIndexParameters().Length > 0) {
                    normalizedValue = string.Empty;
                    return false;
                }

                current = property.GetValue(current);
            }

            return TryNormalizeObject(current, out normalizedValue);
        }

        /// <summary>
        /// Maps legacy serialized private backing fields such as _name onto their current public property names.
        /// </summary>
        private static string GetPublicPropertyNameForLegacyField(string propertyName) {
            return propertyName.Length > 1 && propertyName[0] == '_'
                ? $"{char.ToUpperInvariant(propertyName[1])}{propertyName[2..]}"
                : propertyName;
        }

        /// <summary>
        /// Normalizes legacy JSON scalar values to type-tagged strings for culture-stable equality checks.
        /// </summary>
        private static bool TryNormalizeJValue(JValue value, out string normalized) {
            if (value.Type == JTokenType.Null) {
                normalized = NormalizeNull();
                return true;
            }

            if (value.Type == JTokenType.Integer || value.Type == JTokenType.Float) {
                normalized = NormalizeNumber(Convert.ToDouble(value.Value, CultureInfo.InvariantCulture));
                return true;
            }

            if (value.Type == JTokenType.Boolean) {
                normalized = $"Boolean:{value.Value<bool>()}";
                return true;
            }

            if (value.Type == JTokenType.String) {
                string? stringValue = value.Value<string>();
                normalized = Guid.TryParse(stringValue, out Guid guid)
                    ? NormalizeGuid(guid)
                    : $"String:{stringValue}";
                return true;
            }

            normalized = string.Empty;
            return false;
        }

        /// <summary>
        /// Normalizes current object property values to the same type-tagged representation used for legacy JSON.
        /// </summary>
        private static bool TryNormalizeObject(object? value, out string normalized) {
            if (value == null) {
                normalized = NormalizeNull();
                return true;
            }

            Type type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
            if (type.IsEnum) {
                normalized = NormalizeNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                return true;
            }

            if (type == typeof(string)) {
                normalized = $"String:{value}";
                return true;
            }

            if (type == typeof(Guid)) {
                normalized = NormalizeGuid((Guid)value);
                return true;
            }

            if (type == typeof(bool)) {
                normalized = $"Boolean:{value}";
                return true;
            }

            if (type == typeof(byte) ||
                    type == typeof(sbyte) ||
                    type == typeof(short) ||
                    type == typeof(ushort) ||
                    type == typeof(int) ||
                    type == typeof(uint) ||
                    type == typeof(long) ||
                    type == typeof(ulong) ||
                    type == typeof(float) ||
                    type == typeof(double) ||
                    type == typeof(decimal)) {
                normalized = NormalizeNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                return true;
            }

            if (type == typeof(TimeSpan)) {
                normalized = $"TimeSpan:{((TimeSpan)value).Ticks}";
                return true;
            }

            if (type == typeof(DateTime)) {
                normalized = $"DateTime:{((DateTime)value).ToString("O", CultureInfo.InvariantCulture)}";
                return true;
            }

            normalized = string.Empty;
            return false;
        }

        /// <summary>
        /// Produces the shared sentinel used when legacy and current values are both null.
        /// </summary>
        private static string NormalizeNull() {
            return "<null>";
        }

        /// <summary>
        /// Formats numbers with full precision and invariant culture so value comparisons are not locale-dependent.
        /// </summary>
        private static string NormalizeNumber(double value) {
            return $"Number:{value.ToString("G17", CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Formats GUIDs consistently because legacy JSON stores them as strings while current properties may expose Guid.
        /// </summary>
        private static string NormalizeGuid(Guid value) {
            return $"Guid:{value:D}";
        }

        /// <summary>
        /// Fails the test when a legacy entity fell back to an unknown placeholder instead of a current sequencer type.
        /// </summary>
        private static void AssertNoUnknownEntities(IEnumerable<EntityNode> nodes, string because) {
            nodes
                .Where(x => x.Entity.GetType().Name.StartsWith("UnknownSequence", StringComparison.Ordinal))
                .Select(x => $"{x.Path}: {x.Entity.GetType().FullName} ({x.Entity.Name})")
                .Should()
                .BeEmpty(because);
        }

        /// <summary>
        /// Checks the known populated SwitchFilter case because it exercises filter lookup and symbol-definition migration.
        /// </summary>
        private static void AssertPopulatedSwitchFilterWasMigrated(IReadOnlyList<EntityNode> nodes) {
            SwitchFilter switchFilter = nodes
                .Single(x => x.Path == "$.Items[1].Items[22]")
                .Entity
                .Should()
                .BeOfType<SwitchFilter>()
                .Subject;

            switchFilter.ComboBoxText.Should().Be("Green");
            switchFilter.Filter.Name.Should().Be("Green");
            switchFilter.Filter.Position.Should().Be(2);
        }

        /// <summary>
        /// Captures the migrated entity structure so current-version serialization can be checked for type/path stability.
        /// </summary>
        private static IReadOnlyList<string> CreateEntityTypeDigest(IEnumerable<EntityNode> nodes) {
            return nodes
                .Select(node => $"{node.Path}:Type={node.Entity.GetType().FullName}")
                .ToList();
        }

        private sealed record LegacyCorpusFile(string ManifestPath, string SequencePath);

        private sealed record JsonEntityNode(string Path, JObject Json);

        private sealed record EntityNode(string Path, ISequenceEntity Entity);

        private sealed record ComparableJsonValue(string PropertyName, string DisplayPath, string NormalizedValue);

        private sealed record ExpressionExpectation(string ExpressionPropertyName, string SourcePath, double? NumericValue, string? StringValue);

        private sealed class LegacyManifest {
            public string SourceBranch { get; set; } = string.Empty;
            public string SourceCommit { get; set; } = string.Empty;
            public string GeneratedBy { get; set; } = string.Empty;
            public string SequenceFile { get; set; } = string.Empty;
            public IList<string> Items { get; set; } = new List<string>();
            public IList<string> Conditions { get; set; } = new List<string>();
            public IList<string> Triggers { get; set; } = new List<string>();
            public IList<string> Containers { get; set; } = new List<string>();
            public IList<ManifestEntity> Entities { get; set; } = new List<ManifestEntity>();
        }

        private sealed class ManifestEntity {
            public string Kind { get; set; } = string.Empty;
            public string Variant { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        private sealed class CurrentSequencerCatalog : IDisposable {
            private readonly CompositionContainer container;
            private readonly NINA.Profile.Profile profile;

            /// <summary>
            /// Keeps the MEF container and profile alive for the duration of a corpus import and exposes the factory under test.
            /// </summary>
            private CurrentSequencerCatalog(CompositionContainer container, NINA.Profile.Profile profile, ISequencerFactory factory) {
                this.container = container;
                this.profile = profile;
                Factory = factory;
            }

            public ISequencerFactory Factory { get; }

            /// <summary>
            /// Builds the current sequencer catalog with representative mocked services so legacy entities deserialize normally.
            /// </summary>
            public static CurrentSequencerCatalog Create() {
                NINA.Profile.Profile profile = new NINA.Profile.Profile();
                profile.FilterWheelSettings.FilterWheelFilters = new ObserveAllCollection<FilterInfo> {
                    new FilterInfo("Red", 0, 1),
                    new FilterInfo("Green", 0, 2),
                    new FilterInfo("Blue", 0, 3)
                };

                Mock<IProfileService> profileService = new Mock<IProfileService>();
                profileService.SetupGet(x => x.ActiveProfile).Returns(profile);
                profileService.SetupGet(x => x.Profiles).Returns(new AsyncObservableCollection<ProfileMeta> {
                    new ProfileMeta {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Name = "Legacy Profile",
                        Description = "Generated corpus profile",
                        LastUsed = new DateTime(2024, 1, 2, 3, 4, 5),
                        IsActive = true
                    }
                });

                Mock<ICameraMediator> cameraMediator = new Mock<ICameraMediator>();
                cameraMediator.Setup(x => x.GetInfo()).Returns(new CameraInfo {
                    Connected = false,
                    CanSetTemperature = true,
                    XSize = 8000,
                    YSize = 6000,
                    CanSetGain = true,
                    GainMin = 0,
                    GainMax = 300,
                    CanSetOffset = true,
                    OffsetMin = 0,
                    OffsetMax = 100,
                    BinningModes = new AsyncObservableCollection<BinningMode> {
                        new BinningMode(1, 1),
                        new BinningMode(2, 2)
                    },
                    ReadoutModes = new AsyncObservableCollection<string> {
                        "Default",
                        "High Gain"
                    }
                });

                Mock<IFilterWheelMediator> filterWheelMediator = new Mock<IFilterWheelMediator>();
                filterWheelMediator.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo {
                    Connected = false,
                    SelectedFilter = profile.FilterWheelSettings.FilterWheelFilters[1]
                });

                Mock<ITelescopeMediator> telescopeMediator = new Mock<ITelescopeMediator>();
                telescopeMediator.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = false });

                Mock<IFocuserMediator> focuserMediator = new Mock<IFocuserMediator>();
                focuserMediator.Setup(x => x.GetInfo()).Returns(new FocuserInfo { Connected = false });

                Mock<IGuiderMediator> guiderMediator = new Mock<IGuiderMediator>();
                guiderMediator.Setup(x => x.GetInfo()).Returns(new GuiderInfo { Connected = false });

                Mock<IRotatorMediator> rotatorMediator = new Mock<IRotatorMediator>();
                rotatorMediator.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = false });

                Mock<IFlatDeviceMediator> flatDeviceMediator = new Mock<IFlatDeviceMediator>();
                flatDeviceMediator.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo { Connected = false });

                Mock<IWeatherDataMediator> weatherDataMediator = new Mock<IWeatherDataMediator>();
                weatherDataMediator.Setup(x => x.GetInfo()).Returns(new WeatherDataInfo { Connected = false });

                Mock<IDomeMediator> domeMediator = new Mock<IDomeMediator>();
                domeMediator.Setup(x => x.GetInfo()).Returns(new DomeInfo { Connected = false });

                Mock<ISwitchMediator> switchMediator = new Mock<ISwitchMediator>();
                switchMediator.Setup(x => x.GetInfo()).Returns(new SwitchInfo { Connected = false });

                Mock<ISafetyMonitorMediator> safetyMonitorMediator = new Mock<ISafetyMonitorMediator>();
                safetyMonitorMediator.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo { Connected = false });

                Mock<INighttimeCalculator> nighttimeCalculator = new Mock<INighttimeCalculator>();
                IList<IDateTimeProvider> dateTimeProviders = new List<IDateTimeProvider> {
                    new NINA.Sequencer.Utility.DateTimeProvider.TimeProvider(nighttimeCalculator.Object),
                    new SunsetProvider(nighttimeCalculator.Object),
                    new CivilDuskProvider(nighttimeCalculator.Object),
                    new NauticalDuskProvider(nighttimeCalculator.Object),
                    new DuskProvider(nighttimeCalculator.Object),
                    new DawnProvider(nighttimeCalculator.Object),
                    new NauticalDawnProvider(nighttimeCalculator.Object),
                    new CivilDawnProvider(nighttimeCalculator.Object),
                    new SunriseProvider(nighttimeCalculator.Object),
                    new MeridianProvider(profileService.Object)
                };

                Mock<IApplicationResourceDictionary> resourceDictionary = new Mock<IApplicationResourceDictionary>();
                resourceDictionary.Setup(x => x[It.IsAny<string>()]).Returns(new GeometryGroup());

                CompositionContainer container = new CompositionContainer(
                    new AssemblyCatalog(typeof(ISequenceItem).Assembly),
                    CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);
                container.ComposeExportedValue<IProfileService>(profileService.Object);
                container.ComposeExportedValue<ICameraMediator>(cameraMediator.Object);
                container.ComposeExportedValue<ITelescopeMediator>(telescopeMediator.Object);
                container.ComposeExportedValue<IFocuserMediator>(focuserMediator.Object);
                container.ComposeExportedValue<IFilterWheelMediator>(filterWheelMediator.Object);
                container.ComposeExportedValue<IGuiderMediator>(guiderMediator.Object);
                container.ComposeExportedValue<IRotatorMediator>(rotatorMediator.Object);
                container.ComposeExportedValue<IFlatDeviceMediator>(flatDeviceMediator.Object);
                container.ComposeExportedValue<IWeatherDataMediator>(weatherDataMediator.Object);
                container.ComposeExportedValue<IImagingMediator>(Mock.Of<IImagingMediator>());
                container.ComposeExportedValue<IApplicationStatusMediator>(Mock.Of<IApplicationStatusMediator>());
                container.ComposeExportedValue<INighttimeCalculator>(nighttimeCalculator.Object);
                container.ComposeExportedValue<IPlanetariumFactory>(Mock.Of<IPlanetariumFactory>());
                container.ComposeExportedValue<IImageHistoryVM>(Mock.Of<IImageHistoryVM>());
                container.ComposeExportedValue<IDeepSkyObjectSearchVM>(Mock.Of<IDeepSkyObjectSearchVM>());
                container.ComposeExportedValue<IDomeMediator>(domeMediator.Object);
                container.ComposeExportedValue<IImageSaveMediator>(Mock.Of<IImageSaveMediator>());
                container.ComposeExportedValue<ISwitchMediator>(switchMediator.Object);
                container.ComposeExportedValue<IApplicationResourceDictionary>(resourceDictionary.Object);
                container.ComposeExportedValue<IList<IDateTimeProvider>>(dateTimeProviders);
                container.ComposeExportedValue<ISafetyMonitorMediator>(safetyMonitorMediator.Object);
                container.ComposeExportedValue<IApplicationMediator>(Mock.Of<IApplicationMediator>());
                container.ComposeExportedValue<IFramingAssistantVM>(Mock.Of<IFramingAssistantVM>());
                container.ComposeExportedValue<IPlateSolverFactory>(Mock.Of<IPlateSolverFactory>());
                container.ComposeExportedValue<IWindowServiceFactory>(Mock.Of<IWindowServiceFactory>());
                container.ComposeExportedValue<IDomeFollower>(Mock.Of<IDomeFollower>());
                container.ComposeExportedValue<IPluggableBehaviorSelector<IStarDetection>>(Mock.Of<IPluggableBehaviorSelector<IStarDetection>>());
                container.ComposeExportedValue<IPluggableBehaviorSelector<IStarAnnotator>>(Mock.Of<IPluggableBehaviorSelector<IStarAnnotator>>());
                container.ComposeExportedValue<IImageDataFactory>(Mock.Of<IImageDataFactory>());
                container.ComposeExportedValue<IAutoFocusVMFactory>(Mock.Of<IAutoFocusVMFactory>());
                container.ComposeExportedValue<IMeridianFlipVMFactory>(Mock.Of<IMeridianFlipVMFactory>());
                container.ComposeExportedValue<IImageControlVM>(Mock.Of<IImageControlVM>());
                container.ComposeExportedValue<IImageStatisticsVM>(Mock.Of<IImageStatisticsVM>());
                container.ComposeExportedValue<IDomeSynchronization>(Mock.Of<IDomeSynchronization>());
                container.ComposeExportedValue<ISequenceMediator>(Mock.Of<ISequenceMediator>());
                container.ComposeExportedValue<IOptionsVM>(Mock.Of<IOptionsVM>());
                container.ComposeExportedValue<IExposureDataFactory>(Mock.Of<IExposureDataFactory>());
                container.ComposeExportedValue<ITwilightCalculator>(Mock.Of<ITwilightCalculator>());
                container.ComposeExportedValue<IMessageBroker>(Mock.Of<IMessageBroker>());
                container.ComposeExportedValue<ISymbolBroker>(Mock.Of<ISymbolBroker>());
                container.ComposeExportedValue<ITemplateLinkResolver>(Mock.Of<ITemplateLinkResolver>());

                IList<ISequenceItem> items = container.GetExports<ISequenceItem, IDictionary<string, object>>().Select(x => x.Value).ToList();
                IList<ISequenceCondition> conditions = container.GetExports<ISequenceCondition, IDictionary<string, object>>().Select(x => x.Value).ToList();
                IList<ISequenceTrigger> triggers = container.GetExports<ISequenceTrigger, IDictionary<string, object>>().Select(x => x.Value).ToList();
                IList<ISequenceContainer> containers = container.GetExports<ISequenceContainer, IDictionary<string, object>>().Select(x => x.Value).ToList();
                IList<ISequenceEntityUpgrader> upgraders = container.GetExports<ISequenceEntityUpgrader, IDictionary<string, object>>().Select(x => x.Value).ToList();

                return new CurrentSequencerCatalog(
                    container,
                    profile,
                    new CorpusSequencerFactory(items, conditions, triggers, containers, dateTimeProviders, upgraders));
            }

            /// <summary>
            /// Releases the composed catalog resources after the migration test has finished importing the corpus.
            /// </summary>
            public void Dispose() {
                container.Dispose();
                profile.Dispose();
            }
        }

        private sealed class CorpusSequencerFactory : ISequencerFactory {
            /// <summary>
            /// Provides the factory surface SequenceJsonConverter needs while keeping the test catalog limited to MEF exports.
            /// </summary>
            public CorpusSequencerFactory(
                    IList<ISequenceItem> items,
                    IList<ISequenceCondition> conditions,
                    IList<ISequenceTrigger> triggers,
                    IList<ISequenceContainer> containers,
                    IList<IDateTimeProvider> dateTimeProviders,
                    IList<ISequenceEntityUpgrader> upgraders
            ) {
                Items = items;
                Conditions = conditions;
                Triggers = triggers;
                Container = containers;
                DateTimeProviders = dateTimeProviders;
                Upgraders = upgraders;
            }

            public IList<ISequenceCondition> Conditions { get; }
            public IList<ISequenceContainer> Container { get; }
            public IList<ISequenceItem> Items { get; }
            public ICollectionView ItemsView => null!;
            public ICollectionView InstructionsView => null!;
            public ICollectionView ConditionsView => null!;
            public ICollectionView TriggersView => null!;
            public IList<ISequenceTrigger> Triggers { get; }
            public IList<IDateTimeProvider> DateTimeProviders { get; }
            public IList<ISequenceEntityUpgrader> Upgraders { get; }
            public string ViewFilter { get; set; } = string.Empty;

            /// <summary>
            /// Returns a clone of the requested condition prototype so deserialization uses current entity construction rules.
            /// </summary>
            public T GetCondition<T>() where T : ISequenceCondition {
                return (T?)Conditions.FirstOrDefault(x => x.GetType() == typeof(T))?.Clone() ?? default!;
            }

            /// <summary>
            /// Returns a clone of the requested container prototype so deserialization uses current entity construction rules.
            /// </summary>
            public T GetContainer<T>() where T : ISequenceContainer {
                return (T?)Container.FirstOrDefault(x => x.GetType() == typeof(T))?.Clone() ?? default!;
            }

            /// <summary>
            /// Returns a clone of the requested item prototype so deserialization uses current entity construction rules.
            /// </summary>
            public T GetItem<T>() where T : ISequenceItem {
                return (T?)Items.FirstOrDefault(x => x.GetType() == typeof(T))?.Clone() ?? default!;
            }

            /// <summary>
            /// Returns a clone of the requested trigger prototype so deserialization uses current entity construction rules.
            /// </summary>
            public T GetTrigger<T>() where T : ISequenceTrigger {
                return (T?)Triggers.FirstOrDefault(x => x.GetType() == typeof(T))?.Clone() ?? default!;
            }
        }
    }
}
