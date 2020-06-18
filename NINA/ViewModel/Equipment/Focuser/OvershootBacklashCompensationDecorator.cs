#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyFocuser;
using NINA.Profile;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.Focuser {

    internal class OvershootBacklashCompensationDecorator : FocuserDecorator {

        public OvershootBacklashCompensationDecorator(IProfileService profileService, IFocuser focuser) : base(profileService, focuser) {
        }

        public override async Task Move(int position, CancellationToken ct) {
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

                    Logger.Debug($"Moving back to position {targetPosition}");
                }
            }

            await base.Move(targetPosition, ct);
        }

        private int CalculateBacklashCompensation(int lastPosition, int newPosition) {
            var direction = DetermineMovingDirection(lastPosition, newPosition);

            if (direction == Direction.IN && profileService.ActiveProfile.FocuserSettings.BacklashIn != 0) {
                return profileService.ActiveProfile.FocuserSettings.BacklashIn * -1;
            } else if (direction == Direction.OUT && profileService.ActiveProfile.FocuserSettings.BacklashOut != 0) {
                return profileService.ActiveProfile.FocuserSettings.BacklashOut;
            } else {
                return 0;
            }
        }
    }
}