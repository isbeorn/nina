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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using SequencerExpression = NINA.Sequencer.Logic.Expression;

namespace NINA.Test.Sequencer.Logic {

    [TestFixture]
    public class SymbolFunctionTest {
        private SymbolBroker broker;

        [SetUp]
        public void SetUp() {
            NINA.Profile.Profile profile = new NINA.Profile.Profile {
                Id = Guid.NewGuid(),
                Name = "Function Profile"
            };
            profile.AstrometrySettings.Latitude = 47.1;
            profile.AstrometrySettings.Longitude = 11.3;
            profile.AstrometrySettings.Elevation = 650;

            Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);

            broker = new SymbolBroker(
                profileServiceMock.Object,
                new Mock<ISwitchMediator>().Object,
                new Mock<IWeatherDataMediator>().Object,
                new Mock<ICameraMediator>().Object,
                new Mock<IDomeMediator>().Object,
                new Mock<IFlatDeviceMediator>().Object,
                new Mock<IFilterWheelMediator>().Object,
                new Mock<IRotatorMediator>().Object,
                new Mock<ISafetyMonitorMediator>().Object,
                new Mock<IFocuserMediator>().Object,
                new Mock<ITelescopeMediator>().Object,
                new Mock<IGuiderMediator>().Object,
                new Mock<IImagingMediator>().Object);
        }

        [TearDown]
        public void TearDown() {
            broker.Dispose();
        }

        /// <summary>
        /// Verifies the Math Functions Evaluate Expected Numeric Results scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void MathFunctions_EvaluateExpectedNumericResults() {
            IReadOnlyList<(string Definition, double Expected, double Tolerance)> cases = new List<(string, double, double)> {
                ("Abs(-3)", 3, 1e-10),
                ("Acos(1)", 0, 1e-10),
                ("Asin(0)", 0, 1e-10),
                ("Atan(0)", 0, 1e-10),
                ("Avg(1, 2, 3)", 2, 1e-10),
                ("Ceiling(1.2)", 2, 1e-10),
                ("Cos(0)", 1, 1e-10),
                ("Exp(0)", 1, 1e-10),
                ("Floor(1.8)", 1, 1e-10),
                ("IEEERemainder(5, 2)", 1, 1e-10),
                ("Ln(1)", 0, 1e-10),
                ("Log(8, 2)", 3, 1e-10),
                ("Log10(100)", 2, 1e-10),
                ("Max(1, 2)", 2, 1e-10),
                ("Min(1, 2)", 1, 1e-10),
                ("Pow(2, 3)", 8, 1e-10),
                ("Round(3.222, 2)", 3.22, 1e-10),
                ("Round(3.6)", 4, 1e-10),
                ("Sign(-10)", -1, 1e-10),
                ("Sin(0)", 0, 1e-10),
                ("Sqrt(9)", 3, 1e-10),
                ("Tan(0)", 0, 1e-10),
                ("Truncate(1.7)", 1, 1e-10),
                ("Mod(-1, 10)", 9, 1e-10),
                ("Clamp(15, 1, 10)", 10, 1e-10),
                ("Between(5, 1, 10)", 1, 1e-10),
                ("Deg(1.5707963267948966)", 90, 1e-8),
                ("Rad(180)", Math.PI, 1e-10),
                ("Sum(1, 2, 3)", 6, 1e-10)
            };

