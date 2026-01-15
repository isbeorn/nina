using FluentAssertions;
using NINA.Astrometry;
using System.Collections.Concurrent;
using System.Reflection;

namespace NINA.Test.AstrometryTest {
    [TestFixture]
    [NonParallelizable]
    public class DeltaUTCacheTest {
        [TestCase(-1, 0)]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(10, 0)]
        [TestCase(-1, 0.25)]
        [TestCase(0, -0.25)]
        [TestCase(1, 0.25)]
        [TestCase(10, -0.25)]
        public void CachedCorrection_IsReturnedWithoutLookingUpDatabase(int offset, double correction) {
            WithIsolatedCache(() => {
                var today = DateTime.UtcNow.Date;
                Field("DeltaUTReference").SetValue(null, today);
                if (Math.Abs(offset) <= 1) {
                    Field(offset == -1 ? "DeltaUTYesterday" : offset == 1 ? "DeltaUTTomorrow" : "DeltaUTToday").SetValue(null, correction);
                } else {
                    Cache()[today.AddDays(offset)] = correction;
                }
                var unavailableDatabase = new DatabaseInteraction("Data Source=:memory:;");
                AstroUtil.DeltaUT(today.AddDays(offset), unavailableDatabase).Should().Be(correction);
                AstroUtil.DeltaUT(today.AddDays(offset), unavailableDatabase).Should().Be(correction);
            });
        }

        [Test]
        public void NewUtcDay_ResetsShortCacheAndUsesCorrectionForActualDate() {
            WithIsolatedCache(() => {
                var today = DateTime.UtcNow.Date;
                Field("DeltaUTReference").SetValue(null, today.AddDays(-1));
                Field("DeltaUTToday").SetValue(null, 99d);
                Cache()[today] = 0d;
                AstroUtil.DeltaUT(today, new DatabaseInteraction("Data Source=:memory:;")).Should().Be(0d);
                Field("DeltaUTReference").GetValue(null).Should().Be(today);
            });
        }

        private static FieldInfo Field(string name) => typeof(AstroUtil).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
        private static ConcurrentDictionary<DateTime, double> Cache() => (ConcurrentDictionary<DateTime, double>)Field("DeltaUTCache").GetValue(null);
        private static void WithIsolatedCache(Action action) {
            string[] names = { "DeltaUTReference", "DeltaUTToday", "DeltaUTYesterday", "DeltaUTTomorrow", "DeltaUTCache" };
            var saved = names.ToDictionary(x => x, x => Field(x).GetValue(null));
            try {
                foreach (var name in names) Field(name).SetValue(null, null);
                Field("DeltaUTCache").SetValue(null, new ConcurrentDictionary<DateTime, double>());
                action();
            } finally {
                foreach (var name in names) Field(name).SetValue(null, saved[name]);
            }
        }
    }
}
