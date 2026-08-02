using FluentAssertions;
using MdXaml.Plugins;
using Moq;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Expressions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Text;

namespace NINA.Test.Sequencer.Logic {
    [TestFixture]
    public class ExpressionTest {
        private Mock<ISequenceEntity> _context;
        private Mock<ISymbolBroker> _symbolBroker;

        [SetUp]
        public void SetUp() {
            _context = new Mock<ISequenceEntity>();
            _symbolBroker = new Mock<ISymbolBroker>();

            _context.SetupGet(c => c.SymbolBroker).Returns(_symbolBroker.Object);
            _context.SetupGet(c => c.Parent).Returns((ISequenceContainer)null);
            _context.SetupGet(c => c.Name).Returns("TestContext");
        }

        private Expression CreateExpression(string definition) {
            var expr = new Expression(definition, _context.Object) {
                SymbolBroker = _symbolBroker.Object
            };
            expr.IsExpression = true;
            return expr;
        }


        [Test]
        public void Expression_Evaluate_LiteralNumericDefinition_ShouldNotBeExpression_AndKeepValue() {
            // arrange
            var expr = CreateExpression("123.45");

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Value.Should().Be(123.45);
            expr.Error.Should().BeNull();
        }

        [Test]
        public void Expression_Evaluate_EmptyDefinition_ShouldResetToNaN_AndNoError() {
            // arrange
            var expr = CreateExpression("42");
            expr.Definition = ""; // resets state

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Value.Should().Be(double.NaN);
            expr.Error.Should().BeNull();
        }

        [Test]
        public void Expression_Evaluate_SimpleArithmeticExpression_ShouldEvaluateCorrectly() {
            // arrange
            var expr = CreateExpression("1 + 2 * 3");
            expr.IsExpression.Should().BeTrue();

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(7.0);
        }

        [Test]
        public void Expression_Evaluate_BooleanExpression_ShouldMapTrueToOne() {
            // arrange
            var expr = CreateExpression("1 < 2");

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(1.0);
        }

        [Test]
        public void Expression_Evaluate_BooleanExpression_ShouldMapFalseToZero() {
            // arrange
            var expr = CreateExpression("1 > 2");

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(0.0);
        }

        [Test]
        public void Expression_Evaluate_StringExpression_ShouldSetStringValue_AndNegativeInfinityValue() {
            // arrange
            var expr = CreateExpression("\"Hello NINA\"");

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.StringValue.Should().Be("Hello NINA");
            expr.Value.Should().Be(double.NegativeInfinity);
        }

        [Test]
        public void Expression_Evaluate_ExpressionWithSymbolBrokerParameters_ShouldUseBrokerValues() {
            // arrange
            var expr = CreateExpression("a + b * 2"); // expect 3 + 4 * 2 = 11

            // force parse references
            expr.Definition = "a + b * 2";
            expr.IsExpression.Should().BeTrue();

            object aVal = 3.0;
            _symbolBroker
                .Setup(b => b.TryGetValue("a", out aVal))
                .Returns(true);

            object bVal = 4.0;
            _symbolBroker
                .Setup(b => b.TryGetValue("b", out bVal))
                .Returns(true);

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(11.0);
            expr.Parameters.Keys.Should().Contain(new[] { "a", "b" });
            expr.Volatile.Should().BeTrue("values came from SymbolBroker");
        }

        [Test]
        public void Expression_Evaluate_DateTimeSymbolBrokerParameter_ShouldUseUnixSeconds() {
            // arrange
            DateTime eventTime = new DateTime(2026, 5, 12, 12, 34, 56, DateTimeKind.Utc);
            double expectedUnixSeconds = CoreUtil.ToUnixSeconds(eventTime);
            var expr = CreateExpression("eventTime + 60");

            object eventTimeValue = eventTime;
            _symbolBroker
                .Setup(b => b.TryGetValue("eventTime", out eventTimeValue))
                .Returns(true);

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(expectedUnixSeconds + 60);
            expr.Parameters["eventTime"].Should().Be(expectedUnixSeconds);
            expr.Volatile.Should().BeTrue("values came from SymbolBroker");
        }

