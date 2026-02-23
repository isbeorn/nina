using FluentAssertions;
using MdXaml.Plugins;
using Moq;
using NCalc.Handlers;
using NINA.Core.Locale;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
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
                    It.IsAny<FunctionArgs>(),
                    out It.Ref<object>.IsAny,
                    out It.Ref<bool>.IsAny))
                .Callback((string name, FunctionArgs args, out object result, out bool isVolatile) => {
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
                    It.IsAny<FunctionArgs>(),
                    out It.Ref<object>.IsAny,
                    out It.Ref<bool>.IsAny))
                .Callback((string name, FunctionArgs args, out object result, out bool isVolatile) => {
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
    }
}
