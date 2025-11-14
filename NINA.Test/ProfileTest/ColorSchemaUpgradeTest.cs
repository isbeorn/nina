using FluentAssertions;
using NINA.Core.Utility.ColorSchema;
using NINA.Profile;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace NINA.Test.ProfileTest {
    [TestFixture]
    public class ColorSchemaUpgradeTest {
        [TestCase("Light", "Dark")]
        [TestCase("Custom", "Dark")]
        [TestCase("Light", "Alternative Custom")]
        [TestCase("Custom", "Alternative Custom")]
        [TestCase("Alternative Custom", "Light")]
        [TestCase("Dark", "Custom")]
        public void Deserialize_RefreshesBuiltinsAndPreservesEachCustomSelection(string primary, string alternate) {
            var settings = new ColorSchemaSettings {
                ColorSchema = new ColorSchema { Name = primary, PrimaryColor = Colors.Magenta },
                AltColorSchema = new ColorSchema { Name = alternate, PrimaryColor = Colors.Lime }
            };
            var serializer = new DataContractSerializer(typeof(ColorSchemaSettings));
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, settings);
            stream.Position = 0;
            var restored = (ColorSchemaSettings)serializer.ReadObject(stream);

            AssertSelection(restored, restored.ColorSchema, primary, Colors.Magenta);
            AssertSelection(restored, restored.AltColorSchema, alternate, Colors.Lime);
        }

        private static void AssertSelection(ColorSchemaSettings settings, ColorSchema selected, string name, Color customColor) {
            selected.Name.Should().Be(name);
            selected.Should().BeSameAs(settings.ColorSchemas.Items.Single(x => x.Name == name));
            var expected = name.Contains("Custom") ? customColor : ColorSchemas.ReadColorSchemas().Items.Single(x => x.Name == name).PrimaryColor;
            selected.PrimaryColor.Should().Be(expected);
        }
    }
}
