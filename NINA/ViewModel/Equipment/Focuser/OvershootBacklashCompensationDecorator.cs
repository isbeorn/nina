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

using ASCOM.Astrometry;
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
                Logger.Debug($"Overshooting from {startPosition} to overshoot position {overshoot} using a compensation of {backlashCompensation}");

                await base.Move(overshoot, ct);

                Logger.Debug($"Moving back to position {targetPosition}");
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