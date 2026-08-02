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
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Test.Sequencer.Logic {
    [TestFixture]
    public class NCalcIdentifierValidationTest {

        [Test]
        [TestCase("myVar")]
        [TestCase("my_var")]
        [TestCase("myVar123")]
        [TestCase("_private")]
        public void NCalc_ShouldAcceptValidIdentifiers(string identifier) {
            var expr = new NCalc.Expression($"{identifier} + 1");
            var parameters = new Dictionary<string, object> {
                { identifier, 5.0 }
            };
            AddParameters(expr, parameters);

            Action act = () => {
                var result = expr.Evaluate();
            };

            act.Should().NotThrow();
            var result = expr.Evaluate();
            result.Should().Be(6.0);
        }

        [Test]
        [TestCase("my-var", "hyphen")]
        [TestCase("my+var", "plus sign")]
        public void NCalc_ShouldNotAcceptIdentifiersWithOperators(string identifier, string reason) {
            // When we try to use an identifier with operator characters,
            // NCalc will parse it as an expression with the operator
            var expr = new NCalc.Expression($"{identifier} + 1");
            var parameters = new Dictionary<string, object> {
                { identifier, 5.0 }  // This won't work as expected
            };
            AddParameters(expr, parameters);

            // The expression "my-var + 1" will be parsed as "my - var + 1"
            // not as a single identifier "my-var"
            Action act = () => {
                var result = expr.Evaluate();
            };

            // This will throw because 'my' and 'var' are undefined parameters
            act.Should().Throw<Exception>($"identifier with {reason} should not work");
        }

        [Test]
        public void NCalc_IdentifierWithHyphen_ParsedAsSubtraction() {
            // Demonstrate that "test-var" is parsed as "test" minus "var"
            var expr = new NCalc.Expression("test-var");
            var parameters = new Dictionary<string, object> {
                { "test", 10.0 },
                { "var", 3.0 }
            };
            AddParameters(expr, parameters);

            var result = expr.Evaluate();
            
            // If "test-var" were a single identifier, this would fail
            // Instead, it's parsed as "test - var" = 10 - 3 = 7
            result.Should().Be(7.0);
        }

        [Test]
        public void NCalc_IdentifierWithPlus_ParsedAsAddition() {
            // Demonstrate that "test+var" is parsed as "test" plus "var"
            var expr = new NCalc.Expression("test+var");
            var parameters = new Dictionary<string, object> {
                { "test", 10.0 },
                { "var", 3.0 }
            };
            AddParameters(expr, parameters);

            var result = expr.Evaluate();
            
            // If "test+var" were a single identifier, this would fail
            // Instead, it's parsed as "test + var" = 10 + 3 = 13
            result.Should().Be(13.0);
        }

        private static void AddParameters(NCalc.Expression expression, IReadOnlyDictionary<string, object> parameters) {
            foreach (KeyValuePair<string, object> parameter in parameters) {
                expression.Parameters[parameter.Key] = parameter.Value;
            }
        }
    }
}
