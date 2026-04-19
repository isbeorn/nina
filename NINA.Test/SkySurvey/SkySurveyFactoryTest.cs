using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Image.Interfaces;
using NINA.WPF.Base.SkySurvey;

namespace NINA.Test.SkySurvey {

    [TestFixture]
    public class SkySurveyFactoryTest {

        /// <summary>
        /// Verifies that each persisted sky-survey enum value creates the expected provider implementation.
        /// This protects framing-assistant compatibility when saved profiles or templates specify a survey source.
        /// </summary>
        [TestCase(SkySurveySource.NASA, "NASASkySurvey")]
        [TestCase(SkySurveySource.SKYSERVER, "SkyServerSkySurvey")]
        [TestCase(SkySurveySource.STSCI, "StsciSkySurvey")]
        [TestCase(SkySurveySource.ESO, "ESOSkySurvey")]
        [TestCase(SkySurveySource.HIPS2FITS, "Hips2FitsSurvey")]
        [TestCase(SkySurveySource.FILE, "FileSkySurvey")]
        [TestCase(SkySurveySource.SKYATLAS, "SkyAtlasSkySurvey")]
        public void Create_ForKnownSource_ReturnsExpectedSurveyType(SkySurveySource source, string expectedTypeName) {
            SkySurveyFactory factory = new SkySurveyFactory(Mock.Of<IImageDataFactory>());

            ISkySurvey survey = factory.Create(source);

            survey.GetType().Name.Should().Be(expectedTypeName);
        }

        /// <summary>
        /// Verifies that unknown enum values fall back to NASA imagery rather than returning null.
        /// This documents the compatibility behavior for future enum expansion or corrupt persisted values.
        /// </summary>
        [Test]
        public void Create_ForUnknownSource_ReturnsNasaSkySurveyFallback() {
            SkySurveyFactory factory = new SkySurveyFactory(Mock.Of<IImageDataFactory>());

            ISkySurvey survey = factory.Create((SkySurveySource)999);

            survey.GetType().Name.Should().Be("NASASkySurvey");
        }

        /// <summary>
        /// Verifies the cache-source strings used to identify cached images from each source.
        /// This protects cache lookup compatibility across survey-provider type names and persisted CacheInfo.xml entries.
        /// </summary>
        [TestCase(SkySurveySource.NASA, "NASASkySurvey")]
        [TestCase(SkySurveySource.SKYSERVER, "SkyServerSkySurvey")]
        [TestCase(SkySurveySource.STSCI, "StsciSkySurvey")]
        [TestCase(SkySurveySource.ESO, "ESOSkySurvey")]
        [TestCase(SkySurveySource.HIPS2FITS, "Hips2FitsSurvey")]
        [TestCase(SkySurveySource.SKYATLAS, "SkyAtlasSkySurvey")]
        [TestCase(SkySurveySource.FILE, "FileSkySurvey")]
        [TestCase(SkySurveySource.CACHE, "CacheSkySurvey")]
        public void GetCacheSourceString_ForKnownSource_ReturnsProviderTypeName(SkySurveySource source, string expectedName) {
            source.GetCacheSourceString().Should().Be(expectedName);
        }

        /// <summary>
        /// Verifies that unknown source values produce an empty cache-source string.
        /// This prevents accidental cache hits for unrecognized persisted enum values.
        /// </summary>
        [Test]
        public void GetCacheSourceString_ForUnknownSource_ReturnsEmptyString() {
            ((SkySurveySource)999).GetCacheSourceString().Should().BeEmpty();
        }
    }
}
