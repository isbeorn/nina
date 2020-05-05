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

using NINA.Model.MyFocuser;
using NINA.Profile;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.Focuser {

    /// <summary>
    /// This decorator will wrap an absolute backlash compensation model around the focuser.
    /// On each move an absolute backlash compensation value will be applied, if the focuser changes its moving direction
    /// The returned position will then accommodate for this backlash and simulating the position without backlash
    /// </summary>
    internal class AbsoluteBacklashCompensationDecorator : FocuserDecorator {

        // The offset that is remembered to simulate a focus position without backlash compensation
        private int offset = 0;

        public AbsoluteBacklashCompensationDecorator(IProfileService profileService, IFocuser focuser) : base(profileService, focuser) {
        }

        /// <summary>
        /// Returns the adjusted position based on the amount of backlash compensation
        /// </summary>
        public override int Position {
            get {
                return base.Position - offset;
            }
        }

        public override Task Move(int position, CancellationToken ct) {
            var startPosition = base.Position;
            var adjustedTargetPosition = position + offset;

            var backlashCompensation = CalculateBacklashCompensation(startPosition, adjustedTargetPosition);

            Logger.Debug($"Backlash compensation is using backlash value of {backlashCompensation}");

            var finalizedTargetPosition = adjustedTargetPosition + backlashCompensation;
            offset += backlashCompensation;

            return base.Move(finalizedTargetPosition, ct);
        }

        private int CalculateBacklashCompensation(int lastPosition, int newPosition) {
            var direction = DetermineMovingDirection(lastPosition, newPosition);

            if (direction == Direction.IN && base.lastDirection == Direction.OUT) {
                Logger.Debug("Focuser is reversing direction from outwards to inwards");
                return profileService.ActiveProfile.FocuserSettings.BacklashIn * -1;
            } else if (direction == Direction.OUT && base.lastDirection == Direction.IN) {
                Logger.Debug("Focuser is reversing direction from inwards to outwards");
                return profileService.ActiveProfile.FocuserSettings.BacklashOut;
            } else {
                return 0;
            }
        }
    }
}