            foreach ((string definition, double expected, double tolerance) in cases) {
                SequencerExpression expression = Evaluate(definition);

                expression.Value.Should().BeApproximately(expected, tolerance, definition);
            }
        }

        /// <summary>
        /// Verifies the String Functions Evaluate Expected String And Boolean Results scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void StringFunctions_EvaluateExpectedStringAndBooleanResults() {
            Evaluate("StartsWith(\"hello\", \"he\")").Value.Should().Be(1);
            Evaluate("Contains(\"hello\", \"ell\")").Value.Should().Be(1);
            Evaluate("EndsWith(\"hello\", \"lo\")").Value.Should().Be(1);
            Evaluate("StrLength(\"hello\")").Value.Should().Be(5);

            Evaluate("StrConcat(\"hello\", \" world\")").StringValue.Should().Be("hello world");
            Evaluate("StrAtPos(\"hello\", 1)").StringValue.Should().Be("e");
            Evaluate("Substring(\"hello\", 1, 3)").StringValue.Should().Be("ell");
            Evaluate("Substring(\"hello\", 2)").StringValue.Should().Be("llo");
            Evaluate("Substring(\"hello\", -1)").StringValue.Should().BeEmpty();
            Evaluate("ToLower(\"HaRGB\")").StringValue.Should().Be("hargb");
            Evaluate("ToUpper(\"HaRGB\")").StringValue.Should().Be("HARGB");
        }

        /// <summary>
        /// Verifies the Logic Functions Evaluate Expected Results scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void LogicFunctions_EvaluateExpectedResults() {
            Evaluate("In(2, 1, 2, 3)").Value.Should().Be(1);
            Evaluate("In(4, 1, 2, 3)").Value.Should().Be(0);
            Evaluate("If(1 < 2, \"yes\", \"no\")").StringValue.Should().Be("yes");
            Evaluate("Ifs(1 > 2, \"bad\", 2 > 1, \"good\", \"fallback\")").StringValue.Should().Be("good");
            Evaluate("Ifs(1 > 2, \"bad\", \"fallback\")").StringValue.Should().Be("fallback");
            Evaluate("Defined(\"NINA_ProfileName\")").Value.Should().Be(1);
            Evaluate("Defined(\"DefinitelyMissing\")").Value.Should().Be(0);
            Evaluate("Not(1 < 2)").Value.Should().Be(0);
        }

        [Test]
        public void LogicIn_MatchesShortSymbolAgainstIntegerLiteral() {
            ISymbolProvider provider = broker.RegisterSymbolProvider("Test");
            provider.AddOrUpdateSymbol("ShortValue", (short)2);

            Evaluate("In(Test_ShortValue, 1, 2, 3)").Value.Should().Be(1);
        }

        [Test]
        public void StringAtPos_UsesShortSymbolAsIndex() {
            ISymbolProvider provider = broker.RegisterSymbolProvider("Test");
            provider.AddOrUpdateSymbol("ShortIndex", (short)1);

            Evaluate("StrAtPos(\"hello\", Test_ShortIndex)").StringValue.Should().Be("e");
        }

        /// <summary>
        /// Verifies the Time And Utility Functions Evaluate Expected Results scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void TimeAndUtilityFunctions_EvaluateExpectedResults() {
            DateTime localTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local);
            string timestamp = CoreUtil.ToUnixSeconds(localTime).ToString(CultureInfo.InvariantCulture);

            Evaluate($"Hour({timestamp})").Value.Should().Be(localTime.Hour);
            Evaluate($"Minute({timestamp})").Value.Should().Be(localTime.Minute);
            Evaluate($"Day({timestamp})").Value.Should().Be(localTime.Day);
            Evaluate($"Month({timestamp})").Value.Should().Be(localTime.Month);
            Evaluate($"Year({timestamp})").Value.Should().Be(localTime.Year);
            Evaluate($"Dow({timestamp})").Value.Should().Be((int)localTime.DayOfWeek);
            Evaluate($"AddMinutes({timestamp}, 30)").Value.Should().Be(CoreUtil.ToUnixSeconds(localTime.AddMinutes(30)));
            Evaluate($"AddHours({timestamp}, 2)").Value.Should().Be(CoreUtil.ToUnixSeconds(localTime.AddHours(2)));
            Evaluate($"DateString({timestamp}, \"yyyy-MM-dd HH:mm\")").StringValue.Should().Be(localTime.ToString("yyyy-MM-dd HH:mm"));

            SequencerExpression now = Evaluate("Now()");
            now.Value.Should().BeGreaterThan(0);
            now.GlobalVolatile.Should().BeTrue();

            DateTime thirtySecondsAgo = DateTime.Now.AddSeconds(-30);
            string thirtySecondsAgoTimestamp = CoreUtil.ToUnixSeconds(thirtySecondsAgo).ToString(CultureInfo.InvariantCulture);
            SequencerExpression secondsSince = Evaluate($"SecondsSince({thirtySecondsAgoTimestamp})");
            secondsSince.Value.Should().BeGreaterThan(0);
            secondsSince.GlobalVolatile.Should().BeTrue();

            SequencerExpression random = Evaluate("Random()");
            random.Value.Should().BeGreaterThanOrEqualTo(0);
            random.Value.Should().BeLessThan(1);
            random.GlobalVolatile.Should().BeTrue();
        }

        private SequencerExpression Evaluate(string definition) {
            Mock<ISequenceEntity> context = new Mock<ISequenceEntity>();
            context.SetupGet(x => x.SymbolBroker).Returns(broker);
            context.SetupGet(x => x.Parent).Returns((ISequenceContainer)null);
            context.SetupGet(x => x.Name).Returns("Function Test Context");

            SequencerExpression expression = new SequencerExpression(definition, context.Object) {
                SymbolBroker = broker,
                IsExpression = true
            };

            expression.Evaluate(ignoreRoot: true);

            expression.Error.Should().BeNull(definition);
            return expression;
        }
    }
}
