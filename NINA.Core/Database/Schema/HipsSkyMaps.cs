#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace NINA.Core.Database.Schema {

    public class HipsSkyMaps {

        [Key]
        public string Id { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string Path { get; set; }
        public string Band { get; set; }
        public double Coverage { get; set; }
        public string Comment { get; set; }
    }

    internal class HipsSkyMapsConfiguration : EntityTypeConfiguration<HipsSkyMaps> {

        public HipsSkyMapsConfiguration() {
            ToTable("dbo.hipsskymaps");
            HasKey(x => x.Id);
            Property(x => x.Id).HasColumnName("id").IsRequired();
            HasKey(x => x.Path);
            Property(x => x.Path).HasColumnName("path").IsRequired();
        }
    }
}