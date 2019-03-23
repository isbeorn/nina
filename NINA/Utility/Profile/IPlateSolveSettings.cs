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

using NINA.Model.MyFilterWheel;
using NINA.Utility.Enum;

namespace NINA.Utility.Profile {

    public interface IPlateSolveSettings : ISettings {
        string AstrometryAPIKey { get; set; }
        BlindSolverEnum BlindSolverType { get; set; }
        string CygwinLocation { get; set; }
        double ExposureTime { get; set; }
        FilterInfo Filter { get; set; }
        PlateSolverEnum PlateSolverType { get; set; }
        string PS2Location { get; set; }
        int Regions { get; set; }
        double SearchRadius { get; set; }
        double Threshold { get; set; }
        double RotationTolerance { get; set; }
        string AspsLocation { get; set; }
        string ASTAPLocation { get; set; }
        int DownSampleFactor { get; set; }
        int MaxObjects { get; set; }
    }
}