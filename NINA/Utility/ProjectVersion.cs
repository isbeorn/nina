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

namespace NINA.Utility {

    internal class ProjectVersion {
        private Version version;

        public ProjectVersion(string version) {
            this.version = new Version(version);
        }

        public ProjectVersion(Version version) {
            this.version = version;
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
                    return $"{major}.{minor} {patch}NIGHTLY #{buildNumber}";
            }
        }
    }
}