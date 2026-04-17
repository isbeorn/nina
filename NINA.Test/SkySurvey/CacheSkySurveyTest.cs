using FluentAssertions;
using NINA.Astrometry;
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

        /// <summary>
        /// Verifies that a new cache initializes its directory and CacheInfo.xml with an ImageCacheInfo root.
        /// This protects first-run framing assistant behavior on a clean profile or cache folder.
        /// </summary>
        [Test]
        public void Constructor_WhenCacheDoesNotExist_CreatesCacheDirectoryAndInfoFile() {
            CacheSkySurvey cache = new CacheSkySurvey(cachePath);

            Directory.Exists(cachePath).Should().BeTrue();
            File.Exists(Path.Combine(cachePath, "CacheInfo.xml")).Should().BeTrue();
            cache.Cache.Name.LocalName.Should().Be("ImageCacheInfo");
            cache.Cache.Elements("Image").Should().BeEmpty();
        }

        /// <summary>
        /// Verifies backward compatibility for v1.6.0.2 cache entries that do not contain Id or Source attributes.
        /// This preserves old user cache files by assigning stable runtime attributes based on the legacy rotation heuristic.
        /// </summary>
        [Test]
        public void Constructor_WithLegacyCacheInfo_AddsMissingIdAndSourceAttributes() {
            Directory.CreateDirectory(cachePath);
            XElement cacheInfo = new XElement("ImageCacheInfo",
                CreateLegacyCacheImageElement("legacy-nasa.jpg", "0"),
                CreateLegacyCacheImageElement("legacy-file.jpg", "13.5"));
            cacheInfo.Save(Path.Combine(cachePath, "CacheInfo.xml"));

            CacheSkySurvey cache = new CacheSkySurvey(cachePath);
            XElement[] images = cache.Cache.Elements("Image").ToArray();

            images.Should().HaveCount(2);
            foreach (XElement image in images) {
                Guid.TryParse(image.Attribute("Id")?.Value, out Guid parsedId).Should().BeTrue();
                parsedId.Should().NotBeEmpty();
            }
            images[0].Attribute("Source")?.Value.Should().Be("NASASkySurvey");
            images[1].Attribute("Source")?.Value.Should().Be(nameof(FileSkySurvey));
        }

        /// <summary>
        /// Verifies that saving a survey image writes the original image, all thumbnail sizes, and invariant-culture metadata.
        /// This protects cache reuse across locales and ensures the image pyramid needed by the framing overlay is present.
        /// </summary>
        [Test]
        public void SaveImageToCache_NewImage_WritesImageFilesAndMetadata() {
            CacheSkySurvey cache = new CacheSkySurvey(cachePath);
            SkySurveyImage image = CreateSkySurveyImage("Target: M 42?", 5.123456789, -0.00004, 90.5, 120.25, 80.75, "NASASkySurvey");

            XElement element = cache.SaveImageToCache(image);
            string fileName = element.Attribute("FileName")!.Value;
            string imagePath = Path.Combine(cachePath, fileName);

            element.Attribute("Id")!.Value.Should().Be(image.Id.ToString());
            element.Attribute("RA")!.Value.Should().Be(image.Coordinates.RA.ToString("R", CultureInfo.InvariantCulture));
            element.Attribute("Dec")!.Value.Should().Be(image.Coordinates.Dec.ToString("R", CultureInfo.InvariantCulture));
            element.Attribute("FoVW")!.Value.Should().Be(image.FoVWidth.ToString("R", CultureInfo.InvariantCulture));
            element.Attribute("FoVH")!.Value.Should().Be(image.FoVHeight.ToString("R", CultureInfo.InvariantCulture));
            element.Attribute("Rotation")!.Value.Should().Be(image.Rotation.ToString(CultureInfo.InvariantCulture));
            element.Attribute("Source")!.Value.Should().Be("NASASkySurvey");
            fileName.Should().NotContain(":");
            fileName.Should().NotContain("?");
            File.Exists(imagePath).Should().BeTrue();
            File.Exists(CacheImage.GetImagePathForThumbnail(imagePath, CacheImage.BigThumbnailSize)).Should().BeTrue();
            File.Exists(CacheImage.GetImagePathForThumbnail(imagePath, CacheImage.MediumThumbnailSize)).Should().BeTrue();
            File.Exists(CacheImage.GetImagePathForThumbnail(imagePath, CacheImage.SmallThumbnailSize)).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that saving the same sky-survey image twice returns the existing cache entry instead of writing duplicates.
        /// This protects cache stability when the same framing request is repeated.
        /// </summary>
        [Test]
        public void SaveImageToCache_SameImageId_ReturnsExistingElement() {
            CacheSkySurvey cache = new CacheSkySurvey(cachePath);
            SkySurveyImage image = CreateSkySurveyImage("Repeat Target", 1.25, 2.5, 0, 45, 45, "NASASkySurvey");

            XElement first = cache.SaveImageToCache(image);
            XElement second = cache.SaveImageToCache(image);

            second.Should().BeSameAs(first);
            cache.Cache.Elements("Image").Should().ContainSingle();
        }

        /// <summary>
        /// Verifies that cache lookup by source and coordinates restores the stored image metadata and bitmap.
        /// This protects offline framing workflows that reopen a cached survey image without contacting the remote provider.
        /// </summary>
        [Test]
        public async Task GetImage_BySourceAndCoordinates_ReturnsCachedImage() {
            CacheSkySurvey cache = new CacheSkySurvey(cachePath);
            SkySurveyImage image = CreateSkySurveyImage("Cached Target", 8.25, -11.5, 37.25, 66, 44, "NASASkySurvey");
            cache.SaveImageToCache(image);

            SkySurveyImage restored = await cache.GetImage("NASASkySurvey", image.Coordinates.RA, image.Coordinates.Dec, image.Rotation, image.FoVWidth);

            restored.Should().NotBeNull();
            restored.Id.Should().Be(image.Id);
            restored.Name.Should().Be("Cached Target");
            restored.Coordinates.RA.Should().BeApproximately(image.Coordinates.RA, 1e-12);
            restored.Coordinates.Dec.Should().BeApproximately(image.Coordinates.Dec, 1e-12);
            restored.FoVWidth.Should().Be(image.FoVWidth);
            restored.FoVHeight.Should().Be(image.FoVHeight);
            restored.Rotation.Should().Be(image.Rotation);
            restored.Image.PixelWidth.Should().Be(image.Image.PixelWidth);
            restored.Image.PixelHeight.Should().Be(image.Image.PixelHeight);
            restored.Image.IsFrozen.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that deleting a cache element removes the original image, thumbnails, and XML entry.
        /// This protects user cache-management behavior from leaving orphaned files behind.
        /// </summary>
        [Test]
        public void DeleteFromCache_RemovesFilesThumbnailsAndXmlEntry() {
            CacheSkySurvey cache = new CacheSkySurvey(cachePath);
            SkySurveyImage image = CreateSkySurveyImage("Delete Target", 12.25, 22.5, 0, 30, 30, "NASASkySurvey");
            XElement element = cache.SaveImageToCache(image);
            string imagePath = Path.Combine(cachePath, element.Attribute("FileName")!.Value);
            string bigThumbnail = CacheImage.GetImagePathForThumbnail(imagePath, CacheImage.BigThumbnailSize);
            string mediumThumbnail = CacheImage.GetImagePathForThumbnail(imagePath, CacheImage.MediumThumbnailSize);
            string smallThumbnail = CacheImage.GetImagePathForThumbnail(imagePath, CacheImage.SmallThumbnailSize);

            cache.DeleteFromCache(element);

            File.Exists(imagePath).Should().BeFalse();
            File.Exists(bigThumbnail).Should().BeFalse();
            File.Exists(mediumThumbnail).Should().BeFalse();
            File.Exists(smallThumbnail).Should().BeFalse();
            cache.Cache.Elements("Image").Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that clearing the cache deletes files and recreates an empty CacheInfo.xml.
        /// This protects the reset path used when users explicitly clear framing-assistant cache data.
        /// </summary>
        [Test]
        public void Clear_RemovesCacheContentsAndReinitializesInfoFile() {
            CacheSkySurvey cache = new CacheSkySurvey(cachePath);
            cache.SaveImageToCache(CreateSkySurveyImage("Clear Target", 4.5, 6.75, 0, 20, 20, "NASASkySurvey"));

            cache.Clear();

            Directory.GetFiles(cachePath).Should().ContainSingle(path => Path.GetFileName(path) == "CacheInfo.xml");
            cache.Cache.Name.LocalName.Should().Be("ImageCacheInfo");
            cache.Cache.Elements("Image").Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that CacheImage selects the smallest cached thumbnail when the plate is much smaller than the viewport.
        /// This protects the performance-sensitive overlay path from loading full-resolution images unnecessarily.
        /// </summary>
        [Test]
        public void CacheImage_GetImageForScale_WhenPlateIsSmall_LoadsSmallThumbnail() {
            string imagePath = Path.Combine(cachePath, "cache-source.jpg");
            Directory.CreateDirectory(cachePath);
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(40, 40)) {
                bitmap.Save(imagePath);
            }
            string thumbnailPath = CacheImage.GetImagePathForThumbnail(imagePath, CacheImage.SmallThumbnailSize);

            using CacheImage cacheImage = new CacheImage(ra: 1, dec: -0.00001, fovW: 12, fovH: 12, rotation: 0, path: imagePath);
            using System.Drawing.Bitmap rendered = cacheImage.GetImageForScale(totalFieldOfViewDeg: 20, totalWidth: 400);

            rendered.Width.Should().Be(CacheImage.SmallThumbnailSize);
            rendered.Height.Should().Be(CacheImage.SmallThumbnailSize);
            File.Exists(thumbnailPath).Should().BeTrue();
            cacheImage.Coordinates.Dec.Should().Be(0);
        }

        private static SkySurveyImage CreateSkySurveyImage(string name, double raHours, double decDegrees, double rotation, double fovWidth, double fovHeight, string source) {
            return new SkySurveyImage {
                Name = name,
                Source = source,
                Coordinates = new Coordinates(raHours, decDegrees, Epoch.J2000, Coordinates.RAType.Hours),
                Rotation = rotation,
                FoVWidth = fovWidth,
                FoVHeight = fovHeight,
                Image = CreateBitmapSource()
            };
        }

        private static BitmapSource CreateBitmapSource() {
            const int width = 4;
            const int height = 4;
            byte[] pixels = new byte[width * height * 4];
            for (int i = 0; i < pixels.Length; i += 4) {
                pixels[i] = 32;
                pixels[i + 1] = 96;
                pixels[i + 2] = 192;
                pixels[i + 3] = 255;
            }

            BitmapSource source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            source.Freeze();
            return source;
        }

        private static XElement CreateLegacyCacheImageElement(string fileName, string rotation) {
            return new XElement("Image",
                new XAttribute("RA", "1"),
                new XAttribute("Dec", "2"),
                new XAttribute("Rotation", rotation),
                new XAttribute("FoVW", "30"),
                new XAttribute("FoVH", "20"),
                new XAttribute("FileName", fileName),
                new XAttribute("Name", Path.GetFileNameWithoutExtension(fileName)));
        }
    }
}
