using FluentAssertions;
using NINA.Core.Enum;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NUnit.Framework;

namespace NINA.Test.Image.ImageData {

    [TestFixture]
    public class DebayeredImageBehaviorTest {

        [Test]
        public void ReRender_PreservesBayerPatternAndChannelData() {
            var utility = new global::NINA.Test.ImageDataFactoryTestUtility();
            ushort[] pixels = {
                100, 2000, 300, 4000,
                5000, 600, 7000, 800,
                900, 10000, 1100, 12000,
                13000, 1400, 15000, 1600
            };
            var imageData = utility.ImageDataFactory.CreateBaseImageData(
                input: pixels,
                width: 4,
                height: 4,
                bitDepth: 16,
                isBayered: true,
                metaData: new ImageMetaData());

            var original = imageData.RenderImage().Debayer(
                saveColorChannels: true,
                saveLumChannel: true,
                bayerPattern: SensorType.GBRG);

            var reRendered = original.ReRender();

            reRendered.Should().BeAssignableTo<IDebayeredImage>();
            var reRenderedDebayered = (IDebayeredImage)reRendered;
            reRenderedDebayered.BayerPattern.Should().Be(SensorType.GBRG);
            reRenderedDebayered.SaveColorChannels.Should().BeTrue();
            reRenderedDebayered.SaveLumChannel.Should().BeTrue();
            reRenderedDebayered.DebayeredData.Red.Should().Equal(original.DebayeredData.Red);
            reRenderedDebayered.DebayeredData.Green.Should().Equal(original.DebayeredData.Green);
            reRenderedDebayered.DebayeredData.Blue.Should().Equal(original.DebayeredData.Blue);
            reRenderedDebayered.DebayeredData.Lum.Should().Equal(original.DebayeredData.Lum);
        }
    }
}
