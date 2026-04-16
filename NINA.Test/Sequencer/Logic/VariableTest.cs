using FluentAssertions;
using Moq;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Expressions;

namespace NINA.Test.Sequencer.Logic {
    [TestFixture]
    public class VariableTest {
        private Mock<ISymbolBroker> _symbolBroker;

        [SetUp]
        public void SetUp() {
            _symbolBroker = new Mock<ISymbolBroker>();
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
        }

        [TearDown]
        public void TearDown() {
            UserSymbol.SymbolCache.Clear();
            UserSymbol.ClearUserSymbols();
        }

        private Variable CreateAttachedVariable(string identifier, string definition = "5") {
            var root = new SequenceRootContainer();
            var variable = new Variable { SymbolBroker = _symbolBroker.Object };
            variable.Expr = new Expression(definition, variable) { SymbolBroker = _symbolBroker.Object };
            variable.OriginalExpr = new Expression(definition, variable) { SymbolBroker = _symbolBroker.Object };
            root.Add(variable);
            variable.Identifier = identifier;
            return variable;
        }

        [Test]
        public void Variable_Validate_WhenExecuted_VolatileExpression_ValueNotChanged() {
            // Arrange: simulate an already-executed Variable whose expression is volatile
            var variable = CreateAttachedVariable("myVar", "5");
            variable.Executed = true;
            variable.Expr.Value = 42.0;    // value was set at execution time
            variable.Expr.Volatile = true; // expression depends on volatile data

            // Act
            variable.Validate();

            // Assert: Validate() must not re-evaluate a volatile expression for an already-executed Variable
            variable.Expr.Value.Should().Be(42.0,
                "Validate() must not change the value of an already-executed Variable");
        }

        [Test]
        public void Variable_Validate_WhenNotExecuted_VolatileExpression_ValueIsReEvaluated() {
            // Arrange: Variable not yet executed; Expr.Value is stale
            var variable = CreateAttachedVariable("myVar", "5");
            // Executed = false by default; AfterParentChanged already set Expr.Error = "Not evaluated"
            variable.Expr.Value = 42.0;    // stale/wrong value
            variable.Expr.Volatile = true;

            // Act
            variable.Validate();

            // Assert: expression IS re-evaluated (definition "5" → 5.0)
            variable.Expr.Value.Should().Be(5.0,
                "Validate() must re-evaluate the expression of an un-executed Variable");
        }
    }
}
