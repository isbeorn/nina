#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility.ValidationRules;
using NUnit.Framework;
using System.Globalization;
using System.Threading;
using System.Windows.Controls;

namespace NINA.Test.Utility.ValidationRules {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class CoreValidationRulesTest {

        /// <summary>
        /// Verifies integer range validation accepts in-range values and rejects out-of-range or non-numeric text.
        /// </summary>
        [Test]
        public void IntRangeRule_RepresentativeInputs_ReturnsExpectedValidationResult() {
            IntRangeRule rule = new IntRangeRule {
                ValidRange = new IntRangeChecker {
                    Minimum = 1,
                    Maximum = 5
                }
            };

            ValidationResult valid = rule.Validate("3", CultureInfo.InvariantCulture);
            ValidationResult tooLow = rule.Validate("0", CultureInfo.InvariantCulture);
            ValidationResult invalidText = rule.Validate("abc", CultureInfo.InvariantCulture);

            Assert.That(valid.IsValid, Is.True);
            Assert.That(tooLow.IsValid, Is.False);
            Assert.That(invalidText.IsValid, Is.False);
        }

        /// <summary>
        /// Verifies the integer range variant permits the persisted default sentinel while still enforcing other bounds.
        /// </summary>
        [Test]
        public void IntRangeRuleWithDefault_MinusOne_AllowsDefaultSentinel() {
            IntRangeRuleWithDefault rule = new IntRangeRuleWithDefault {
                ValidRange = new IntRangeChecker {
                    Minimum = 10,
                    Maximum = 20
                }
            };

            Assert.That(rule.Validate("-1", CultureInfo.InvariantCulture).IsValid, Is.True);
            Assert.That(rule.Validate("9", CultureInfo.InvariantCulture).IsValid, Is.False);
            Assert.That(rule.Validate("10", CultureInfo.InvariantCulture).IsValid, Is.True);
        }

        /// <summary>
        /// Verifies double range validation uses the provided culture and permits the default sentinel value.
        /// </summary>
        [Test]
        public void DoubleRangeRule_CultureAwareInputs_ValidatesRangeAndDefaultSentinel() {
            DoubleRangeRule rule = new DoubleRangeRule {
                ValidRange = new DoubleRangeChecker {
                    Minimum = 1.5,
                    Maximum = 2.5
                }
            };

            Assert.That(rule.Validate("2,25", CultureInfo.GetCultureInfo("de-DE")).IsValid, Is.True);
            Assert.That(rule.Validate("-1", CultureInfo.InvariantCulture).IsValid, Is.True);
            Assert.That(rule.Validate("3.5", CultureInfo.InvariantCulture).IsValid, Is.False);
            Assert.That(rule.Validate("bad", CultureInfo.InvariantCulture).IsValid, Is.False);
        }

        /// <summary>
        /// Verifies non-negative numeric validation accepts zero and positive values while rejecting negatives and invalid text.
        /// </summary>
        [Test]
        public void GreaterZeroRule_RepresentativeInputs_RequiresNonNegativeNumber() {
            GreaterZeroRule rule = new GreaterZeroRule();

            Assert.That(rule.Validate("0", CultureInfo.InvariantCulture).IsValid, Is.True);
            Assert.That(rule.Validate("3.5", CultureInfo.InvariantCulture).IsValid, Is.True);
            Assert.That(rule.Validate("-0.1", CultureInfo.InvariantCulture).IsValid, Is.False);
            Assert.That(rule.Validate("abc", CultureInfo.InvariantCulture).IsValid, Is.False);
        }

        /// <summary>
        /// Verifies IP address validation accepts IPv4 and IPv6 addresses but rejects hostnames.
        /// </summary>
        [Test]
        public void IsValidIPAddressRule_IPv4IPv6AndHostname_ReturnsExpectedValidity() {
            IsValidIPAddressRule rule = new IsValidIPAddressRule();

            Assert.That(rule.Validate("192.168.1.10", CultureInfo.InvariantCulture).IsValid, Is.True);
            Assert.That(rule.Validate("::1", CultureInfo.InvariantCulture).IsValid, Is.True);
            Assert.That(rule.Validate("nina.local", CultureInfo.InvariantCulture).IsValid, Is.False);
        }

        /// <summary>
        /// Verifies IP port validation enforces the inclusive IPEndPoint port range.
        /// </summary>
        [Test]
        public void IsValidIpPortRule_PortBoundaries_ReturnsExpectedValidity() {
            IsValidIpPortRule rule = new IsValidIpPortRule();

            Assert.That(rule.Validate("0", CultureInfo.InvariantCulture).IsValid, Is.True);
            Assert.That(rule.Validate("65535", CultureInfo.InvariantCulture).IsValid, Is.True);
            Assert.That(rule.Validate("-1", CultureInfo.InvariantCulture).IsValid, Is.False);
            Assert.That(rule.Validate("65536", CultureInfo.InvariantCulture).IsValid, Is.False);
            Assert.That(rule.Validate("not-a-port", CultureInfo.InvariantCulture).IsValid, Is.False);
        }
    }
}
