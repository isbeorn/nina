#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Profile.Interfaces;
using System.Linq;
using System.Windows.Shell;
using static System.Windows.Shell.JumpList;

namespace NINA {

    internal static class AppJumpListExtensions {

        internal static void RefreshJumpList(
            this App app,
            IProfileService profileService) =>
                SetJumpList(app,
                (GetJumpList(app) ?? new JumpList())
                .WithJumpTaskForEachAvailableProfile(profileService));

        private static JumpList WithJumpTaskForEachAvailableProfile(
            this JumpList jumpList,
            IProfileService profileService) {
            profileService
                .Profiles
                .GroupJoin(
                    jumpList.JumpItems.OfType<JumpTask>(),
                    p => $"--profileid {p.Id}",
                    jt => jt.Arguments,
                    (p, jt) => new { Profile = p, JumpTask = jt.SingleOrDefault() }
                )
                .OrderBy(p => p.Profile.Name)
                .ToList()
                .ForEach(x => {
                    if (x.JumpTask == null) {
                        jumpList.JumpItems.Add(
                            new JumpTask {
                                Title = x.Profile.Name,
                                Description = "Launch N.I.N.A. using profile " + x.Profile.Name,
                                ApplicationPath = System.Environment.ProcessPath,
                                Arguments = $"--profileid {x.Profile.Id}",
                                WorkingDirectory = System.IO.Directory.GetCurrentDirectory(),
                                CustomCategory = "Profiles",
                            });
                    } else {
                        x.JumpTask.Title = x.Profile.Name;
                        x.JumpTask.Description = "Launch N.I.N.A. using profile " + x.Profile.Name;
                    }
                });
            jumpList.Apply();
            return jumpList;
        }
    }
}