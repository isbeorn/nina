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
using NINA.Core.Locale;
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
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.Dome;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Rotator;
using NINA.Sequencer.SequenceItem.Switch;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Trigger.Platesolving;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace NINA.Test.Sequencer {

    [TestFixture]
    public class ExpressionBackedEntityContractTest {
        private const string IsExpressionAttributeName = "NINA.Sequencer.Generators.IsExpressionAttribute";
        private const string UsesExpressionsAttributeName = "NINA.Sequencer.Generators.UsesExpressionsAttribute";

        private static readonly IReadOnlyDictionary<string, Func<object>> EntityFactories = new Dictionary<string, Func<object>> {
            [nameof(AboveHorizonCondition)] = () => new AboveHorizonCondition(CreateProfileService()),
            [nameof(AltitudeCondition)] = () => new AltitudeCondition(CreateProfileService()),
            [nameof(AutoBrightnessFlat)] = () => new AutoBrightnessFlat(CreateProfileService(), CreateCameraMediator(), CreateImagingMediator(), CreateImageSaveMediator(), CreateImageHistory(), CreateFilterWheelMediator(), CreateFlatDeviceMediator()),
            [nameof(AutoExposureFlat)] = () => new AutoExposureFlat(CreateProfileService(), CreateCameraMediator(), CreateImagingMediator(), CreateImageSaveMediator(), CreateImageHistory(), CreateFilterWheelMediator(), CreateFlatDeviceMediator()),
            [nameof(AutofocusAfterExposures)] = () => new AutofocusAfterExposures(CreateProfileService(), CreateImageHistory(), CreateCameraMediator(), CreateFilterWheelMediator(), CreateFocuserMediator(), CreateAutoFocusVMFactory(), CreateSafetyMonitorMediator()),
            [nameof(AutofocusAfterHFRIncreaseTrigger)] = () => new AutofocusAfterHFRIncreaseTrigger(CreateProfileService(), CreateImageHistory(), CreateCameraMediator(), CreateFilterWheelMediator(), CreateFocuserMediator(), CreateAutoFocusVMFactory(), CreateSafetyMonitorMediator()),
            [nameof(AutofocusAfterTemperatureChangeTrigger)] = () => new AutofocusAfterTemperatureChangeTrigger(CreateProfileService(), CreateImageHistory(), CreateCameraMediator(), CreateFilterWheelMediator(), CreateFocuserMediator(), CreateAutoFocusVMFactory(), CreateSafetyMonitorMediator()),
            [nameof(AutofocusAfterTimeTrigger)] = () => new AutofocusAfterTimeTrigger(CreateProfileService(), CreateImageHistory(), CreateCameraMediator(), CreateFilterWheelMediator(), CreateFocuserMediator(), CreateAutoFocusVMFactory(), CreateSafetyMonitorMediator()),
            [nameof(CenterAfterDriftTrigger)] = () => new CenterAfterDriftTrigger(CreateProfileService(), CreateTelescopeMediator(), CreateFilterWheelMediator(), CreateGuiderMediator(), CreateImagingMediator(), CreateCameraMediator(), CreateDomeMediator(), CreateDomeFollower(), CreateImageSaveMediator(), CreateApplicationStatusMediator(), CreateSafetyMonitorMediator()),
            [nameof(ConditionalContainer)] = () => new ConditionalContainer(),
            [nameof(CoolCamera)] = () => new CoolCamera(CreateCameraMediator()),
            [nameof(CoordinatesInstruction)] = () => new CoordinatesInstruction(),
            [nameof(DitherAfterExposures)] = () => new DitherAfterExposures(CreateGuiderMediator(), CreateImageHistory(), CreateProfileService(), CreateSafetyMonitorMediator()),
            [nameof(LoopCondition)] = () => new LoopCondition(),
            [nameof(LoopWhile)] = () => new LoopWhile(),
            [nameof(MoonAltitudeCondition)] = () => new MoonAltitudeCondition(CreateProfileService()),
            [nameof(MoonIlluminationCondition)] = () => new MoonIlluminationCondition(),
            [nameof(MoveFocuserAbsolute)] = () => new MoveFocuserAbsolute(CreateFocuserMediator()),
            [nameof(MoveFocuserByTemperature)] = () => new MoveFocuserByTemperature(CreateFocuserMediator()),
            [nameof(MoveFocuserRelative)] = () => new MoveFocuserRelative(CreateFocuserMediator()),
            [nameof(MoveRotatorMechanical)] = () => new MoveRotatorMechanical(CreateRotatorMediator()),
            [nameof(SetBrightness)] = () => new SetBrightness(CreateFlatDeviceMediator()),
            [nameof(SetSwitchValue)] = () => new SetSwitchValue(CreateSwitchMediator()),
            [nameof(SlewDomeAzimuth)] = () => new SlewDomeAzimuth(CreateDomeMediator()),
            [nameof(SlewScopeToAltAz)] = () => new SlewScopeToAltAz(CreateProfileService(), CreateTelescopeMediator(), CreateGuiderMediator()),
            [nameof(SmartExposure)] = () => new SmartExposure(CreateProfileService(), CreateCameraMediator(), CreateImagingMediator(), CreateImageSaveMediator(), CreateImageHistory(), CreateFilterWheelMediator(), CreateGuiderMediator(), CreateSafetyMonitorMediator()),
            [nameof(SolveAndRotate)] = () => new SolveAndRotate(CreateProfileService(), CreateTelescopeMediator(), CreateImagingMediator(), CreateRotatorMediator(), CreateFilterWheelMediator(), CreateGuiderMediator(), CreatePlateSolverFactory(), CreateWindowServiceFactory()),
            [nameof(SunAltitudeCondition)] = () => new SunAltitudeCondition(CreateProfileService()),
            [nameof(SwitchFilter)] = () => new SwitchFilter(CreateProfileService(), CreateFilterWheelMediator()),
            [nameof(TakeExposure)] = () => new TakeExposure(CreateProfileService(), CreateCameraMediator(), CreateImagingMediator(), CreateImageSaveMediator(), CreateImageHistory()),
            [nameof(TakeManyExposures)] = () => new TakeManyExposures(CreateProfileService(), CreateCameraMediator(), CreateImagingMediator(), CreateImageSaveMediator(), CreateImageHistory()),
            [nameof(TakeSubframeExposure)] = () => new TakeSubframeExposure(CreateProfileService(), CreateCameraMediator(), CreateImagingMediator(), CreateImageSaveMediator(), CreateImageHistory()),
            [nameof(WaitForMoonAltitude)] = () => new WaitForMoonAltitude(CreateProfileService()),
            [nameof(WaitForSunAltitude)] = () => new WaitForSunAltitude(CreateProfileService()),
            [nameof(WaitForTimeSpan)] = () => new WaitForTimeSpan(),
            [nameof(WaitUntil)] = () => new WaitUntil(CreateSafetyMonitorMediator(), CreateSequenceMediator(), CreateProfileService()),
            [nameof(WarmCamera)] = () => new WarmCamera(CreateCameraMediator()),
        };

        /// <summary>
        /// Verifies the Every Uses Expressions Entity Has Factory Coverage scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void EveryUsesExpressionsEntity_HasFactoryCoverage() {
            IReadOnlyList<Type> expressionEntityTypes = GetExpressionEntityTypes();

            EntityFactories.Keys.Should().BeEquivalentTo(expressionEntityTypes.Select(t => t.Name));
        }

        /// <summary>
        /// Verifies the Generated Expression Properties Are Initialized From Attribute Metadata scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(ExpressionPropertyCases))]
        public void GeneratedExpressionProperties_AreInitializedFromAttributeMetadata(Type entityType, string propertyName) {
            object entity = CreateEntity(entityType);
            PropertyInfo scalarProperty = GetScalarProperty(entityType, propertyName);
            CustomAttributeData attribute = GetIsExpressionAttribute(scalarProperty);

            Expression expression = GetExpression(entity, propertyName);

            expression.Context.Should().BeSameAs(entity);
            expression.Type.Should().Be(GetExpectedExpressionType(scalarProperty.PropertyType));

            if (TryGetNamedArgument<double>(attribute, nameof(Expression.Default), out double defaultValue)) {
                expression.Default.Should().Be(defaultValue);
            } else {
                expression.Default.Should().Be(double.NaN);
            }

            if (TryGetNamedArgument<double>(attribute, nameof(Expression.AutoValue), out double autoValue)) {
                expression.AutoValue.Should().Be(autoValue);
            } else {
                expression.AutoValue.Should().Be(double.NaN);
            }

            double[] expectedRange = GetExpectedRange(attribute);
            if (expectedRange != null) {
                expression.Range.Should().Equal(expectedRange);
            } else {
                expression.Range.Should().BeNull();
            }

            if (TryGetNamedArgument<bool>(attribute, "HasValidator", out bool hasValidator) && hasValidator) {
                expression.Validator.Should().NotBeNull();
            } else {
                expression.Validator.Should().BeNull();
            }
        }

        /// <summary>
        /// Verifies the Expression Definitions Evaluate Through Generated Properties And Clone Independently scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(ExpressionEntityCases))]
        public void ExpressionDefinitions_EvaluateThroughGeneratedProperties_AndCloneIndependently(Type entityType) {
            object entity = CreateEntity(entityType);
            IReadOnlyList<PropertyInfo> expressionBackedProperties = GetExpressionBackedProperties(entityType);
            Dictionary<string, string> definitions = new Dictionary<string, string>();
            Dictionary<string, double> expectedValues = new Dictionary<string, double>();

            foreach (PropertyInfo scalarProperty in expressionBackedProperties) {
                double expectedValue = ChooseValidValue(scalarProperty);
                string definition = BuildExpressionDefinition(expectedValue, scalarProperty.PropertyType);
                SetExpressionDefinition(entity, scalarProperty.Name, definition);

                GetNumericScalarValue(entity, scalarProperty).Should().BeApproximately(expectedValue, 1e-5);
                GetExpression(entity, scalarProperty.Name).Definition.Should().Be(definition);

                definitions[scalarProperty.Name] = definition;
                expectedValues[scalarProperty.Name] = expectedValue;
            }

            object clone = entityType.GetMethod(nameof(ISequenceEntity.Clone)).Invoke(entity, Array.Empty<object>());

            clone.Should().NotBeSameAs(entity);
            clone.GetType().Should().Be(entityType);

            foreach (PropertyInfo scalarProperty in expressionBackedProperties) {
                Expression originalExpression = GetExpression(entity, scalarProperty.Name);
                Expression cloneExpression = GetExpression(clone, scalarProperty.Name);

                cloneExpression.Should().NotBeSameAs(originalExpression);
                cloneExpression.Definition.Should().Be(definitions[scalarProperty.Name]);
                cloneExpression.Context.Should().BeSameAs(clone);
                GetNumericScalarValue(clone, scalarProperty).Should().BeApproximately(expectedValues[scalarProperty.Name], 1e-5);

                SetExpressionDefinition(clone, scalarProperty.Name, BuildExpressionDefinition(expectedValues[scalarProperty.Name] + 1, scalarProperty.PropertyType));
                originalExpression.Definition.Should().Be(definitions[scalarProperty.Name]);
            }
        }

        /// <summary>
        /// Verifies the Invalid Expression Definitions Are Reported By Entity Validation scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(ExpressionEntityCases))]
        public void InvalidExpressionDefinitions_AreReportedByEntityValidation(Type entityType) {
            object entity = CreateEntity(entityType);
            PropertyInfo scalarProperty = GetExpressionBackedProperties(entityType).First();

            SetExpressionDefinition(entity, scalarProperty.Name, "1 +");

            Expression expression = GetExpression(entity, scalarProperty.Name);
            expression.Error.Should().Be(Loc.Instance["LblSyntaxError"]);

            if (entity is IValidatable validatable) {
                Action validate = () => validatable.Validate();

                validate.Should().NotThrow($"{entityType.Name}.{scalarProperty.Name} has a syntax error but validation should remain a normal result path");
                if (!validatable.Validate()) {
                    validatable.Issues.Should().NotBeEmpty();
                }
            }
        }

        /// <summary>
        /// Verifies expression-backed entities can validate, stringify, estimate duration, and reset progress after all generated expressions evaluate successfully.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(ExpressionEntityCases))]
        public void EvaluatedExpressionEntities_RunCommonLifecycleMembersWithoutThrowing(Type entityType) {
            object entity = CreateEntity(entityType);
            IReadOnlyList<PropertyInfo> expressionBackedProperties = GetExpressionBackedProperties(entityType);

            foreach (PropertyInfo scalarProperty in expressionBackedProperties) {
                double expectedValue = ChooseValidValue(scalarProperty);
                SetExpressionDefinition(entity, scalarProperty.Name, BuildExpressionDefinition(expectedValue, scalarProperty.PropertyType));
            }

            entity.Invoking(x => x.ToString()).Should().NotThrow();

            if (entity is IValidatable validatable) {
                validatable.Invoking(x => x.Validate()).Should().NotThrow();
                validatable.Issues.Should().NotBeNull();
            }

            if (entity is ISequenceItem sequenceItem) {
                sequenceItem.Invoking(x => x.GetEstimatedDuration()).Should().NotThrow();
                sequenceItem.Invoking(x => x.ResetProgress()).Should().NotThrow();
            }

            if (entity is ISequenceTrigger trigger) {
                trigger.Invoking(x => x.SequenceBlockInitialize()).Should().NotThrow();
                trigger.Invoking(x => x.SequenceBlockStarted()).Should().NotThrow();
                trigger.Invoking(x => x.SequenceBlockFinished()).Should().NotThrow();
                trigger.Invoking(x => x.SequenceBlockTeardown()).Should().NotThrow();
            }
        }

        private static IEnumerable<TestCaseData> ExpressionEntityCases() {
            foreach (Type entityType in GetExpressionEntityTypes()) {
                yield return new TestCaseData(entityType).SetName($"{entityType.Name}_ExpressionDefinitionsClone");
            }
        }

        private static IEnumerable<TestCaseData> ExpressionPropertyCases() {
            foreach (Type entityType in GetExpressionEntityTypes()) {
                foreach (PropertyInfo scalarProperty in GetExpressionBackedProperties(entityType)) {
                    yield return new TestCaseData(entityType, scalarProperty.Name).SetName($"{entityType.Name}_{scalarProperty.Name}_ExpressionMetadata");
                }
            }
        }

        private static IReadOnlyList<Type> GetExpressionEntityTypes() {
            return typeof(LoopCondition)
                .Assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetCustomAttributes(inherit: false).Any(a => a.GetType().FullName == UsesExpressionsAttributeName))
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();
        }

        private static IReadOnlyList<PropertyInfo> GetExpressionBackedProperties(Type entityType) {
            return entityType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes().Any(a => a.GetType().FullName == IsExpressionAttributeName))
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();
        }

        private static object CreateEntity(Type entityType) {
            EntityFactories.TryGetValue(entityType.Name, out Func<object> factory).Should().BeTrue($"a factory is required for {entityType.FullName}");
            return factory();
        }

        private static PropertyInfo GetScalarProperty(Type entityType, string propertyName) {
            return entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        }

        private static Expression GetExpression(object entity, string propertyName) {
            PropertyInfo expressionProperty = entity.GetType().GetProperty($"{propertyName}Expression", BindingFlags.Public | BindingFlags.Instance);
            expressionProperty.Should().NotBeNull($"{entity.GetType().Name}.{propertyName} should have a generated Expression property");
            return expressionProperty.GetValue(entity) as Expression;
        }

        private static PropertyInfo GetDefinitionProperty(Type entityType, string propertyName) {
            PropertyInfo definitionProperty = entityType.GetProperty($"{propertyName}Definition", BindingFlags.Public | BindingFlags.Instance);
            return definitionProperty;
        }

        private static void SetExpressionDefinition(object entity, string propertyName, string definition) {
            PropertyInfo definitionProperty = GetDefinitionProperty(entity.GetType(), propertyName);
            if (definitionProperty != null) {
                definitionProperty.SetValue(entity, definition);
                return;
            }

            Expression expression = GetExpression(entity, propertyName);
            expression.Definition = definition;
            expression.Evaluate(true);
        }

        private static CustomAttributeData GetIsExpressionAttribute(PropertyInfo scalarProperty) {
            return scalarProperty.CustomAttributes.Single(a => a.AttributeType.FullName == IsExpressionAttributeName);
        }

        private static string GetExpectedExpressionType(Type scalarType) {
            if (scalarType == typeof(int)) {
                return "int";
            }

            if (scalarType == typeof(double)) {
                return "Double";
            }

            return scalarType.Name;
        }

        private static double[] GetExpectedRange(CustomAttributeData attribute) {
            if (!TryGetNamedArgument(attribute, "Range", out CustomAttributeNamedArgument rangeArgument)) {
                return null;
            }

            IReadOnlyCollection<CustomAttributeTypedArgument> typedValues = (IReadOnlyCollection<CustomAttributeTypedArgument>)rangeArgument.TypedValue.Value;
            List<double> values = typedValues.Select(v => Convert.ToDouble(v.Value, CultureInfo.InvariantCulture)).ToList();
            while (values.Count < 3) {
                values.Add(0);
            }

            return values.ToArray();
        }

        private static bool TryGetNamedArgument<T>(CustomAttributeData attribute, string name, out T value) {
            foreach (CustomAttributeNamedArgument argument in attribute.NamedArguments) {
                if (argument.MemberName == name) {
                    value = (T)Convert.ChangeType(argument.TypedValue.Value, typeof(T), CultureInfo.InvariantCulture);
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static bool TryGetNamedArgument(CustomAttributeData attribute, string name, out CustomAttributeNamedArgument value) {
            foreach (CustomAttributeNamedArgument argument in attribute.NamedArguments) {
                if (argument.MemberName == name) {
                    value = argument;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static double ChooseValidValue(PropertyInfo scalarProperty) {
            double[] expectedRange = GetExpectedRange(GetIsExpressionAttribute(scalarProperty));
            if (expectedRange == null) {
                return scalarProperty.PropertyType == typeof(int) ? 7 : 7.5;
            }

            double min = expectedRange[0];
            double max = expectedRange[1];
            int flags = Convert.ToInt32(expectedRange[2], CultureInfo.InvariantCulture);
            bool minExclusive = (flags & ExpressionRange.MIN_EXCLUSIVE) == ExpressionRange.MIN_EXCLUSIVE;
            bool maxExclusive = (flags & ExpressionRange.MAX_EXCLUSIVE) == ExpressionRange.MAX_EXCLUSIVE;

            double value;
            if (max == 0) {
                value = min + (minExclusive ? 2 : 1);
            } else {
                value = (min + max) / 2d;
                if (minExclusive && value <= min) {
                    value = min + 1;
                }

                if (maxExclusive && value >= max) {
                    value = max - 1;
                }
            }

            if (scalarProperty.PropertyType == typeof(int)) {
                value = Math.Max(Math.Ceiling(value), minExclusive ? min + 1 : min);
            }

            return value;
        }

        private static string BuildExpressionDefinition(double value, Type scalarType) {
            if (scalarType == typeof(int)) {
                int intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return $"{intValue - 1} + 1";
            }

            return string.Create(CultureInfo.InvariantCulture, $"{value - 0.5:0.#######} + 0.5");
        }

        private static double GetNumericScalarValue(object entity, PropertyInfo scalarProperty) {
            object value = scalarProperty.GetValue(entity);
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private static IProfileService CreateProfileService() {
            NINA.Profile.Profile profile = new NINA.Profile.Profile();
            profile.AstrometrySettings.Latitude = 47.1;
            profile.AstrometrySettings.Longitude = 11.3;
            profile.AstrometrySettings.Elevation = 650;
            profile.CameraSettings.PixelSize = 3.76;
            profile.TelescopeSettings.FocalLength = 550;
            profile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo { Name = "L", Position = 1 });
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo { Name = "Ha", Position = 2 });
            profile.GuiderSettings.SettleTimeout = 10;

            Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            return profileServiceMock.Object;
        }

        private static ICameraMediator CreateCameraMediator() {
            Mock<ICameraMediator> cameraMediatorMock = new Mock<ICameraMediator>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo {
                Connected = true,
                CanSetGain = true,
                CanSetOffset = true,
                DefaultGain = 10,
                DefaultOffset = 20,
                GainMin = 0,
                GainMax = 100,
                OffsetMin = 0,
                OffsetMax = 100,
                XSize = 300,
                YSize = 200
            });
            return cameraMediatorMock.Object;
        }

        private static ITelescopeMediator CreateTelescopeMediator() {
            Mock<ITelescopeMediator> telescopeMediatorMock = new Mock<ITelescopeMediator>();
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, AtPark = false });
            return telescopeMediatorMock.Object;
        }

        private static IGuiderMediator CreateGuiderMediator() {
            Mock<IGuiderMediator> guiderMediatorMock = new Mock<IGuiderMediator>();
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo { Connected = true });
            return guiderMediatorMock.Object;
        }

        private static IFilterWheelMediator CreateFilterWheelMediator() {
            Mock<IFilterWheelMediator> filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo {
                Connected = true,
                SelectedFilter = new FilterInfo { Name = "L", Position = 1 }
            });
            return filterWheelMediatorMock.Object;
        }

        private static IFocuserMediator CreateFocuserMediator() {
            Mock<IFocuserMediator> focuserMediatorMock = new Mock<IFocuserMediator>();
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo { Connected = true, Position = 1000, Temperature = 10 });
            return focuserMediatorMock.Object;
        }

        private static IRotatorMediator CreateRotatorMediator() {
            Mock<IRotatorMediator> rotatorMediatorMock = new Mock<IRotatorMediator>();
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo { Connected = true });
            return rotatorMediatorMock.Object;
        }

        private static IDomeMediator CreateDomeMediator() {
            Mock<IDomeMediator> domeMediatorMock = new Mock<IDomeMediator>();
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo { Connected = true });
            return domeMediatorMock.Object;
        }

        private static IFlatDeviceMediator CreateFlatDeviceMediator() {
            Mock<IFlatDeviceMediator> flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            flatDeviceMediatorMock.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo { Connected = true });
            return flatDeviceMediatorMock.Object;
        }

        private static ISwitchMediator CreateSwitchMediator() {
            Mock<ISwitchMediator> switchMediatorMock = new Mock<ISwitchMediator>();
            switchMediatorMock.Setup(x => x.GetInfo()).Returns(new SwitchInfo { Connected = true });
            return switchMediatorMock.Object;
        }

        private static ISafetyMonitorMediator CreateSafetyMonitorMediator() {
            Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            safetyMonitorMediatorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo { Connected = true, IsSafe = true });
            return safetyMonitorMediatorMock.Object;
        }

        private static IImagingMediator CreateImagingMediator() {
            return new Mock<IImagingMediator>().Object;
        }

        private static IImageSaveMediator CreateImageSaveMediator() {
            return new Mock<IImageSaveMediator>().Object;
        }

        private static IImageHistoryVM CreateImageHistory() {
            Mock<IImageHistoryVM> imageHistoryMock = new Mock<IImageHistoryVM>();
            imageHistoryMock.SetupGet(x => x.ImageHistory).Returns(new List<NINA.WPF.Base.Model.ImageHistoryPoint>());
            return imageHistoryMock.Object;
        }

        private static IAutoFocusVMFactory CreateAutoFocusVMFactory() {
            return new Mock<IAutoFocusVMFactory>().Object;
        }

        private static IDomeFollower CreateDomeFollower() {
            return new Mock<IDomeFollower>().Object;
        }

        private static IApplicationStatusMediator CreateApplicationStatusMediator() {
            return new Mock<IApplicationStatusMediator>().Object;
        }

        private static IPlateSolverFactory CreatePlateSolverFactory() {
            return new Mock<IPlateSolverFactory>().Object;
        }

        private static IWindowServiceFactory CreateWindowServiceFactory() {
            return new Mock<IWindowServiceFactory>().Object;
        }

        private static ISequenceMediator CreateSequenceMediator() {
            return new Mock<ISequenceMediator>().Object;
        }
    }
}
