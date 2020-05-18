#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace NINA.Database.Schema {

    public class EarthRotationParameters {

        [Key]
        public long date { get; set; }

        public double modifiedjuliandate { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double ut1_utc { get; set; }
        public double lod { get; set; }
        public double dx { get; set; }
        public double dy { get; set; }
    }

    internal class EarthRotationParametersConfiguration : EntityTypeConfiguration<EarthRotationParameters> {

        public EarthRotationParametersConfiguration() {
            ToTable("dbo.earthrotationparameters");
            HasKey(x => x.date);
            Property(x => x.date).HasColumnName("date").IsRequired();
        }
    }
}
