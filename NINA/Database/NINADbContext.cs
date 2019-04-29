#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Database.Schema;
using NINA.Utility;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace NINA.Database {

    internal class NINADbContext : DbContext {
        public IDbSet<EarthRotationParameters> EarthRotationParameterSet { get; set; }
        public IDbSet<BrightStars> BrightStarsSet { get; set; }
        public IDbSet<DsoDetail> DsoDetailSet { get; set; }
        public IDbSet<Constellation> ConstellationSet { get; set; }
        public IDbSet<ConstellationStar> ConstellationStarSet { get; set; }
        public IDbSet<ConstellationBoundaries> ConstellationBoundariesSet { get; set; }
        public IDbSet<VisualDescription> VisualDescriptionSet { get; set; }
        public IDbSet<CatalogueNr> CatalogueNrSet { get; set; }

        public NINADbContext(string connectionString) : base(new SQLiteConnection() { ConnectionString = connectionString }, true) {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Configurations.Add(new EarthRotationParametersConfiguration());
            modelBuilder.Configurations.Add(new BrightStarsConfiguration());
            modelBuilder.Configurations.Add(new DsoDetailConfiguration());
            modelBuilder.Configurations.Add(new ConstellationConfiguration());
            modelBuilder.Configurations.Add(new ConstellationStarConfiguration());
            modelBuilder.Configurations.Add(new ConstellationBoundariesConfiguration());
            modelBuilder.Configurations.Add(new VisualDescriptionConfiguration());
            modelBuilder.Configurations.Add(new CatalogueNrConfiguration());

            var sqi = new CreateOrMigrateDatabaseInitializer<NINADbContext>();
            System.Data.Entity.Database.SetInitializer(sqi);
        }

        private class CreateOrMigrateDatabaseInitializer<TContext> : CreateDatabaseIfNotExists<TContext>, IDatabaseInitializer<TContext> where TContext : DbContext {

            void IDatabaseInitializer<TContext>.InitializeDatabase(TContext context) {
                base.InitializeDatabase(context);

                if (context.Database.Exists()) {
                    Migrate(context);
                }

                context.Database.SqlQuery<int>("PRAGMA foreign_keys = ON");
            }

            private void Migrate(DbContext context) {
                int version = context.Database.SqlQuery<int>("PRAGMA user_version").First();
                bool vacuum = false;
                context.Database.SqlQuery<int>("PRAGMA foreign_keys = OFF");

                int numTables = context.Database.SqlQuery<int>("SELECT COUNT(*) FROM sqlite_master AS TABLES WHERE TYPE = 'table'").First();

                var initial = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Database", "Initial");

                if (numTables == 0) {
                    try {
                        vacuum = true;
                        context.Database.BeginTransaction();

                        var initial_schema = Path.Combine(initial, "initial_schema.sql");
                        context.Database.ExecuteSqlCommand(File.ReadAllText(initial_schema));

                        var initial_data = Path.Combine(initial, "initial_data.sql");
                        context.Database.ExecuteSqlCommand(File.ReadAllText(initial_data));

                        context.Database.CurrentTransaction.Commit();
                    } catch (Exception ex) {
                        context.Database.CurrentTransaction.Rollback();
                        Logger.Error(ex);
                    }
                }

                var migration = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Database", "Migration");

                foreach (var migrationFile in Directory.GetFiles(migration, "*.sql")) {
                    if (!int.TryParse(Path.GetFileName(migrationFile).Split('.').First(), out int sqlVersion)) {
                        continue;
                    }

                    if (sqlVersion <= version) {
                        continue;
                    }

                    try {
                        var migrationScript = File.ReadAllText(migrationFile);
                        context.Database.BeginTransaction();
                        context.Database.ExecuteSqlCommand(migrationScript);
                        context.Database.CurrentTransaction.Commit();
                        vacuum = true;
                    } catch (Exception ex) {
                        context.Database.CurrentTransaction.Rollback();
                        Logger.Error(ex);
                    }
                }

                try {
                    if (vacuum) {
                        context.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, "VACUUM;");
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }
    }
}