        [Test]
        public void Expression_Evaluate_DateOnlySymbolBrokerParameter_ShouldUseUnixSecondsAtMidnight() {
            // arrange
            DateOnly eventDate = new DateOnly(2026, 5, 12);
            double expectedUnixSeconds = CoreUtil.ToUnixSeconds(eventDate.ToDateTime(TimeOnly.MinValue));
            var expr = CreateExpression("eventDate + 86400");

            object eventDateValue = eventDate;
            _symbolBroker
                .Setup(b => b.TryGetValue("eventDate", out eventDateValue))
                .Returns(true);

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(expectedUnixSeconds + 86400);
            expr.Parameters["eventDate"].Should().Be(expectedUnixSeconds);
            expr.Volatile.Should().BeTrue("values came from SymbolBroker");
        }

        [Test]
        public void Expression_Evaluate_TimeOnlySymbolBrokerParameter_ShouldUseSecondsSinceMidnight() {
            // arrange
            TimeOnly timeOnly = new TimeOnly(12, 34, 56);
            double expectedSeconds = timeOnly.ToTimeSpan().TotalSeconds;
            var expr = CreateExpression("TimeOnly + 30");

            object timeOnlyValue = timeOnly;
            _symbolBroker
                .Setup(b => b.TryGetValue("TimeOnly", out timeOnlyValue))
                .Returns(true);

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(expectedSeconds + 30);
            expr.Parameters["TimeOnly"].Should().Be(expectedSeconds);
            expr.Volatile.Should().BeTrue("values came from SymbolBroker");
        }

        [Test]
        public void Expression_Evaluate_ReentrantEvaluationDuringValidator_ShouldNotReevaluate() {
            // arrange
            int brokerReads = 0;

            _symbolBroker
                .Setup(b => b.TryGetValue("volatileValue", out It.Ref<object>.IsAny))
                .Callback((string key, out object value) => {
                    brokerReads++;
                    if (brokerReads > 1) {
                        throw new InvalidOperationException("Expression was reevaluated while already evaluating.");
                    }
                    value = brokerReads;
                })
                .Returns(true);

            Expression expr = new Expression("", _context.Object) {
                SymbolBroker = _symbolBroker.Object,
                IsExpression = true,
                Type = "int"
            };
            expr.Validator = e => e.Evaluate(ignoreRoot: true);

            // act
            expr.Definition = "volatileValue";
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(1);
            brokerReads.Should().Be(1);
        }

        [Test]
        public void Expression_Evaluate_MissingParameter_ShouldProduceUndefinedError() {
            // arrange
            var expr = CreateExpression("a + b");

            expr.Definition = "a + b";

            // only a is provided
            object aVal = 1.0;
            _symbolBroker
                .Setup(b => b.TryGetValue("a", out aVal))
                .Returns(true);

            // no setup for b -> TryGetValue returns false by default

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().NotBeNullOrEmpty();
            expr.Error.Should().Contain(Loc.Instance["LblUndefined"]);
            expr.Error.Should().Contain("b");
        }

        [Test]
        public void Expression_Evaluate_ExecutedVariableWithoutResult_ShouldReportInvalidValueNotUndefined() {
            // arrange
            SequenceRootContainer root = new SequenceRootContainer {
                SymbolBroker = _symbolBroker.Object
            };
            Variable variable = new Variable {
                SymbolBroker = _symbolBroker.Object,
                Expr = new Expression("", root) {
                    SymbolBroker = _symbolBroker.Object
                },
                OriginalExpr = new Expression("1", root) {
                    SymbolBroker = _symbolBroker.Object
                },
                Executed = true
            };
            root.Add(variable);
            variable.Identifier = "target";

            Expression expr = new Expression("target + 1", variable) {
                SymbolBroker = _symbolBroker.Object,
                IsExpression = true
            };

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().Be("Invalid value: target");
            expr.Error.Should().NotContain(Loc.Instance["LblUndefined"]);
        }

