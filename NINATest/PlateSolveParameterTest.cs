using NINA.PlateSolving;
using NUnit.Framework;
using System.Collections.Generic;
using FluentAssertions;
using System.Reflection;
using System;
using NINA.Utility.Astrometry;
using System.Collections.ObjectModel;

namespace NINATest {

    [TestFixture]
    internal class PlateSolveParameterTest {

        [Test]
        public void GetLabelForOptionalPropertyTest_success() {
            var expected = new Dictionary<string, string>() {
                { "FocalLength", "LblFocalLength" },
                { "PixelSize", "LblPixelSize" },
            };
            foreach (var kvp in expected) {
                var actualLabel = PlateSolveParameter.GetLabelForOptionalProperty(kvp.Key);
                var expectedLabel = kvp.Value;
                actualLabel.Should().Be(expectedLabel);
            }
        }

        [Test]
        public void GetLabelForOptionalProperty_invalid_throws() {
            Assert.Throws<ArgumentException>(
                () => PlateSolveParameter.GetLabelForOptionalProperty("Invalid"));
        }

        [Test]
        public void AllOptionalPropertiesHaveLabels() {
            var properties = typeof(PlateSolveParameter).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties) {
                var propertyType = property.PropertyType;
                if (!propertyType.IsGenericType || (propertyType.GetGenericTypeDefinition() != typeof(Nullable<>))) {
                    continue;
                }
                var genericArguments = propertyType.GetGenericArguments();
                if (genericArguments.Length != 1 || genericArguments[0] != typeof(Double)) {
                    continue;
                }

                Assert.DoesNotThrow(
                    () => PlateSolveParameter.GetLabelForOptionalProperty(property.Name),
                    $"{property.Name} is an optional property that does not have a label");
            }
        }

        [Test]
        public void CopyAndUpdateTest() {
            var original = new PlateSolveParameter() {
                FocalLength = null,
                PixelSize = null,
                SearchRadius = 1,
                Regions = 2,
                DownSampleFactor = 3,
                MaxObjects = 4,
                Coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000)
            };

            var expectedFocalLength = 10.0d;
            var expectedPixelSize = 20.0d;
            var updated = original.Clone();
            updated.Update(new ReadOnlyDictionary<string, double?>(
                new Dictionary<string, double?> {
                    { "FocalLength", expectedFocalLength },
                    { "PixelSize",  expectedPixelSize }
                }));

            updated.FocalLength.Should().Be(expectedFocalLength);
            updated.PixelSize.Should().Be(expectedPixelSize);
            updated.SearchRadius.Should().Be(original.SearchRadius);
            updated.Regions.Should().Be(original.Regions);
            updated.DownSampleFactor.Should().Be(original.DownSampleFactor);
            updated.MaxObjects.Should().Be(original.MaxObjects);
            updated.Coordinates.Should().Be(original.Coordinates);
        }
    }
}