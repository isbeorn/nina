#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace NINA.Database.Schema {

    public class EarthRotationParameters {

        [Key]
        public int date { get; set; }

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