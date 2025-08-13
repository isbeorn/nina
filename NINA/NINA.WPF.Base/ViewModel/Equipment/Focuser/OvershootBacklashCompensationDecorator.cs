#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces;
using System;
using NINA.Core.Locale;

namespace NINA.WPF.Base.ViewModel.Equipment.Focuser {

    public class OvershootBacklashCompensationDecorator : FocuserDecorator {

        public OvershootBacklashCompensationDecorator(IProfileService profileService, IFocuser focuser) : base(profileService, focuser) {
        }

        public override async Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            var startPosition = base.Position;
            var targetPosition = position;

            var backlashCompensation = CalculateBacklashCompensation(startPosition, targetPosition);

            if (backlashCompensation != 0) {
                var overshoot = targetPosition + backlashCompensation;

                if (overshoot < 0) {
                    Logger.Debug($"Overshooting position is below minimum 0, skipping overshoot");
                } else if (overshoot > MaxStep) {
                    Logger.Debug($"Overshooting position is above maximum {MaxStep}, skipping overshoot");
                } else {
                    Logger.Debug($"Overshooting from {startPosition} to overshoot position {overshoot} using a compensation of {backlashCompensation}");

                    await base.Move(overshoot, ct);

                    //Wait for focuser to settle
                    if (profileService.ActiveProfile.FocuserSettings.FocuserSettleTime > 0) {
                        Logger.Debug($"Settling Focuser for {profileService.ActiveProfile.FocuserSettings.FocuserSettleTime}s after overshooting");
                        await CoreUtil.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.FocuserSettings.FocuserSettleTime), ct);
                    }

                    Logger.Debug($"Moving back to position {targetPosition}");
                }
            }

            await base.Move(targetPosition, ct);
        }

        private int CalculateBacklashCompensation(int lastPosition, int newPosition) {
            var direction = DetermineMovingDirection(lastPosition, newPosition);

            if (direction == OvershootDirection.IN && profileService.ActiveProfile.FocuserSettings.BacklashIn != 0) {
                return profileService.ActiveProfile.FocuserSettings.BacklashIn * -1;
            } else if (direction == OvershootDirection.OUT && profileService.ActiveProfile.FocuserSettings.BacklashOut != 0) {
                return profileService.ActiveProfile.FocuserSettings.BacklashOut;
            } else {
                return 0;
            }
        }
    }
}