        [Test]
        public void Expression_Evaluate_SyntaxError_ShouldSetSyntaxErrorMessage() {
            // arrange
            var expr = CreateExpression("1 +"); // invalid

            // The Definition setter already sets Error/LblSyntaxError on first parse, but we re-check Evaluate
            expr.Definition = "1 +";

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().Be(Loc.Instance["LblSyntaxError"]);
        }

        [Test]
        public void Expression_Evaluate_OutOfRangeValue_ShouldSetRangeError() {
            // arrange
            var expr = CreateExpression("100"); 

            // inclusive [0, 10]
            expr.Range = new[] { 0.0, 10.0, 0.0 };
            expr.Definition = "100";

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().NotBeNullOrEmpty();
            expr.Error.Should().Contain("0");
            expr.Error.Should().Contain("10");
        }

        [Test]
        public void Expression_Evaluate_InRangeValue_ShouldNotProduceRangeError() {
            // arrange
            var expr = CreateExpression("5");
            expr.Range = new[] { 0.0, 10.0, 0.0 };
            expr.Definition = "5";

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Value.Should().Be(5.0);
            expr.Error.Should().BeNull();
        }

        [Test]
        public void Expression_Evaluate_FunctionCallViaSymbolBroker_ShouldSetResult_AndMarkGlobalVolatile() {
            // arrange
            var expr = CreateExpression("myFunc(1)");
            expr.Definition = "myFunc(1)";

            _symbolBroker
                .Setup(b => b.InvokeFunction(
                    "myFunc",
                    It.IsAny<ISymbolFunctionArguments>(),
                    out It.Ref<object>.IsAny,
                    out It.Ref<bool>.IsAny))
                .Callback((string name, ISymbolFunctionArguments args, out object result, out bool isVolatile) => {
                    result = 42.0;
                    isVolatile = true;
                });

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(42.0);
            expr.GlobalVolatile.Should().BeTrue();
        }

        [Test]
        public void Expression_Evaluate_FunctionCallException_ShouldSurfaceErrorMessage() {
            // arrange
            var expr = CreateExpression("failFunc(1)");
            expr.Definition = "failFunc(1)";

            _symbolBroker
                .Setup(b => b.InvokeFunction(
                    "failFunc",
                    It.IsAny<ISymbolFunctionArguments>(),
                    out It.Ref<object>.IsAny,
                    out It.Ref<bool>.IsAny))
                .Callback((string name, ISymbolFunctionArguments args, out object result, out bool isVolatile) => {
                    result = null;
                    isVolatile = false;
                    throw new InvalidOperationException("Boom");
                });

            // act
            expr.Evaluate(ignoreRoot: true);

            // assert
            expr.Error.Should().Be("Boom");
        }

        [Test]
        public void Expression_Validator_AfterValueAssignment_IsCalled() {
            var validator = new Mock<Action<Expression>>();
            var sut = new Expression();

            sut.Validator = validator.Object;
            sut.Value = 10;

            validator.Verify(v => v(It.Is<Expression>(x => ReferenceEquals(x,sut))), Times.Once);
        }

        [Test]
        // 10 <= Value <= 100
        [TestCase(50, 10, 100, 0, true)]
        [TestCase(10, 10, 100, 0, true)]
        [TestCase(100, 10, 100, 0, true)]
        [TestCase(9.9999, 10, 100, 0, false)]
        [TestCase(100.0001, 10, 100, 0, false)]
        // 10 < Value <= 100
        [TestCase(50, 10, 100, 1, true)]
        [TestCase(10.0001, 10, 100, 1, true)]
        [TestCase(100, 10, 100, 1, true)]
        [TestCase(10, 10, 100, 1, false)]
        [TestCase(100.0001, 10, 100, 1, false)]
        // 10 <= Value < 100
        [TestCase(50, 10, 100, 2, true)]
        [TestCase(99.9999, 10, 100, 2, true)]
        [TestCase(10, 10, 100, 2, true)]
        [TestCase(9.9999, 10, 100, 2, false)]
        [TestCase(100, 10, 100, 2, false)]
        // 10 < Value < 100
        [TestCase(50, 10, 100, 3, true)]
        [TestCase(10.0001, 10, 100, 3, true)]
        [TestCase(99.9999, 10, 100, 3, true)]
        [TestCase(10, 10, 100, 3, false)]
        [TestCase(100, 10, 100, 3, false)]
        // 10 <= Value
        [TestCase(50, 10, 0, 0, true)]
        [TestCase(10, 10, 0, 0, true)]
        [TestCase(9.9999, 10, 0, 0, false)]
        // 10 < Value
        [TestCase(50, 10, 0, 1, true)]
        [TestCase(10.0001, 10, 0, 1, true)]
        [TestCase(10, 10, 0, 1, false)]
        public void Expression_CheckRange_RangeTests(double value, int min, int max, int flag, bool valid) {
            var sut = new Expression();
            sut.Range = new double[] { min, max, flag };

            sut.Value = value;

            if (valid) {
                sut.Error.Should().BeNullOrEmpty();
            } else {
                sut.Error.Should().NotBeNullOrEmpty();
            }
        }

