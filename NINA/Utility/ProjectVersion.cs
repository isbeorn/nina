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
        /// N.I.N.A. utilizes the versioning scheme MAJOR.MINOR.PATCH.CHANNEL|BUILDNRXXX
        /// There is currently no automation used and versions are maintained manually.
        ///
        /// MAJOR version increases for big changes, like changing technologies etc.
        ///
        /// MINOR version will increase for every new released version
        ///
        /// PATCH version is reserved to apply Hotfixes to a released versions
        ///
        /// CHANNEL|BUILDNR will not be displayed for Released versions, as these are only used to identify Release, RC, Beta and Develop versions
        ///
        /// CHANNEL consists of the following values:
        /// * 1: Nightly
        /// * 2: Beta
        /// * 3: Release Candidate
        /// * 9: Release
        ///
        /// BUILDNR should be incremented each nightly build(only in develop, beta and rc versions) by using 3 digits.
        ///
        /// Examples:
        /// Release: 1.8.0.9001             (Displayed as "1.8")
        /// Release: 1.8.1.9001             (Displayed as "1.8 HF1")
        /// Release Candidate: 1.8.0.3001   (Displayed as "1.8 RC1")
        /// Beta: 1.8.0.2004                (Displayed as "1.8 BETA4")
        /// Develop: 1.8.0.1022             (Displayed as "1.8 NIGHTLY #022")
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            int major = version.Major;
            int minor = version.Minor;
            int build = version.Build;
            string revision = version.Revision.ToString();

            string channel = revision.Substring(0, 1);
            string buildNumber = revision.Substring(1, revision.Length - 1);

            string patch = string.Empty;
            if (build > 0) {
                patch = $"HF{build} ";
            }

            switch (channel) {
                case "9":
                    //Release does not show anything after patch
                    return $"{major}.{minor} {patch}";

                case "2":
                    return $"{major}.{minor} {patch}BETA{buildNumber}";

                case "3":
                    return $"{major}.{minor} {patch}RC{buildNumber}";

                default:
                    var buildDate = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime;
                    return $"{major}.{minor} {patch}NIGHTLY #{buildNumber} - Build Date {buildDate}";
            }
        }
    }
}