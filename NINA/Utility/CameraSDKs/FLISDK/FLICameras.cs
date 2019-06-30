#region "copyright"

/*
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

/*
 * Copyright (c) 2019 Dale Ghent <daleg@elemental.org> All rights reserved.
 */

#endregion "copyright"

using NINA.Utility;
using System.Collections.Generic;
using System.Linq;

namespace FLI {

    public static class FLICameras {

        public static List<string> GetCameras() {
            List<string> cams = new List<string>();
            uint domain = (uint)(LibFLI.FLIDomains.DEV_CAMERA | LibFLI.FLIDomains.IF_USB);

            cams = LibFLI.N_FLIList(domain);
            Logger.Debug(string.Format("FLI: Found {0} camera(s)", cams.Count()));

            return cams;
        }
    }
}