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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Database.Schema {

    public class DsoDetail {

        [Key]
        public string id { get; set; }

        public double ra { get; set; }
        public double dec { get; set; }

        public double? magnitude { get; set; }

        public double? surfacebrightness { get; set; }

        public double? sizemin { get; set; }

        public double? sizemax { get; set; }

        public double? positionangle { get; set; }

        public double? nrofstars { get; set; }

        //public double? brighteststar { get; set; }
        public string constellation { get; set; }

        public string dsotype { get; set; }
        public string dsoclass { get; set; }

        public string notes { get; set; }
        public string syncedfrom { get; set; }

        public string lastmodified { get; set; }
    }

    internal class DsoDetailConfiguration : EntityTypeConfiguration<DsoDetail> {

        public DsoDetailConfiguration() {
            ToTable("dbo.dsodetail");
            HasKey(x => x.id);
            Property(x => x.id).HasColumnName("id").IsRequired();
            Property(x => x.ra).HasColumnName("ra").IsRequired();
            Property(x => x.dec).HasColumnName("dec").IsRequired();

            Property(x => x.magnitude).HasColumnName("magnitude").IsOptional();
            Property(x => x.surfacebrightness).HasColumnName("surfacebrightness").IsOptional();
            Property(x => x.sizemin).HasColumnName("sizemin").IsOptional();
            Property(x => x.sizemax).HasColumnName("sizemax").IsOptional();
            Property(x => x.positionangle).HasColumnName("positionangle").IsOptional();
            Property(x => x.nrofstars).HasColumnName("nrofstars").IsOptional();
            //Property(x => x.brighteststar).HasColumnName("brighteststar").IsOptional();
            Property(x => x.notes).HasColumnName("notes").IsOptional();
        }
    }
}