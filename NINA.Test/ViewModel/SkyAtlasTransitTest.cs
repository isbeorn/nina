using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Data.SQLite;
using System.IO;

namespace NINA.Test.ViewModel {
    [TestFixture]
    [Explicit("Uses an isolated process-local LOCALAPPDATA for real Sky Atlas database searches; run alone.")]
    [NonParallelizable]
    public class SkyAtlasTransitTest {
        private string originalLocalAppData;
        private string directory;
        private string database;
        private NighttimeData night;
        private SkyAtlasVM vm;
        private readonly DateTime reference = new DateTime(2031, 1, 1, 12, 0, 0, DateTimeKind.Local);

        [SetUp]
        public async Task SetUp() {
            directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "SkyAtlasTransit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(directory, "NINA"));
            database = Path.Combine(directory, "NINA", "NINA.sqlite");
            ExecuteSql(@"CREATE TABLE dsodetail (
                id TEXT PRIMARY KEY, ra REAL, dec REAL, magnitude REAL, surfacebrightness REAL,
                sizemin NUMERIC, sizemax REAL, positionangle REAL, nrofstars REAL, brighteststar REAL,
                constellation TEXT, dsotype TEXT, dsoclass TEXT, notes REAL, syncedfrom TEXT, lastmodified TEXT);
                CREATE TABLE cataloguenr (dsodetailid TEXT, catalogue TEXT, designation TEXT,
                PRIMARY KEY(dsodetailid, catalogue, designation)); PRAGMA user_version = 15;");
            ExecuteSql("CREATE TABLE earthrotationparameters (date INTEGER PRIMARY KEY, modifiedjuliandate REAL, x REAL, y REAL, ut1_utc REAL, lod REAL, dx REAL, dy REAL); INSERT INTO earthrotationparameters VALUES (0,0,0,0,0,0,0,0);");
            originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            Environment.SetEnvironmentVariable("LOCALAPPDATA", directory);
            var rise = new Mock<RiseAndSetEvent>(reference, 0d, 0d, 0d).Object;
            night = new NighttimeData(reference, reference, default, 0, rise, rise, rise, rise, rise);
            var calculator = new Mock<INighttimeCalculator>();
            calculator.Setup(x => x.Calculate(It.IsAny<DateTime?>())).Returns(night);
            var profile = new Mock<IProfileService> { DefaultValue = DefaultValue.Mock };
            profile.SetupGet(x => x.ActiveProfile.AstrometrySettings.Horizon).Returns((CustomHorizon)null);
            profile.SetupGet(x => x.ActiveProfile.ApplicationSettings.PageSize).Returns(20);
            profile.SetupGet(x => x.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory).Returns(Path.Combine(directory, "cache"));
            vm = new SkyAtlasVM(profile.Object, Mock.Of<ITelescopeMediator>(), Mock.Of<IFramingAssistantVM>(),
                Mock.Of<ISequenceMediator>(), calculator.Object, Mock.Of<IApplicationMediator>());
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (vm.SelectedAltitudeDuration != 1) {
                if (DateTime.UtcNow > deadline) { Assert.Fail("Sky Atlas initialization did not finish"); }
                await Task.Delay(10);
            }
            await Task.Delay(20);
            vm.OrderByField = SkyAtlasOrderByFieldsEnum.TRANSITTIME;
        }

        [TearDown]
        public void TearDown() {
            night?.Ticker.Stop();
            Environment.SetEnvironmentVariable("LOCALAPPDATA", originalLocalAppData);
            SQLiteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SQLiteConnection.ClearAllPools();
            if (Directory.Exists(directory)) { Directory.Delete(directory, true); }
        }

        [TestCase(11.995)]
        [TestCase(12.005)]
        [TestCase(23.9)]
        [TestCase(0.01)]
        public async Task Search_UsesClockTimeForTransitAcrossMidnightAndDayBoundaries(double hoursAfterNoon) {
            AddTarget("Target", hoursAfterNoon);
            var transit = reference.AddHours(hoursAfterNoon);
            vm.SelectedTransitTimeFrom = transit.AddSeconds(-10);
            vm.SelectedTransitTimeThrough = transit.AddSeconds(10);
            await ((IAsyncCommand)vm.SearchCommand).ExecuteAsync(null);
            vm.SearchResult.ItemPage.Select(x => x.Id).Should().Equal("Target");
        }

        [TestCase(SkyAtlasOrderByDirectionEnum.ASC)]
        [TestCase(SkyAtlasOrderByDirectionEnum.DESC)]
        public async Task Search_SortsBothDirectionsAndResetsTransitFilters(SkyAtlasOrderByDirectionEnum direction) {
            AddTarget("BeforeMidnight", 11);
            AddTarget("AfterMidnight", 13);
            AddTarget("Morning", 18);
            vm.OrderByDirection = direction;
            vm.SelectedTransitTimeFrom = reference.AddHours(10);
            vm.SelectedTransitTimeThrough = reference.AddHours(14);
            await ((IAsyncCommand)vm.SearchCommand).ExecuteAsync(null);
            vm.SearchResult.ItemPage.Select(x => x.Id).Should().Equal(direction == SkyAtlasOrderByDirectionEnum.ASC
                ? new[] { "BeforeMidnight", "AfterMidnight" } : new[] { "AfterMidnight", "BeforeMidnight" });
            vm.ResetFiltersCommand.Execute(null);
            vm.SelectedTransitTimeFrom.Should().Be(DateTime.MinValue);
            vm.SelectedTransitTimeThrough.Should().Be(DateTime.MaxValue);
            await ((IAsyncCommand)vm.SearchCommand).ExecuteAsync(null);
            vm.SearchResult.Count.Should().Be(3);
        }

        private void AddTarget(string name, double hoursAfterNoon) {
            var ra = AstroUtil.EuclidianModulus(AstroUtil.GetLocalSiderealTime(reference.AddHours(hoursAfterNoon), 0), 24) * 15;
            using var connection = new SQLiteConnection($"Data Source={database};Pooling=False;");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO dsodetail(id,ra,dec,sizemax,constellation,dsotype) VALUES (@id,@ra,0,30,'ORI','GALAXY'); INSERT INTO cataloguenr VALUES(@id,'NAME',@id);";
            command.Parameters.AddWithValue("@id", name);
            command.Parameters.AddWithValue("@ra", ra);
            command.ExecuteNonQuery();
        }

        private void ExecuteSql(string sql) {
            using var connection = new SQLiteConnection($"Data Source={database};Pooling=False;");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
