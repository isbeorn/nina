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

using NINA.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {

    internal class ProjectVersion {
        private Version version;

        public ProjectVersion(string version) {
            this.version = new Version(version);
        }

        /// <summary>
        /// N.I.N.A.utilizes the versioning scheme MAJOR.MINOR.PATCH.KIND|BUILDNRXXX
        /// There is currently no automation used and versions are maintained manually.
        ///
        /// MAJOR version increases for big changes, like changing technologies etc.
        ///
        /// MINOR version will increase for every new released version
        ///
        /// PATCH version is reserved to apply Hotfixes to a released versions
        ///
        /// KIND|BUILDNR will not be displayed for Released versions, as these are only used to identify Release, RC, Beta and Develop versions
        ///
        /// KIND consists of the following values:
        /// * 0: Develop
        /// * 1: Beta
        /// * 2: Release Candidate
        /// * 9: Release
        ///
        /// BUILDNR should be incremented each nightly build(only in develop, beta and rc versions) by using 3 digits.
        ///
        /// Examples:
        /// Release: 1.8.0.9000             (Displayed as "1.8.0")
        /// Release: 1.8.1.9000             (Displayed as "1.8.0 HF1")
        /// Release Candidate: 1.8.0.2001   (Displayed as "1.8.0 RC 1")
        /// Beta: 1.8.0.1004                (Displayed as "1.8.0 BETA 1")
        /// Develop: 1.8.0.0022             (Displayed as "1.8.0 DEVELOP 022")
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            int major = version.Major;
            int minor = version.Minor;
            int build = version.Build;
            // (PadLeft is used to pad the leading DEV zeros, as those are truncated by version object)
            string revision = version.Revision.ToString().PadLeft(4, '0');

            string kind = revision.Substring(0, 1);
            string buildNumber = revision.Substring(1, revision.Length - 1);

            string patch = string.Empty;
            if (build > 0) {
                patch = $"HF{build} ";
            }

            if (kind == "9") {
                //Release does not show anything after patch
                return $"{major}.{minor} {patch}";
            } else if (kind == "1") {
                return $"{major}.{minor} {patch}BETA{buildNumber}";
            } else if (kind == "2") {
                return $"{major}.{minor} {patch}RC{buildNumber}";
            } else {
                var buildDate = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime;
                return $"{major}.{minor} {patch}DEVELOP{buildNumber} - Build Date {buildDate}";
            }
        }
    }
}