#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Database.Schema {

    public class Constellation {
        public string constellationid { get; set; }

        public int starid { get; set; }
        public int followstarid { get; set; }
    }

    internal class ConstellationConfiguration : EntityTypeConfiguration<Constellation> {

        public ConstellationConfiguration() {
            ToTable("dbo.constellation");
            HasKey(x => new { x.constellationid, x.starid, x.followstarid });
            Property(x => x.constellationid).HasColumnName("constellationid").IsRequired();
        }
    }
}