        [Test]
        public void Expression_CheckRange_MinAndMax_OutOfMaxRange_ErrorGenerated() {
            var sut = new Expression();
            sut.Range = new double[] { 0, 10, 0 };

            sut.Value = 20;
            sut.Error.Should().Be("Value must be between 0 and 10.");
        }

        [Test]
        public void Expression_CheckRange_MinAndMax_OutOfMinRange_ErrorGenerated() {
            var sut = new Expression();
            sut.Range = new double[] { 10, 20, 0 };

            sut.Value = 5;
            sut.Error.Should().Be("Value must be between 10 and 20.");
        }

        [Test]
        public void Expression_CheckRange_MinRange_BelowMinRange_MinInclusive_ErrorGenerated() {
            var sut = new Expression();
            sut.Range = new double[] { 10, 0, 0 };

            sut.Value = 5;
            sut.Error.Should().Be("Value must be greater than or equal to 10.");
        }

        [Test]
        public void Expression_CheckRange_MinRange_BelowMinRange_MinExclusive_ErrorGenerated() {
            var sut = new Expression();
            sut.Range = new double[] { 10, 0, 1 };

            sut.Value = 5;
            sut.Error.Should().Be("Value must be greater than 10.");
        }

        [Test]
        public void Expression_CheckRange_MinMaxRange_MinExclusive_ExactlyMin_ErrorGenerated() {
            var sut = new Expression();
            sut.Range = new double[] { 10, 20, 1 };

            sut.Value = 10;
            sut.Error.Should().Be("Value must be greater than 10 and up to 20.");
        }

        [Test]
        public void Expression_CheckRange_MinMaxRange_MaxExclusive_ExactlyMax_ErrorGenerated() {
            var sut = new Expression();
            sut.Range = new double[] { 10, 20, 2 };

            sut.Value = 20;
            sut.Error.Should().Be("Value must be between 10 and less than 20.");
        }

        [Test]
        public void Expression_CheckRange_MinMaxRange_MinMaxExclusive_ExactlyMin_ErrorGenerated() {
            var sut = new Expression();
            sut.Range = new double[] { 10, 20, 3 };

            sut.Value = 10;
            sut.Error.Should().Be("Value must be greater than 10 and less than 20.");
        }

        [Test]
        public void Expression_CheckRange_MinMaxRange_MinMaxExclusive_ExactlyMax_ErrorGenerated() {
            var sut = new Expression();
            sut.Range = new double[] { 10, 20, 3 };

            sut.Value = 20;
            sut.Error.Should().Be("Value must be greater than 10 and less than 20.");
        }

        [Test]
        public void Expression_CheckDefault_NotDuplicated() {
            var sut = CreateExpression("A + B");
            // Simulate initial set of DefaultString (from generator)
            sut.DefaultString = "20";
            // Subsequent checks should be in one set of brackets
            sut.DefaultString.Should().Be("{20}");
            var sut2 = new Expression(sut, null);
            // Regardless of "cloning"
            sut2.DefaultString.Should().Be("{20}");
        }
        
