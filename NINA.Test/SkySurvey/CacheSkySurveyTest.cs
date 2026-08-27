using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.WPF.Base.SkySurvey;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace NINA.Test.SkySurvey {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class CacheSkySurveyTest {
        private string cachePath;

        [SetUp]
        public void SetUp() {
            cachePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "SkySurveyCacheTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown() {
            if (Directory.Exists(cachePath)) {
                Directory.Delete(cachePath, true);
            }
        }

        [TestCase(21.073240111482587, 45.153436349090754, 0, true)]
        [TestCase(21.073240111482587, -45.153436349090754, 0, true)]
        [TestCase(0.001, 89, 0, true)]
        [TestCase(23.999, -89, 0, true)]
        [TestCase(0.001, 45.153436349090754, 180, false)]
        [TestCase(23.999, -45.153436349090754, 180, false)]
        public void Render_WhenZoomChanges_PreservesTileOrientation(double raHours, double declination, double rotation, bool bottomRight) {
            Coordinates tileCoordinates = new Coordinates(
                raHours,
                declination,
                Epoch.J2000,
                Coordinates.RAType.Hours);
            CacheSkySurvey cache = new CacheSkySurvey(cachePath);
            cache.SaveImageToCache(new SkySurveyImage {
                Name = "Issue 71 directional tile",
                Source = nameof(FileSkySurvey),
                Coordinates = tileCoordinates,
                Rotation = 179.44002835323062,
                FoVWidth = 210.89612594552327,
                FoVHeight = 210.89612594552327,
                Image = CreateDirectionalBitmapSource()
            });
            CacheSkySurveyImageFactory sut = new CacheSkySurveyImageFactory(400, 190, cache);

            foreach (double fieldOfView in new[] { 10.0, 12.0, 20.0, 12.0, 10.0 }) {
                var marker = FindRedMarkerCentroid(sut.Render(tileCoordinates, fieldOfView, rotation));
                if (bottomRight) {
                    marker.X.Should().BeGreaterThan(200);
                    marker.Y.Should().BeGreaterThan(95);
                } else {
                    marker.X.Should().BeLessThan(200);
                    marker.Y.Should().BeLessThan(95);
                }
            }
        }

        private static BitmapSource CreateDirectionalBitmapSource() {
            const int width = 32;
            const int height = 32;
            byte[] pixels = new byte[width * height * 4];
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int offset = (y * width + x) * 4;
                    bool marker = x < 8 && y < 8;
                    pixels[offset] = marker ? (byte)0 : (byte)255;
                    pixels[offset + 1] = 0;
                    pixels[offset + 2] = marker ? (byte)255 : (byte)0;
                    pixels[offset + 3] = 255;
                }
            }

            BitmapSource source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            source.Freeze();
            return source;
        }

        private static System.Windows.Point FindRedMarkerCentroid(BitmapSource source) {
            int stride = source.PixelWidth * 4;
            byte[] pixels = new byte[stride * source.PixelHeight];
            source.CopyPixels(pixels, stride, 0);
            double totalX = 0;
            double totalY = 0;
            int count = 0;
            for (int y = 0; y < source.PixelHeight; y++) {
                for (int x = 0; x < source.PixelWidth; x++) {
                    int offset = y * stride + x * 4;
                    if (pixels[offset + 2] > 160 && pixels[offset] < 100 && pixels[offset + 1] < 100) {
                        totalX += x;
                        totalY += y;
                        count++;
                    }
                }
            }

            count.Should().BeGreaterThan(0);
            return new System.Windows.Point(totalX / count, totalY / count);
        }
    }
}
