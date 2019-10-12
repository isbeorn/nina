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
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Database.Schema {

    public class CatalogueNr {
        public virtual DsoDetail DsoDetail { get; set; }

        [ForeignKey("DsoDetail")]
        public string dsodetailid { get; set; }

        public string catalogue { get; set; }

        public string designation { get; set; }
    }

    internal class CatalogueNrConfiguration : EntityTypeConfiguration<CatalogueNr> {

        public CatalogueNrConfiguration() {
            ToTable("dbo.cataloguenr");
            HasKey(x => new { x.dsodetailid, x.catalogue, x.designation });
            Property(x => x.dsodetailid).HasColumnName("dsodetailid").IsRequired();
            Property(x => x.catalogue).HasColumnName("catalogue").IsRequired();
            Property(x => x.designation).HasColumnName("designation").IsRequired();
        }
    }
}