        [Test]
        public void Expression_CheckDefault_Number() {
            var sut = CreateExpression("A");
            // Simulate initial set of DefaultString (from generator)
            sut.Default = 20;
            // Subsequent checks should be in one set of brackets
            sut.DefaultString.Should().Be("{20}");
            var sut2 = new Expression(sut, null);
            // Regardless of "cloning"
            sut2.DefaultString.Should().Be("{20}");
        }

        /// <summary>
        /// Verifies default display text for empty string expressions, pre-braced defaults, and invalid numeric defaults used by generated properties.
        /// </summary>
        [Test]
        public void Expression_DefaultString_FormatsStringBracedAndInvalidNumericDefaults() {
            var stringExpression = new Expression("", _context.Object) { Type = "String" };
            stringExpression.DefaultString = "ignored";

            stringExpression.DefaultString.Should().BeEmpty();

            var bracedExpression = new Expression("", _context.Object);
            bracedExpression.DefaultString = "{Auto}";

            bracedExpression.DefaultString.Should().Be("{Auto}");

            var noDefaultExpression = new Expression("", _context.Object);

            noDefaultExpression.DefaultString.Should().Be("--");

            var invalidExpression = new Expression("notDefined", _context.Object);
            invalidExpression.Default = 12;
            invalidExpression.DefaultString = "Fallback";

            invalidExpression.DefaultString.Should().Be("{Fallback}");
        }

        /// <summary>
        /// Verifies expression diagnostics distinguish warning-only unresolved variables from hard errors when collecting validation issues.
        /// </summary>
        [Test]
        public void Expression_ValidateExpressions_AddsOnlyHardErrorsToIssues() {
            List<string> issues = new List<string>();
            Expression warning = new Expression("", _context.Object) {
                Error = Loc.Instance["LblNotEvaluated"] + ": pending"
            };
            Expression hardError = new Expression("", _context.Object) {
                Error = Loc.Instance["LblUndefined"] + ": missing"
            };

            Expression.ValidateExpressions(issues, warning, hardError);

            issues.Should().ContainSingle().Which.Should().Contain(Loc.Instance["LblUndefined"]);
        }

        /// <summary>
        /// Verifies expression refresh helpers clear cached parameters and force a fresh broker-backed evaluation.
        /// </summary>
        [Test]
        public void Expression_RefreshRemoveParameterAndReferenceRemoved_ReevaluateBrokerValues() {
            SequenceRootContainer root = new SequenceRootContainer();
            var parent = new SequentialContainer();
            root.Add(parent);
            _context.SetupGet(c => c.Parent).Returns(parent);
            object brokerValue = 2.0;
            _symbolBroker.Setup(b => b.TryGetValue("brokerValue", out brokerValue)).Returns(true);
            Expression sut = CreateExpression("brokerValue + 1");
            sut.Definition = "brokerValue + 1";

            sut.Evaluate(ignoreRoot: true);
            sut.Value.Should().Be(3);

            sut.RemoveParameter("brokerValue");
            sut.Refresh();

            sut.Parameters.Should().ContainKey("brokerValue");
            sut.Value.Should().Be(3);

            var removedSymbol = new NINA.Sequencer.SequenceItem.Expressions.Variable {
                Identifier = "brokerValue",
                Expr = new Expression("1", parent)
            };
            sut.ReferenceRemoved(removedSymbol);

            sut.Resolved.Should().ContainKey("brokerValue");
            sut.Resolved["brokerValue"].Should().BeNull();
        }

        /// <summary>
        /// Verifies expression stringification reports undefined, error, and evaluated states with enough context for diagnostics.
        /// </summary>
        [Test]
        public void Expression_ToString_ReportsUndefinedErrorAndEvaluatedStates() {
            Expression undefined = new Expression("", _context.Object);
            Expression errored = new Expression("missing + 1", _context.Object) {
                Error = "boom"
            };
            Expression evaluated = CreateExpression("1 + 2");
            evaluated.Evaluate(ignoreRoot: true);

            undefined.ToString().Should().Contain("Undefined").And.Contain("TestContext");
            errored.ToString().Should().Contain("boom").And.Contain("missing + 1");
            evaluated.ToString().Should().Contain("Expression: 1 + 2").And.Contain("Value: 3");
        }
    }
}
