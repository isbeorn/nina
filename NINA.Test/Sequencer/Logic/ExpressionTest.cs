using FluentAssertions;
using NINA.Sequencer.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace NINA.Test.Sequencer.Logic {
    [TestFixture]
    public class ExpressionTest {

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
    }
}
