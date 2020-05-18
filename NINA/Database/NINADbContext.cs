#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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

    public class NINADbContext : DbContext {
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
                Migrate(context);

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
