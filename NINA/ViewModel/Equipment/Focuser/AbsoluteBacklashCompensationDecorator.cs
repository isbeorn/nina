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